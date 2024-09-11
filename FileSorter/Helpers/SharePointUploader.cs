using Azure.Identity;
using FileSorter.Data;
using FileSorter.Interfaces;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;
using Microsoft.Graph;

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

        public SharePointUploader(ILogging logging, IConfiguration configuration, DBContext db)
        {
            _logging = logging;
            _configuration = configuration;
            _db = db;
        }

        private readonly int _maxConcurrentUploads = 10;

        public async Task Upload(List<SharePointFileUpload> sharePointFileUploads)
        {
            _clientId = _configuration["SharePointOnline:client_id"];
            _clientSecret = _configuration["SharePointOnline:client_secret"];
            _tenantId = _configuration["SharePointOnline:tenant"];
            _sharePointSiteId = _configuration["SharePointOnline:site_id"];
            _driveId = _configuration["SharePointOnline:drive_id"];
            var graphClient = GetAuthenticatedGraphClient();

            // Upload all files concurrently
            await UploadFilesInParallel(graphClient, sharePointFileUploads);
        }

        // Method to upload multiple files in parallel
        private async Task UploadFilesInParallel(GraphServiceClient graphClient, List<SharePointFileUpload> sharePointFileUploads)
        {
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
                            await UploadFileToSharePoint(graphClient, filePath, sharePointFolderPath, fileUpload.FileIntId);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(uploadTasks); // Wait for all uploads to complete
            }

            Console.WriteLine("All files uploaded.");
            await _db.SaveChangesAsync();
        }

        // Method to upload a single file to SharePoint
        private async Task UploadFileToSharePoint(GraphServiceClient graphClient, string filePath, string sharePointFolderPath, long fileIntId)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    // Navigate to the SharePoint folder and create an upload session
                    var uploadSession = await graphClient
                        .Sites[_sharePointSiteId]
                        .Drives[_driveId]
                        .Root
                        .ItemWithPath($"{sharePointFolderPath}/{fileName}")
                        .CreateUploadSession()
                        .Request()
                        .PostAsync();

                    var maxChunkSize = 320 * 1024; // 320 KB per chunk, adjust based on file sizes and network conditions
                    var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxChunkSize);

                    // Upload the file in chunks
                    var uploadResult = await fileUploadTask.UploadAsync();

                    if (uploadResult.UploadSucceeded)
                    {
                        var clientFiles = _db.ClientFiles.FirstOrDefault(f => f.FileIntID == fileIntId && f.FileName == fileName);
                        clientFiles.ModifiedDate = DateTime.Now;
                        clientFiles.StatusId = (int)Common.Status.Migrated;
                        _db.Update(clientFiles);
                        Console.WriteLine($"File '{fileName}' uploaded to '{sharePointFolderPath}' successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"File upload for '{fileName}' failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logging.Log(ex.Message, null, null, null);
            }
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
