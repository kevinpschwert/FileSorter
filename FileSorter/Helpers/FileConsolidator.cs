using FileSorter.Cached.Interfaces;
using FileSorter.Common;
using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;

namespace FileSorter.Helpers
{
    public class FileConsolidator : IFileConsolidator
    {
        private readonly ICachedService _cachedService;
        private readonly DBContext _db;
        private readonly ILogging _logging;

        public FileConsolidator(ICachedService cachedService, DBContext db, ILogging logging)
        {
            _cachedService = cachedService;
            _db = db;
            _logging = logging;
        }

        public void ConsolidateFiles(string destinationPath, ArrayOfExportFileMetadata files, string unzipped)
        {
            string fileName = string.Empty;
            string clientFile = string.Empty;
            foreach (var file in files.ClientFiles)
            {
                try
                {
                    fileName = file.FileName;
                    clientFile = file.EntityName;
                    var folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == file.Class && x.Subclass == file.Subclass);
                    if (folderMapping == null)
                    {
                        folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == null && x.Subclass == null);

                    }
                    string contactAccountFolder = file.EntityID.Contains("-100") ? "Contacts" : "Accounts";
                    string filePath = $"{destinationPath}\\{contactAccountFolder}";
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    string clientFolder = $"{file.EntityName} - {file.EntityID}";
                    // If the folder in not in the main consolidated file folder, create the folder.
                    string clientName = Path.Combine(filePath, clientFolder);
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
                    if (file.VirtualFolderPath.Contains("2019 and prior"))
                    {
                        folderName = file.FolderName;
                    }
                    else
                    {
                        folderName = !string.IsNullOrEmpty(file.Year.ToString()) && file.Year != 0 ? file.Year.ToString() : file.FolderName;
                    }

                    string level3 = folderMapping.Level3 == "Year" ? folderName : folderMapping.Level3;

                    string subClass = Path.Combine(folderClass, level3);
                    if (!Directory.Exists(subClass))
                    {
                        Directory.CreateDirectory(subClass);
                    }
                    if (!string.IsNullOrEmpty(folderMapping.Level4))
                    {
                        string level4 = folderMapping.Level4 == "Year" ? folderName : folderMapping.Level4;
                        subClass = Path.Combine(subClass, level4);
                        if (!Directory.Exists(subClass))
                        {
                            Directory.CreateDirectory(subClass);
                        }
                    }
                    string yearFilePath = FindFolder(unzipped, clientFolder, folderName, file.VirtualFolderPath, file.FileName);
                    if (string.IsNullOrEmpty(yearFilePath))
                    {
                        throw new Exception($"File {file.FileName} does not exist in any of the folders.");
                    }
                    var path = Path.Combine(yearFilePath, file.FileName);
                    System.IO.File.Move(Path.Combine(yearFilePath, file.FileName), (Path.Combine(subClass, file.FileName)));
                    var clientFiles = _db.ClientFiles.FirstOrDefault(f => f.FileIntID == file.FileIntID);
                    clientFiles.FolderMappingId = folderMapping.FolderMappingId;
                    clientFiles.ModifiedDate = DateTime.Now;
                    clientFiles.StatusId = (int)Status.Processed;
                    _db.Update(clientFiles);
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logging.Log(ex.Message, clientFile, fileName);
                }
            }
        }

        public bool ValidateConsolidatedFiles(string destinationPath, ArrayOfExportFileMetadata files, string unzipped)
        {
            foreach (var file in files.ClientFiles)
            {
                var folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == file.Class && x.Subclass == file.Subclass);
                if (folderMapping == null)
                {
                    folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == null && x.Subclass == null);

                }
                string clientFolder = $"{file.EntityName} - {file.EntityID}";
                string contactAccountFolder = file.EntityID.Contains("-100") ? "Contacts" : "Accounts";
                string filePath = $"{destinationPath}\\{contactAccountFolder}";
                string clientName = Path.Combine(filePath, clientFolder);
                if (!Directory.Exists(clientName))
                {
                    return false;
                    throw new Exception($"Client folder {clientName} does not exist.");
                }
                string folderClass = Path.Combine(clientName, file.Class);
                if (!Directory.Exists(folderClass))
                {
                    return false;
                    throw new Exception($"Client folder {folderClass} does not exist.");
                }

                string folderName = string.Empty;
                if (file.VirtualFolderPath.Contains("2019 and prior"))
                {
                    folderName = file.FolderName;
                }
                else
                {
                    folderName = !string.IsNullOrEmpty(file.Year.ToString()) && file.Year != 0 ? file.Year.ToString() : file.FolderName;
                }
                string level3 = folderMapping.Level3 == "Year" ? folderName : folderMapping.Level3;

                string subClass = Path.Combine(folderClass, level3);
                if (!Directory.Exists(subClass))
                {
                    return false;
                    throw new Exception($"Level 3 folder {level3} does not exist.");
                }
                if (!string.IsNullOrEmpty(folderMapping.Level4))
                {
                    string level4 = folderMapping.Level4 == "Year" ? folderName : folderMapping.Level4;
                    subClass = Path.Combine(subClass, level4);
                    if (!Directory.Exists(subClass))
                    {
                        return false;
                        throw new Exception($"Level 4 Folder {folderMapping.Level4} does not exist.");
                    }
                }
                string yearFilePath = FindFolder(unzipped, clientFolder, folderName, file.VirtualFolderPath, file.FileName);
                if (string.IsNullOrEmpty(yearFilePath))
                {
                    return false;
                    throw new Exception($"File {file.FileName} does not exist in any of the folders.");
                }
                if (!File.Exists(Path.Combine(subClass, file.FileName)))
                {
                    return false;
                    throw new Exception($"File {file.FileName} does not exist.");
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
