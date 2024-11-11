using Azure.Identity;
using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;

namespace FileSorter.Helpers
{
    public class HelpfulCode
    {
        private readonly DBContext _db;
        private IConfiguration _configuration;
        private ILogging _logging;
        private string _clientId;
        private string _clientSecret;
        private string _tenantId;
        private string _sharePointSiteId;
        private string _driveId;
        private int _retries = 0;
        private static readonly int _maxRetries = 3;            // Maximum retry attempts
        private static readonly int _baseDelay = 1000;          // Base delay for exponential backoff in milliseconds
        private static readonly double _exponentialFactor = 2;

        public HelpfulCode(IConfiguration configuration, ILogging logging, DBContext db)
        {
            _configuration = configuration;
            _logging = logging;
            _db = db;
        }

        public async Task MoveFilesAsync()
        {
            _clientId = _configuration["SharePointOnline:client_id"];
            _clientSecret = _configuration["SharePointOnline:client_secret"];
            _tenantId = _configuration["SharePointOnline:tenant"];
            _sharePointSiteId = _configuration["SharePointOnline:site_id"];
            _driveId = _configuration["SharePointOnline:drive_id"];
            var graphClient = GetAuthenticatedGraphClient();
            var clientFiles = _db.ClientFiles.ToList();
            var sharePointFolders = _db.SharePointFolders.ToList();
            //var filesToMove = clientFiles.Where(x => x.Class == "Planning" && x.Subclass == "Main" && x.SharePointFilePath == "/Permanent/Long-Term Correspondence/" && x.StatusId == 3).ToList().OrderBy(x => x.EntityName);
            var filesToMove = clientFiles.Where(x => x.FolderMappingId == 98 && !string.IsNullOrEmpty(x.FolderName) && x.SharePointFilePath == "/Permanent/Long-Term Correspondence/" && x.StatusId == 3).ToList().OrderBy(x => x.EntityName);
            var distinctClient = filesToMove.Select(x => x.EntityName).Distinct().OrderBy(x => x);
            Dictionary<string, string> folderIdList = new Dictionary<string, string>();
            var clientName = string.Empty;
            var fileName = string.Empty;
            foreach (var dc in distinctClient)
            {
                var entityId = clientFiles.FirstOrDefault(x => x.EntityName == dc).EntityID;
                var client = _db.Clients.Where(x => x.CWAId == entityId || x.XCMId == entityId).FirstOrDefault();
                var clientInSharePoint = sharePointFolders.FirstOrDefault(x => x.Client.Contains(client.ZohoId));
                folderIdList.Add(dc, clientInSharePoint.ClientFolderId);
            }

            foreach (var folder in folderIdList)
            {
                var folders = await graphClient
                        .Sites[_sharePointSiteId]
                        .Drives[_driveId]
                        .Items[folder.Value]
                        .Children
                        .Request()
                        .GetAsync();
                var folderNames = folders.Select(x => x.Name).ToList();
                if (!folderNames.Contains("Permanent"))
                {
                    _logging.Log($"The following Client does not have a the Permanent folder: {clientName}");
                    continue;
                }
                var permanentFolder = folders.FirstOrDefault(x => x.Name == "Permanent");
                var permanentFolderChildren = await graphClient
                   .Sites[_sharePointSiteId]
                   .Drives[_driveId]
                   .Items[permanentFolder.Id]
                   .Children
                   .Request()
                   .GetAsync();
                var permanentFolderNames = permanentFolderChildren.Select(x => x.Name).ToList();
                if (!permanentFolderNames.Contains("Long-Term Planning Documents"))
                {
                    _logging.Log($"The following Client does not have a the Long-Term Planning Documents folder: {clientName}");
                    continue;
                }
                var client = filesToMove.Where(x => x.EntityName == folder.Key);
                foreach (var item in client)
                {
                    try
                    {
                        clientName = item.EntityName;
                        fileName = item.FileName;

                        var sourceFile = await graphClient
                            .Sites[_sharePointSiteId]
                            .Drives[_driveId]
                            .Items[folder.Value]
                            .ItemWithPath($"/Permanent/Long-Term Correspondence/{item.FileName}")
                            .Request()
                            .GetAsync();

                        var targetFile = await graphClient
                          .Sites[_sharePointSiteId]
                          .Drives[_driveId]
                          .Items[folder.Value]
                          .ItemWithPath($"/Permanent/Long-Term Planning Documents/")
                          .Request()
                          .GetAsync();
                        // Define the destination folder path and update the parentReference for each file
                        var destinationItemReference = new ItemReference
                        {
                            Id = targetFile.Id
                        };
                        // Move the file by updating its parentReference
                        await graphClient
                            .Sites[_sharePointSiteId]
                            .Drives[_driveId]
                            .Items[sourceFile.Id]
                            .Request()
                            .UpdateAsync(new DriveItem
                            {
                                ParentReference = destinationItemReference,
                                Name = item.FileName
                            });
                        Console.WriteLine($"Files moved successfully. Client - {clientName}  --- File - {fileName}");

                    }
                    catch (ServiceException ex)
                    {
                        Console.WriteLine($"Error moving files: {ex.Message} - Client - {clientName} - File - {fileName}");
                    }
                }
            }

        }

