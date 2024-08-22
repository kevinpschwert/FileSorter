using FileSorter.Cached.Interfaces;
using FileSorter.Models;

namespace FileSorter.Helpers
{
    public class FileConsolidator : IFileConsolidator
    {
        private readonly ICachedService _cachedService;

        public FileConsolidator(ICachedService cachedService)
        {
            _cachedService = cachedService;
        }

        public void ConsolidateFiles(string destinationPath, ArrayOfExportFileMetadata files, string unzipped)
        {
            try
            {
                foreach (var file in files.ClientFiles)
                {
                    var folderMapping = _cachedService.FolderMapping.FirstOrDefault(x => x.Class == file.Class && x.Subclass == file.Subclass);
                    if (folderMapping == null)
                    {
                        throw new Exception($"Folder mapping does not exist for this Class and Subclass for Client {file.EntityName} and File {file.FileName}");
                    }
                    string clientFolder = $"{file.EntityName} - {file.EntityID}";
                    // If the folder in not in the main consolidated file folder, create the folder.
                    string clientName = Path.Combine(destinationPath, clientFolder);
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
                    if (!File.Exists(Path.Combine(subClass, file.FileName)))
                    {
                        var path = Path.Combine(yearFilePath, file.FileName);
                        System.IO.File.Move(Path.Combine(yearFilePath, file.FileName), (Path.Combine(subClass, file.FileName)));
                    }
                    else
                    {
                        bool exists = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public bool ValidateConsolidatedFiles(string destinationPath, ArrayOfExportFileMetadata files, string unzipped)
        {
            foreach (var file in files.ClientFiles)
            {
                string clientFolder = $"{file.EntityName} - {file.EntityID}";
                string clientName = Path.Combine(destinationPath, clientFolder);
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

                if (file.Class == "Permanent")
                {
                    string subClass = Path.Combine(folderClass, file.Subclass);
                    if (!Directory.Exists(subClass))
                    {
                        return false;
                        throw new Exception($"Client folder {subClass} does not exist.");
                    }
                    string yearFilePath = FindFolder(unzipped, clientFolder, folderName, file.VirtualFolderPath, file.FileName);
                    if (string.IsNullOrEmpty(yearFilePath))
                    {
                        throw new Exception($"File {file.FileName} does not exist in any of the folders.");
                    }
                    string consolidatedYearFilePath = Path.Combine(destinationPath, folderName, folderName);
                    consolidatedYearFilePath = Path.Combine(destinationPath, folderName, file.VirtualFolderPath);
                    var path = Path.Combine(yearFilePath, file.FileName);
                    if (!System.IO.File.Exists(Path.Combine(subClass, file.FileName)))
                    {
                        return false;
                        throw new Exception($"Client folder {file.FileName} does not exist.");
                    }
                }
                else
                {
                    if (!Directory.Exists(Path.Combine(folderClass, folderName)))
                    {
                        return false;
                        throw new Exception($"Client folder {file.FolderName} does not exist.");
                    }
                    if (!Directory.Exists(Path.Combine(folderClass, folderName, file.Subclass)))
                    {
                        return false;
                        throw new Exception($"Client folder {file.Subclass} does not exist.");
                    }

                    string consolidatedYearFilePath = Path.Combine(folderClass, folderName, file.Subclass);
                    if (!System.IO.File.Exists(Path.Combine(consolidatedYearFilePath, file.FileName)))
                    {
                        return false;
                        throw new Exception($"Client folder {file.FileName} does not exist.");
                    }
                }
            }

            return true;
        }


        private string FindFolder(string file, string companyName, string year, string virtualFolderPath, string fileName)
        {
            string yearFilePath = $"C:\\Users\\kevin\\ExportClients\\{file}\\Clients\\Clients in Main Office Office - Main BU\\{companyName}\\Managed";
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
