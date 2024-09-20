using FileSorter.Cached.Interfaces;
using FileSorter.Common;
using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;
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

        public async Task<List<SharePointFileUpload>> ConsolidateFiles(string destinationPath, ArrayOfExportFileMetadata files, string xmlFile)
        {
            List<SharePointFileUpload> sharePointFileList = new List<SharePointFileUpload>();
            foreach (var file in files.ClientFiles)
            {
                try
                {
                    _fileName = file.FileName;
                    _clientFile = file.EntityName;
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
                    string contactAccountFolder = file.EntityID.Contains("-300") || file.EntityID.Contains("-50") ? MainFolder.ACCOUNTS : MainFolder.CONTACTS;
                    string filePath = $"{destinationPath}\\{contactAccountFolder}";
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    string clientFolder = $"{file.EntityName} - {file.EntityID}";
                    var zohoMapping = _cachedService.ZohoClientIdMappings.FirstOrDefault(x => x.ClientId.Split("-")[0] == file.EntityID.Split("-")[0] || x.ClientName == file.EntityName);
                    if (zohoMapping is not null)
                    {
                        string zohoId = zohoMapping.ZohoId;
                        string clientFolderWithZoho = $"{file.EntityName} ({zohoId})";
                        // If the folder in not in the main consolidated file folder, create the folder.
                        string clientName = Path.Combine(filePath, clientFolderWithZoho);
                        if (!Directory.Exists(clientName))
                        {
                            Directory.CreateDirectory(clientName);
                        }
                        string folderClass = Path.Combine(clientName, folderMapping.Level2);
                        if (!Directory.Exists(folderClass))
                        {
                            Directory.CreateDirectory(folderClass);
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
                                Directory.CreateDirectory(subClass);
                            }
                            if (!string.IsNullOrEmpty(folderMapping.Level4))
                            {
                                string level4 = folderMapping.Level4 == FileClass.YEAR ? folderName : folderMapping.Level4;
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
                        string newFileName = file.FileName;
                        if (File.Exists(Path.Combine(subClass, file.FileName)))
                        {
                            var fileType = file.Type.Split('.')[1];
                            var splitFile = file.FileName.Split($".{fileType}")[0];
                            var counter = 1;
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
                            System.IO.File.Move(Path.Combine(yearFilePath, file.FileName), (Path.Combine(subClass, newFileName)));
                        }
                        var clientFiles = _db.ClientFiles.FirstOrDefault(f => f.FileIntID == file.FileIntID && f.FileName == file.FileName);
                        if (clientFiles is null)
                        {
                            _logging.Log($"Could not find the following file: {_fileName}", _fileName, _clientFile, xmlFile);
                        }
                        else
                        {
                            clientFiles.FolderMappingId = folderMapping.FolderMappingId;
                            clientFiles.ModifiedDate = DateTime.Now;
                            clientFiles.StatusId = (int)Status.Processed;
                            _db.Update(clientFiles);
                            if (folderName == FileClass.PERMANENT || (int.TryParse(folderName, out int year) && year >= 2020))
                            {
                                string driveFilePath = Path.Combine(subClass, _fileName);
                                var sharePointFilePath = driveFilePath.Split("\\ConsolidateData")[1].Replace("\\", "/").Replace(_fileName, string.Empty);
                                SharePointFileUpload sharePointFile = new SharePointFileUpload
                                {
                                    DriveFilePath = driveFilePath,
                                    SharePointFilePath = sharePointFilePath,
                                    FileIntId = file.FileIntID,
                                    ClientName = file.EntityName
                                };
                                sharePointFileList.Add(sharePointFile);
                            }
                        }
                    }
                    else
                    {
                        _logging.Log($"Client {file.EntityName} does not have a Zoho ID.", _clientFile, _fileName, xmlFile);
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
                var zohoMapping = _cachedService.ZohoClientIdMappings.FirstOrDefault(x => x.ClientId.Split("-")[0] == file.EntityID.Split("-")[0] || x.ClientName == file.EntityName);
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
            string yearFilePath = $"C:\\Users\\kevin\\OneDrive\\Desktop\\ExportClients\\{file}\\Clients\\Clients in Main Office Office - Main BU\\{companyName}\\Managed";
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