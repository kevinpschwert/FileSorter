using FileSorter.Models;

namespace FileSorter.Helpers
{
    public class FileConsolidator
    {
        public static void ConsolidateFiles(string destinationPath, ArrayOfExportFileMetadata files, List<string> unzipped)
        {
            try
            {
                foreach (var file in files.ClientFiles)
                {
                    string clientFolder = $"{file.EntityName} - {file.EntityID}";
                    // If the folder in not in the main consolidated file folder, create the folder.
                    string clientName = Path.Combine(destinationPath, clientFolder);
                    if (!Directory.Exists(clientName))
                    {
                        Directory.CreateDirectory(clientName);
                    }
                    string folderClass = Path.Combine(clientName, file.Class);
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

                    if (file.Class == "Permanent")
                    {
                        string subClass = Path.Combine(folderClass, file.Subclass);
                        if (!Directory.Exists(subClass))
                        {
                            Directory.CreateDirectory(subClass);
                        }
                        string yearFilePath = FindFolder(unzipped, clientFolder, folderName, file.VirtualFolderPath, file.FileName);
                        if (string.IsNullOrEmpty(yearFilePath))

                        {
                            throw new Exception($"File {file.FileName} does not exist in any of the folders.");
                        }
                        string consolidatedYearFilePath = Path.Combine(destinationPath, clientFolder, folderName);
                        consolidatedYearFilePath = Path.Combine(destinationPath, clientFolder, file.VirtualFolderPath);
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
                    else
                    {
                        if (!Directory.Exists(Path.Combine(folderClass, folderName)))
                        {
                            Directory.CreateDirectory(Path.Combine(folderClass, folderName));
                        }
                        if (!Directory.Exists(Path.Combine(folderClass, folderName, file.Subclass)))
                        {
                            Directory.CreateDirectory(Path.Combine(folderClass, folderName, file.Subclass));
                        }
                        string yearFilePath = FindFolder(unzipped, clientFolder, folderName, file.VirtualFolderPath, file.FileName);
                        if (string.IsNullOrEmpty(yearFilePath))
                        {
                            throw new Exception($"File {file.FileName} does not exist in any of the folders.");
                        }
                        string consolidatedYearFilePath = Path.Combine(folderClass, folderName, file.Subclass);
                        if (!System.IO.File.Exists(Path.Combine(consolidatedYearFilePath, file.FileName)))
                        {
                            var path = Path.Combine(yearFilePath, file.FileName);
                            System.IO.File.Move(Path.Combine(yearFilePath, file.FileName), (Path.Combine(consolidatedYearFilePath, file.FileName)));
                        }
                        else
                        {
                            bool exists = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static bool ValidateConsolidatedFiles(string destinationPath, ArrayOfExportFileMetadata files, List<string> unzipped)
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
                    string yearFilePath = FindFolder(unzipped, clientFolder, folderName, file.VirtualFolderPath, file.FileName);
                    if (string.IsNullOrEmpty(yearFilePath))
                    {
                        throw new Exception($"File {file.FileName} does not exist in any of the folders.");
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

        private static string FindFolder(List<string> unzip, string companyName, string year, string virtualFolderPath, string fileName)
        {
            foreach (var file in unzip)
            {
                string yearFilePath = $"C:\\Users\\kevin\\OneDrive\\Desktop\\CainWattersTestClients\\{file}\\Clients\\Clients in Main Office Office - Main BU\\{companyName}\\Managed";
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
            }
            return string.Empty;
        }
    }
}
