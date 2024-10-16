using FileSorter.Cached.Interfaces;
using FileSorter.Common;
using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;
using System.Linq;
using static FileSorter.Common.Constants;

namespace FileSorter.Helpers
{
    public class FileConsolidator : IFileConsolidator
    {
        private readonly ICachedService _cachedService;
        private readonly DBContext _db;
        private readonly ILogging _logging;
        private readonly IConfiguration _configuration;
        private string _fileName;
        private string _clientFile;

        public FileConsolidator(ICachedService cachedService, DBContext db, ILogging logging, IConfiguration configuration)
        {
            _cachedService = cachedService;
            _db = db;
            _logging = logging;
            _configuration = configuration;
        }

        public async Task<List<SharePointFileUpload>> ConsolidateFiles(string destinationPath, ArrayOfExportFileMetadata files, string xmlFile, string uploadSessionGuid)
        {
            List<SharePointFileUpload> sharePointFileList = new List<SharePointFileUpload>();
            List<string> clientsNoZohoMapping = new List<string>();
            foreach (var file in files.ClientFiles)
            {
                try
                {  
                    _fileName = file.FileName;
                    _clientFile = file.EntityName;
                    FolderMapping folderMapping = new FolderMapping();
                    bool shouldAddToSharePoint = true;
                    string contactAccountFolder = file.EntityID.Contains("-5") ? MainFolder.ACCOUNTS : MainFolder.CONTACTS;
                    var dbFolderMapping = _cachedService.FolderMapping.Where(x => x.Class == file.Class && x.Subclass == file.Subclass);
                    // If the class and subclass of a file do not have a mapping, then we set a default mapping for it and do not add it to SharePoint
                    if (!dbFolderMapping.Any())
                    {
                        folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == null && x.Subclass == null);
                        shouldAddToSharePoint = false;
                    }
                    // If the file mapping is depending on the Account Type is has, we need to make sure we get the correct mapping per the Account Type
                    else if (!string.IsNullOrEmpty(dbFolderMapping.FirstOrDefault().AccountType))
                    {
                        var folderMappingAcctType = contactAccountFolder == MainFolder.ACCOUNTS ? dbFolderMapping.Where(x => x.AccountType == AccountType.PARTNERSHIPS) : dbFolderMapping.Where(x => x.AccountType == AccountType.INDIVIDUAL);
                        if (folderMappingAcctType.Count() == 1)
                        {
                            folderMapping = folderMappingAcctType.FirstOrDefault();
                        }
                        else
                        {
                            if (file.FolderName == FileClass.PERMANENT)
                            {
                                folderMapping = folderMappingAcctType.LastOrDefault();
                            }
                            else
                            {
                                folderMapping = folderMappingAcctType.FirstOrDefault();
                            }
                        }
                    }
                    else if (string.IsNullOrEmpty(file.FolderName) || (file.FolderName == FileClass.PERMANENT && (file.Year is > 2000 and < 2020) && file.Class != FileClass.PERMANENT))
                    {
                        folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.FolderMappingId == (int)DefaultFolderMapping.LongTermPlanningDocuments);
                    }
                    else if (dbFolderMapping.Count() == 1)
                    {
                        folderMapping = dbFolderMapping.FirstOrDefault();
                    }
                    else if (dbFolderMapping.Count() > 1)
                    {
                        if (file.FolderName == FileClass.PERMANENT)
                        {
                            folderMapping = _cachedService.FolderMapping.LastOrDefault(x => x.Class == file.Class && x.Subclass == file.Subclass);
                        }
                        else
                        {
                            folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == file.Class && x.Subclass == file.Subclass);
                        }
                    }

