using Azure.Identity;
using FileSorter.Data;
using FileSorter.Interfaces;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;
using Microsoft.Graph;
using System.Diagnostics;
using static FileSorter.Common.Constants;

namespace FileSorter.Helpers
{
    public class SharePointUploader : ISharePointUploader
    {
        private readonly ILogging _logging;
        private readonly IConfiguration _configuration;
        private readonly DBContext _db;
        private string _clientId;
        private string _tenantId;
        private string _clientSecret;
        private string _sharePointSiteId;
        private string _driveId;
        private List<SharePointFileUpload> _uploadedFiles = new List<SharePointFileUpload>();
        private List<SharePointFileUpload> _filesToRetry = new List<SharePointFileUpload>();
        private List<SharePointFileUpload> _filesRetriedSuccessful = new List<SharePointFileUpload>();
        private int _retries = 0;
        private static readonly int _maxRetries = 3;            // Maximum retry attempts
        private static readonly int _baseDelay = 1000;          // Base delay for exponential backoff in milliseconds
        private static readonly double _exponentialFactor = 2;
        private string _uploadSessionGuid;

        public SharePointUploader(ILogging logging, IConfiguration configuration, DBContext db)
        {
            _logging = logging;
            _configuration = configuration;
            _db = db;
        }

        private readonly int _maxConcurrentUploads = 5;

        public async Task Upload(List<SharePointFileUpload> sharePointFileUploads, string uploadSessionGuid)
        {
            _clientId = _configuration["SharePointOnline:client_id"];
            _clientSecret = _configuration["SharePointOnline:client_secret"];
            _tenantId = _configuration["SharePointOnline:tenant"];
            _sharePointSiteId = _configuration["SharePointOnline:site_id"];
            _driveId = _configuration["SharePointOnline:drive_id"];
            _uploadSessionGuid = uploadSessionGuid;
            var graphClient = GetAuthenticatedGraphClient();

            // Upload all files concurrently
            await UploadFilesInParallel(graphClient, sharePointFileUploads);
        }