        public void GetXMLFolderDiff()
        {
            string path = @"\\SILO\CCHExport\Raw Zip Files";
            List<string> directories = System.IO.Directory.GetFiles(path, "*.zip").ToList();
            List<string> xmlFileList = new List<string>();
            foreach (var dir in directories)
            {
                xmlFileList.Add(dir.Split("\\").LastOrDefault().Split(".zip")[0]);
            }
            var clientFiles = _db.ClientFiles.ToList();
            var xmlFiles = clientFiles.Select(x => x.XMLFIle).Distinct().ToList();
            var diff = xmlFileList.Except(xmlFiles).ToList();
            foreach (var file in diff)
            {
                Console.WriteLine(file);
            }
        }

        public void GetClientFolderDiff()
        {
            string noZohoIdPath = @"\\SILO\CCHExport\\NoZohoId";
            string accountsPath = @"\\SILO\CCHExport\\All Client Export\\Accounts";
            string contactsPath = @"\\SILO\CCHExport\\All Client Export\\Contacts";
            List<string> noZohoDirectories = System.IO.Directory.GetDirectories(noZohoIdPath).ToList();
            List<string> accountsDirectories = System.IO.Directory.GetDirectories(accountsPath).ToList();
            List<string> contactsDirectories = System.IO.Directory.GetDirectories(contactsPath).ToList();
            List<string> clients = new List<string>();
            foreach (var dir in noZohoDirectories)
            {
                clients.Add(dir.Split("\\").LastOrDefault().Split(" - ")[0]);
            }
            foreach (var dir in accountsDirectories)
            {
                clients.Add(dir.Split("\\").LastOrDefault().Split(" (")[0]);
            }
            foreach (var dir in contactsDirectories)
            {
                clients.Add(dir.Split("\\").LastOrDefault().Split(" (")[0]);
            }
            var clientFiles = _db.ClientFiles.ToList();
            var clientNames = clientFiles.Select(x => x.EntityName).Distinct().ToList();
            var diff = clients.Distinct().Except(clientNames).ToList();
            foreach (var file in diff)
            {
                Console.WriteLine(file);
            }
        }

        public async Task GetAllFoldersInParentFolder()
        {
            var _clientId = _configuration["SharePointOnline:client_id"];
            var _clientSecret = _configuration["SharePointOnline:client_secret"];
            var _tenantId = _configuration["SharePointOnline:tenant"];
            var _sharePointSiteId = _configuration["SharePointOnline:site_id"];
            var _driveId = _configuration["SharePointOnline:drive_id"];
            var graphClient = GetAuthenticatedGraphClient();
            List<DriveItem> allFolderItems = new List<DriveItem>();

            var folderItemsPage = await graphClient.Sites["a2a9413b-2cb1-4c09-b51f-e5c6e1d90ac8"]
                                   .Drives["b!O0GporEsCUy1H-XG4dkKyAXmA48tY0dDlG_z0i7Tq-sqS9c7xjhtT6nOYnDL5Gjh"]
                                    //.Items["01PAGKWCL6JW4FXZO2ZVA3PJ2W4L5DT2GV"] // Accounts
                                    .Items["01PAGKWCM73SIAEDGS3VBYRGLPN73E7MHY"] // Contacts
                                   .Children
                                   .Request()
                                   .GetAsync();

            allFolderItems.AddRange(folderItemsPage.CurrentPage);

            // Handle pagination
            while (folderItemsPage.NextPageRequest != null)
            {
                folderItemsPage = await folderItemsPage.NextPageRequest.GetAsync();
                allFolderItems.AddRange(folderItemsPage.CurrentPage);
            }

            // Filter only folder names
            var folderNames = allFolderItems
                              .Where(item => item.Folder != null) // Ensure it's a folder
                              .Select(item => new { item.Name, item.Id })
                              .ToList();

            List<SharePointFolders> sharePointFolders = new List<SharePointFolders>();
            // Output the folder names
            foreach (var folderName in folderNames)
            {
                SharePointFolders sharePointFolder = new SharePointFolders()
                {
                    Client = folderName.Name,
                    ClientFolderId = folderName.Id
                };
                sharePointFolders.Add(sharePointFolder);
            }
            _db.BulkInsert(sharePointFolders);
            _db.SaveChanges();
        }