                    string filePath = $"{destinationPath}\\{contactAccountFolder}";
                    string clientFolder = $"{file.EntityName} - {file.EntityID}";
                    string clientIdFolder = string.Empty;
                    var zohoMapping = _cachedService.Clients.FirstOrDefault(x => x.CWAId.Split("-")[0] == file.EntityID.Split("-")[0] || x.XCMId?.Split("-")[0] == file.EntityID.Split("-")[0] || x.ClientName == file.EntityName);
                    // If the Client does not have a Zoho Id mapped to it, then we need to add it to a different folder
                    if (zohoMapping is null)
                    {
                        if (!clientsNoZohoMapping.Contains(file.EntityName))
                        {
                            clientsNoZohoMapping.Add(file.EntityName);
                            _logging.Log($"Client {file.EntityName} does not have a Zoho ID.", _clientFile, _fileName, xmlFile);
                        }
                        clientIdFolder = clientFolder;
                        filePath = $"{destinationPath}\\NoZohoId";
                        shouldAddToSharePoint = false;
                    }
                    else
                    {
                        clientIdFolder = $"{file.EntityName} ({zohoMapping.ZohoId})";
                    }
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    // If the folder in not in the main consolidated file folder, create the folder.
                    string clientName = Path.Combine(filePath, clientIdFolder);
                    if (!Directory.Exists(clientName))
                    {
                        Directory.CreateDirectory(clientName);
                    }
                    string folderClass = Path.Combine(clientName, folderMapping.Level2);
                    if (!Directory.Exists(folderClass))
                    {
                        Directory.CreateDirectory(folderClass);
                    }
                    string folderName = file.FolderName;
                    string subClass = string.Empty;
                    if (folderMapping.Level2 == FileClass.ARCHIVED)
                    {
                        subClass = folderClass;
                    }
                    else
                    {
                        string folderYear = !string.IsNullOrEmpty(file.Year.ToString()) && file.Year != 0 && file.Year >= 2020 ? file.Year.ToString() : file.FolderName;
                        string level3 = folderMapping.Level3 == FileClass.YEAR ? folderYear : folderMapping.Level3;

                        subClass = Path.Combine(folderClass, level3);
                        if (!Directory.Exists(subClass))
                        {
                            Directory.CreateDirectory(subClass);
                        }
                        if (!string.IsNullOrEmpty(folderMapping.Level4))
                        {
                            string level4 = folderMapping.Level4 == FileClass.YEAR ? folderYear : folderMapping.Level4;
                            subClass = Path.Combine(subClass, level4);
                            if (!Directory.Exists(subClass))
                            {
                                Directory.CreateDirectory(subClass);
                            }
                        }
                    }

                    string yearFilePath = FindFolder(xmlFile, clientFolder, folderName, file.VirtualFolderPath, file.FileName);
                    if (string.IsNullOrEmpty(yearFilePath))
                    {
                        throw new Exception($"File {file.FileName} does not exist in any of the folders.");
                    }

                    // Because Clients can have multiple files with the filename, we need to append a number to the end of the ones that have the same name.
                    if (File.Exists(Path.Combine(subClass, file.FileName)))
                    {
                        var fileType = file.Type.Split('.')[1];
                        var splitFile = file.FileName.Split($".{fileType}")[0];
                        var counter = 1;
                        string newFileName = string.Empty;
                        do
                        {
                            newFileName = $"{splitFile}_Copy{counter}.{fileType}";
                            counter++;
                        }
                        while (File.Exists(Path.Combine(subClass, newFileName)));
                        System.IO.File.Move(Path.Combine(yearFilePath, file.FileName), (Path.Combine(subClass, newFileName)));
                    }
                    else
                    {
                        System.IO.File.Move(Path.Combine(yearFilePath, file.FileName), (Path.Combine(subClass, file.FileName)));
                    }
                    var clientFiles = _db.ClientFiles.FirstOrDefault(f => f.FileIntID == file.FileIntID && f.FileName == file.FileName && f.UploadSessionGuid == uploadSessionGuid);
                    if (clientFiles is null)
                    {
                        _logging.Log($"Could not find the following file: {_fileName}", _clientFile, _fileName, xmlFile);
                    }
                    else
                    {
                        string driveFilePath = Path.Combine(subClass, _fileName);
                        var sharePointFilePath = driveFilePath.Split(clientIdFolder)[1].Replace("\\", "/").Replace(_fileName, string.Empty);
                        clientFiles.DriveFilePath = driveFilePath;
                        clientFiles.SharePointFilePath = sharePointFilePath;
                        clientFiles.FolderMappingId = folderMapping.FolderMappingId;
                        clientFiles.ModifiedDate = DateTime.Now; 
                        clientFiles.StatusId = (int)Status.Processed;
                        _db.Update(clientFiles);
                        if (shouldAddToSharePoint)
                        {
                            var clientInSharePoint = _cachedService.SharePointsFolders.FirstOrDefault(x => x.Client.Contains(zohoMapping.ZohoId));
                            if (clientInSharePoint is not null && (folderName == FileClass.PERMANENT || (int.TryParse(folderName, out int year) && year >= 2020) || string.IsNullOrEmpty(file.FolderName)))
                            {
                                SharePointFileUpload sharePointFile = new SharePointFileUpload
                                {
                                    DriveFilePath = driveFilePath,
                                    SharePointFilePath = sharePointFilePath,
                                    FileIntId = file.FileIntID,
                                    ClientName = file.EntityName,
                                    UploadSessionGuid = uploadSessionGuid,
                                    FileName = file.FileName,
                                    ClientFolderId = clientInSharePoint.ClientFolderId
                                };
                                sharePointFileList.Add(sharePointFile);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logging.Log(ex.Message, _clientFile, _fileName, xmlFile);
                }
            }
            _db.SaveChanges();
            return sharePointFileList;
        }

