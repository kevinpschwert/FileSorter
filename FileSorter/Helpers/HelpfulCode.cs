using Azure.Identity;
using FileSorter.Data;
using FileSorter.Entities;
using Microsoft.Graph;

namespace FileSorter.Helpers
{
    public class HelpfulCode
    {
        private readonly DBContext _db;
        private IConfiguration _configuration;
        private string _clientId;
        private string _clientSecret;
        private string _tenantId;
        private string _sharePointSiteId;
        private string _driveId;

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

            var folders = await graphClient
                    .Sites[_sharePointSiteId]
                    .Drives[_driveId]
                    .Items["01PAGKWCIUQRKWSDEO3ZHJ4MBLYQRJGMTM"] // ITTest
                                                                 //.Items["01PAGKWCL6JW4FXZO2ZVA3PJ2W4L5DT2GV"] // Accounts
                    .Children
                    .Request()
                    .GetAsync();
            var months = Months();
            foreach (var folder in folders)
            {
                var childFolder = await graphClient
                    .Sites[_sharePointSiteId]
                    .Drives[_driveId]
                    .Items[folder.Id]
                    .Children
                    .Request()
                    .GetAsync();
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
                            foreach (var month in months)
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
                        }                        
                    }
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
    }
}