        public async Task UploadNewFolders()
        {
            //01PAGKWCL6JW4FXZO2ZVA3PJ2W4L5DT2GV

            _clientId = _configuration["SharePointOnline:client_id"];
            _clientSecret = _configuration["SharePointOnline:client_secret"];
            _tenantId = _configuration["SharePointOnline:tenant"];
            _sharePointSiteId = _configuration["SharePointOnline:site_id"];
            _driveId = _configuration["SharePointOnline:drive_id"];
            var graphClient = GetAuthenticatedGraphClient();

            var folders = new List<DriveItem>();
            var result = await graphClient
                    .Sites[_sharePointSiteId]
                    .Drives[_driveId]
                    .Items["01PAGKWCL6JW4FXZO2ZVA3PJ2W4L5DT2GV"] // Accounts
                    .Children
                    .Request()
                    .GetAsync();

            folders.AddRange(result.CurrentPage);

            // Handle pagination if more pages are available
            while (result.NextPageRequest != null)
            {
                result = await result.NextPageRequest.GetAsync();
                folders.AddRange(result.CurrentPage);
            }

            int folderSkip = 4790;
            int index = 0;
            var months = Months();
            string client = string.Empty;
            var skipFolders = folders.Skip(folderSkip).ToList();
            int skipFoldersCount = skipFolders.Count();
            try
            {
                for (var i = 0; i < skipFoldersCount; i++)
                {
                    var childFolder = await graphClient
                        .Sites[_sharePointSiteId]
                        .Drives[_driveId]
                        .Items[skipFolders[i].Id]
                        .Children
                        .Request()
                        .GetAsync();
                    client = skipFolders[i].Name;
                    index = folderSkip + 1 + i;
                    var accountFolder = childFolder.FirstOrDefault(x => x.Name == "Accounting");
                    if (accountFolder is not null)
                    {
                        var yearFolders = await graphClient
                            .Sites[_sharePointSiteId]
                            .Drives[_driveId]
                            .Items[accountFolder.Id]
                            .Children
                            .Request()
                            .GetAsync();
                        foreach (var yearFolder in yearFolders)
                        {
                            var clientDocuments = await graphClient
                                .Sites[_sharePointSiteId]
                                .Drives[_driveId]
                                .Items[yearFolder.Id]
                                .Children
                                .Request()
                                .GetAsync();
                            var clientDocument = clientDocuments.FirstOrDefault(x => x.Name == "Client Documents");
                            if (clientDocument is not null)
                            {
                                //var monthFolders = await graphClient
                                //.Sites[_sharePointSiteId]
                                //.Drives[_driveId]
                                //.Items[clientDocument.Id]
                                //.Children
                                //.Request()
                                //.GetAsync();
                                //
                                //if (!monthFolders.Any())
                                //{
                                //    Console.WriteLine($"Client - {client} : Index - {index}");
                                //}
                                foreach (var month in months)
                                {
                                    await CreateFolderWithRetry(() => CreateMonthFolders(month, graphClient, clientDocument));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logging.Log($"Client {client} has the following error: {ex.Message} at Index {index}");
            }
        }

        private async Task<List<SharePointFileUpload>> CheckForDuplicateFiles(List<SharePointFileUpload> sharePointFileUploads)
        {
            var graphClient = GetAuthenticatedGraphClient();
            //Since adding a duplicate file will erase the most current file on SharePoint, we need to verify that there are no duplicate files
            List<SharePointFileUpload> existingFilesList = new List<SharePointFileUpload>();
            foreach (var fileUpload in sharePointFileUploads)
            {
                try
                {
                    var existingFiles = await graphClient
                    .Sites[_sharePointSiteId]
                    .Drives[_driveId]
                    .Items["01PAGKWCM2EEFJAQ62KRB32J7KAFRWZT7Q"]
                    .ItemWithPath($"{fileUpload.SharePointFilePath}/{fileUpload.FileName}")
                    .Request()
                    .GetAsync();

                    if (existingFiles != null)
                    {
                        existingFilesList.Add(fileUpload);
                    }
                }
                catch (Exception ex) { }
            }
            var updatedSharePointFileUploads = sharePointFileUploads.Except(existingFilesList).ToList();
            return updatedSharePointFileUploads;
        }

        private async Task CreateMonthFolders(string month, GraphServiceClient graphClient, DriveItem clientDocument)
        {
            var folderToCreate = new DriveItem
            {
                Name = month,
                Folder = new Folder(),
                AdditionalData = new Dictionary<string, object>
                                    {
                                        { "@microsoft.graph.conflictBehavior", "fail" } // Avoid overriding existing folders
                                    }
            };

            await graphClient.Sites[_sharePointSiteId].Drive.Items[clientDocument.Id].Children
                .Request()
                .AddAsync(folderToCreate);
        }

        private async Task CreateFolderWithRetry(Func<Task> action)
        {
            var graphClient = GetAuthenticatedGraphClient();
            int maxRetries = 3;
            int retryCount = 0;
            bool success = false;
            while (retryCount < maxRetries)
            {
                try
                {
                    retryCount++;
                    await action();

                    return;
                }
                catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // Handle throttling (HTTP 429)
                    retryCount++;
                    var retryAfter = ex.ResponseHeaders?.RetryAfter.Delta?.Seconds ?? 5;
                    Console.WriteLine($"Throttled, retrying in {retryAfter} seconds...");
                    await Task.Delay(retryAfter * 1000); // Wait before retrying
                }
                catch (Exception ex)
                {
                    if (retryCount >= maxRetries)
                    {
                        Console.WriteLine($"Maximum retry attempts reached. Operation failed. Error: {ex.Message}");
                        throw; // Rethrow the exception after max retries
                    }

                    // Calculate exponential backoff delay
                    TimeSpan calculatedDelay = TimeSpan.FromMilliseconds(_baseDelay * Math.Pow(_exponentialFactor, retryCount - 1));

                    Console.WriteLine($"Attempt {retryCount} of {_maxRetries} failed. Retrying in {calculatedDelay.TotalSeconds} seconds... Error: {ex.Message}");

                    // Wait before retrying
                    await Task.Delay(calculatedDelay);
                }
            }
        }

        private List<string> Months()
        {
            List<string> abbreviatedMonths = new List<string>
            {
                "01 - January",
                "02 - February",
                "03 - March",
                "04 - April",
                "05 - May",
                "06 - June",
                "07 - July",
                "08 - August",
                "09 - September",
                "10 - October",
                "11 - November",
                "12 - December",
                "Misc Documents",
                "W9 and 1099"
            };
            return abbreviatedMonths;
        }

        private GraphServiceClient GetAuthenticatedGraphClient()
        {
            var clientSecretCredential = new ClientSecretCredential(
                _tenantId, _clientId, _clientSecret);

            return new GraphServiceClient(clientSecretCredential);
        }

        public void UploadCsv()
        {

            List<MasterList> values = System.IO.File.ReadAllLines("C:\\Users\\cchdoc\\Desktop\\ClientCSV\\MasterList.csv")
                                         .Skip(0)
                                          .Select(v => MasterList.FromCsv(v))
                                          .ToList();

            List<ClientList> clValues = System.IO.File.ReadAllLines("C:\\Users\\cchdoc\\Desktop\\TotalClients.csv")
                                         .Skip(0)
                                          .Select(v => ClientList.FromCsv(v))
                                          .ToList();

            var cwaId = values.Select(x => x.CWAId).ToList();
            var test = clValues.Where(x => cwaId.Contains(x.CWAId)).ToList();

        }
    }

    public class MasterList
    {
        public string ZohoId { get; set; }
        public string CWAId { get; set; }
        public string Client { get; set; }
        public static MasterList FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(",");
            MasterList ml = new MasterList();
            ml.ZohoId = values[0].ToString();
            ml.Client = values[1].ToString();
            ml.CWAId = values[2].Split("-")[0].ToString();
            return ml;
        }
    }

    public class ClientList
    {
        public string CWAId { get; set; }
        public string Client { get; set; }
        public static ClientList FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(",");
            ClientList cl = new ClientList();
            cl.CWAId = values[0].Split("-")[0].ToString();
            cl.Client = values[1].ToString();
            return cl;
        }
    }
}