        public async Task VerifyUploadedFiles(List<string> files)
        {
            _clientId = _configuration["SharePointOnline:client_id"];
            _clientSecret = _configuration["SharePointOnline:client_secret"];
            _tenantId = _configuration["SharePointOnline:tenant"];
            var graphClient = GetAuthenticatedGraphClient();
            foreach (var xml in files)
            {
                var uploadSessionId = _db.UploadSessions.FirstOrDefault(x => x.XMLFile == xml).UploadSessionGuid;
                var cf = _db.ClientFiles.ToList();
                var filesToUpload = cf.Where(x => x.UploadSessionGuid == uploadSessionId && x.StatusId == (int)Common.Status.Processed && (x.FolderName == FileClass.PERMANENT || (int.TryParse(x.FolderName, out int year) && year >= 2020))).ToList();
                var sharePointFileUpload = filesToUpload.Select(x => new SharePointFileUpload
                {
                    DriveFilePath = x.DriveFilePath,
                    SharePointFilePath = x.SharePointFilePath,
                    FileIntId = x.FileIntID,
                    ClientName = x.EntityName,
                    UploadSessionGuid = x.UploadSessionGuid,
                    FileName = x.FileName
                }).ToList();
                foreach (var file in sharePointFileUpload)
                {
                    try
                    {
                        var fileInSharePoint = await graphClient
                        .Sites[_sharePointSiteId]
                        .Drives[_driveId]
                        //.Items[file.ClientFolderId]
                        .Root
                        .ItemWithPath($"{file.SharePointFilePath}/{file.FileName}")
                        .Request()
                        .GetAsync();
                        if (fileInSharePoint == null)
                        {
                            _logging.Log($"The following file was not uploaded to SharePoint: {file.FileName}", file.ClientName, file.FileName, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        // Method to upload multiple files in parallel
        private async Task UploadFilesInParallel(GraphServiceClient graphClient, List<SharePointFileUpload> sharePointFileUploads)
        {
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            var uploadTasks = new List<Task>();

            // Use a limited number of concurrent tasks to avoid overwhelming the server
            using (var semaphore = new System.Threading.SemaphoreSlim(_maxConcurrentUploads))
            {
                foreach (var fileUpload in sharePointFileUploads)
                {
                    await semaphore.WaitAsync();

                    var filePath = fileUpload.DriveFilePath;
                    var sharePointFolderPath = fileUpload.SharePointFilePath;

                    uploadTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await UploadFileToSharePoint(graphClient, fileUpload);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(uploadTasks); // Wait for all uploads to complete
            }

            _filesToRetry.RemoveAll(f => _filesRetriedSuccessful.Contains(f));
            if (_filesToRetry.Any() && _retries < 3)
            {
                _retries++;
                await UploadFilesInParallel(graphClient, _filesToRetry);
            }
            else if (_filesToRetry.Any())
            {
                _filesToRetry.ForEach(f =>
                {
                    _logging.Log($"The following files were not uplaoded to SharePoint: {f.FileName}", f.ClientName, f.FileName, null);
                });
            }

            try
            {
                var filesNotUploaded = sharePointFileUploads.Where(f => !_uploadedFiles.Any(u => u.FileIntId == f.FileIntId)).ToList();
                var uploadedFilesList = _uploadedFiles.ToList(); // This ensures that _uploadedFiles is evaluated in memory
                var uploadedClients = (from c in _db.ClientFiles.AsEnumerable() // Switch to in-memory evaluation
                                       join f in uploadedFilesList on new { X1 = c.FileIntID, X2 = c.FileName, X3 = c.UploadSessionGuid } equals new { X1 = f.FileIntId, X2 = f.FileName, X3 = f.UploadSessionGuid }
                                       where c.StatusId == (int)Common.Status.Processed
                                       select c).ToList();
                uploadedClients.ForEach(c =>
                {
                    c.ModifiedDate = DateTime.Now;
                    c.StatusId = (int)Common.Status.Migrated;
                });
                _db.BulkUpdate(uploadedClients);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logging.Log(ex.Message, null, null, null);
            }

            // We need to ensure that all the files were properly uploaded to SharePoint so we check if the files are there
            foreach (var file in sharePointFileUploads)
            {
                try
                {
                    var fileInSharePoint = await graphClient
                    .Sites[_sharePointSiteId]
                    .Drives[_driveId]
                    //.Items["01PAGKWCM2EEFJAQ62KRB32J7KAFRWZT7Q"] // Use for testing
                    //.Items[file.ClientFolderId] // Use for Production
                    .Root
                    .ItemWithPath($"{file.SharePointFilePath}/{file.FileName}")
                    .Request()
                    .GetAsync();
                    if (fileInSharePoint == null)
                    {
                        _logging.Log($"The following file was not uploaded to SharePoint: {file.FileName}", file.ClientName, file.FileName, null);
                    }
                }
                catch (Exception ex)
                {
                    if (_uploadedFiles.Contains(file))
                    {
                        _uploadedFiles.Remove(file);
                    }
                    _logging.Log($"The following error occurred while checking if the file was uploaded to SharePoint: {file.FileName} - {ex.Message}", file.ClientName, file.FileName, null);
                }
            }
        }

        // Method to upload a single file to SharePoint
        private async Task UploadFileToSharePoint(GraphServiceClient graphClient, SharePointFileUpload fileUpload)
        {
            try
            {
                var fileName = Path.GetFileName(fileUpload.DriveFilePath);

                using (var fileStream = new FileStream(fileUpload.DriveFilePath, FileMode.Open))
                {

                    var driveItem = new DriveItem
                    {
                        Name = fileUpload.FileName,
                        File = new Microsoft.Graph.File(),
                        AdditionalData = new Dictionary<string, object>
                        {
                            {"@microsoft.graph.conflictBehavior", "fail"}
                        }
                    };

                    // Navigate to the SharePoint folder and create an upload session
                    var uploadSession = await graphClient
                        .Sites[_sharePointSiteId]
                        .Drives[_driveId]
                        //.Items["01PAGKWCM2EEFJAQ62KRB32J7KAFRWZT7Q"] // Use for testing 
                        //.Items[fileUpload.ClientFolderId] // Use for Production
                        .Root
                        .ItemWithPath($"{fileUpload.SharePointFilePath}/{fileName}")
                        .CreateUploadSession(new DriveItemUploadableProperties
                        {
                            AdditionalData = new Dictionary<string, object>
                            {
                                {"@microsoft.graph.conflictBehavior", "fail"} // Ensure the upload fails if the file exists
                            }
                        })
                        .Request()
                        .PostAsync();

                    var maxChunkSize = 320 * 1024; // 320 KB per chunk, adjust based on file sizes and network conditions
                    var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxChunkSize);

                    // Upload the file in chunks
                    var uploadResult = await fileUploadTask.UploadAsync();

                    if (uploadResult.UploadSucceeded)
                    {
                        if (_filesToRetry.Contains(fileUpload))
                        {
                            _filesRetriedSuccessful.Add(fileUpload);
                        }
                        _uploadedFiles.Add(new SharePointFileUpload
                        {
                            FileIntId = fileUpload.FileIntId,
                            FileName = fileName,
                            UploadSessionGuid = fileUpload.UploadSessionGuid
                        });
                    }
                    else
                    {
                        _logging.Log($"Error uploading the following file to SharePoint: {fileName}", null, null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_filesToRetry.Contains(fileUpload) && !ex.Message.Contains("The specified item name already exists."))
                {
                    _filesToRetry.Add(fileUpload);
                }
            }
        }

        private async Task RetryWithExponentialBackoffAsync(Func<Task> action)
        {
            int attempt = 0;

            while (attempt < _maxRetries)
            {
                try
                {
                    attempt++;
                    // Execute the action (in this case, file upload)
                    await action();

                    // If successful, break out of the loop
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt >= _maxRetries)
                    {
                        _logging.Log($"Maximum retry attempts reached. Operation failed. Error: {ex.Message}");
                        throw; // Rethrow the exception after max retries
                    }

                    // Calculate exponential backoff delay
                    TimeSpan calculatedDelay = TimeSpan.FromMilliseconds(_baseDelay * Math.Pow(_exponentialFactor, attempt - 1));

                    Console.WriteLine($"Attempt {attempt} of {_maxRetries} failed. Retrying in {calculatedDelay.TotalSeconds} seconds... Error: {ex.Message}");

                    // Wait before retrying
                    await Task.Delay(calculatedDelay);
                }
            }
        }

        private async Task ValidateUploadedFiles(GraphServiceClient graphClient, List<SharePointFileUpload> sharePointFileUploads)
        {
            foreach (var file in sharePointFileUploads)
            {
                try
                {
                    var fileInSharePoint = await graphClient
                    .Sites[_sharePointSiteId]
                    .Drives[_driveId]
                    .Root
                    .ItemWithPath($"{file.SharePointFilePath}/{file.FileName}")
                    .Request()
                    .GetAsync();
                    if (fileInSharePoint == null)
                    {
                        _logging.Log($"The following file was not uploaded to SharePoint: {file.FileName}", file.ClientName, file.FileName, null);
                    }
                }
                catch (Exception ex)
                {
                    if (_uploadedFiles.Contains(file))
                    {
                        _uploadedFiles.Remove(file);
                    }
                    _logging.Log($"The following error occurred while checking if the file was uploaded to SharePoint: {file.FileName} - {ex.Message}", file.ClientName, file.FileName, null);
                }
            }
        }

        private void ValidateFiles(GraphServiceClient graphClient, List<SharePointFileUpload> sharePointFileUploads)
        {
            ValidateUploadedFiles(graphClient, sharePointFileUploads).Wait();
        }

        // Method to get the authenticated Graph Client using ClientSecretCredential
        private GraphServiceClient GetAuthenticatedGraphClient()
        {
            var clientSecretCredential = new ClientSecretCredential(
                _tenantId, _clientId, _clientSecret);

            return new GraphServiceClient(clientSecretCredential);
        }

    }
}