        public bool ValidateConsolidatedFiles(string destinationPath, ArrayOfExportFileMetadata files, string xmlFile)
        {
            foreach (var file in files.ClientFiles)
            {
                FolderMapping folderMapping = new FolderMapping();
                var dbFolderMapping = _cachedService.FolderMapping.Where(x => x.Class == file.Class && x.Subclass == file.Subclass);
                if (dbFolderMapping.Count() == 1)
                {
                    folderMapping = dbFolderMapping.FirstOrDefault();
                }
                else if (dbFolderMapping.Count() > 1)
                {
                    if (file.FolderName == FileClass.PERMANENT)
                    {
                        folderMapping = _cachedService.FolderMapping.LastOrDefault(x => x.Class == file.Class && x.Subclass == file.Subclass);
                    }
                    else
                    {
                        folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == file.Class && x.Subclass == file.Subclass);
                    }
                }
                else
                {
                    folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == null && x.Subclass == null);

                }
                string contactAccountFolder = file.EntityID.Contains("-300") || file.EntityID.Contains("-500") ? MainFolder.ACCOUNTS : MainFolder.CONTACTS;

                string filePath = $"{destinationPath}\\{contactAccountFolder}";
                if (!Directory.Exists(filePath))
                {
                    return false;
                    throw new Exception($"File folder {filePath} does not exist.");
                }
                string clientFolder = $"{file.EntityName} - {file.EntityID}";
                var zohoMapping = _cachedService.Clients.FirstOrDefault(x => x.CWAId.Split("-")[0] == file.EntityID.Split("-")[0] || x.ClientName == file.EntityName);
                if (zohoMapping is not null)
                {
                    string zohoId = zohoMapping.ZohoId;
                    string clientFolderWithZoho = $"{file.EntityName} ({zohoId})";
                    // If the folder in not in the main consolidated file folder, create the folder.
                    string clientName = Path.Combine(filePath, clientFolderWithZoho);
                    if (!Directory.Exists(clientName))
                    {
                        return false;
                        throw new Exception($"Client {clientName} does not exist.");
                    }
                    string folderClass = Path.Combine(clientName, folderMapping.Level2);
                    if (!Directory.Exists(folderClass))
                    {
                        return false;
                        throw new Exception($"Level 2 {folderClass} does not exist.");
                    }
                    string folderName = string.Empty;
                    if (file.VirtualFolderPath.Contains(FileClass.PRIOR2019))
                    {
                        folderName = file.FolderName;
                    }
                    else
                    {
                        folderName = !string.IsNullOrEmpty(file.Year.ToString()) && file.Year != 0 ? file.Year.ToString() : file.FolderName;
                    }
                    string subClass = string.Empty;
                    if (folderMapping.Level2 == FileClass.ARCHIVED)
                    {
                        subClass = folderClass;
                    }
                    else
                    {
                        string level3 = folderMapping.Level3 == FileClass.YEAR ? folderName : folderMapping.Level3;

                        subClass = Path.Combine(folderClass, level3);
                        if (!Directory.Exists(subClass))
                        {
                            return false;
                            throw new Exception($"Level 3 {subClass} does not exist.");
                        }
                        if (!string.IsNullOrEmpty(folderMapping.Level4))
                        {
                            string level4 = folderMapping.Level4 == FileClass.YEAR ? folderName : folderMapping.Level4;
                            subClass = Path.Combine(subClass, level4);
                            if (!Directory.Exists(subClass))
                            {
                                return false;
                                throw new Exception($"Level 4 {subClass} does not exist.");
                            }
                        }
                    }

                    string yearFilePath = FindFolder(xmlFile, clientFolder, folderName, file.VirtualFolderPath, file.FileName);
                    if (string.IsNullOrEmpty(yearFilePath))
                    {
                        throw new Exception($"File {file.FileName} does not exist in any of the folders.");
                    }

                    if (!File.Exists(Path.Combine(subClass, file.FileName)))
                    {
                        return false;
                        throw new Exception($"File {file.FileName} does not exist.");
                    }
                }
            }

            return true;
        }


        private string FindFolder(string file, string companyName, string year, string virtualFolderPath, string fileName)
        {
            string yearFilePath = $"C:\\Users\\cchdoc\\Desktop\\ExportClients\\{file}\\Clients\\Clients in Main Office Office - Main BU\\{companyName}\\Managed";
            string yearPath = !string.IsNullOrEmpty(virtualFolderPath) ? virtualFolderPath : year;
            if (virtualFolderPath.Contains("2019 and prior"))
            {
                yearFilePath = Path.Combine(yearFilePath, "2019 and prior", year);
                return yearFilePath;
            }
            else if (virtualFolderPath == "Permanent")
            {
                yearFilePath = Path.Combine(yearFilePath, "Permanent");
                return yearFilePath;
            }
            else if (System.IO.File.Exists(Path.Combine(yearFilePath, yearPath, fileName)))
            {
                return Path.Combine(yearFilePath, yearPath);
            }
            else if (System.IO.File.Exists(Path.Combine(yearFilePath, fileName)))
            {
                return yearFilePath;
            }

            return string.Empty;
        }
    }
}