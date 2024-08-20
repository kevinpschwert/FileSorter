using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Models;

namespace FileSorter.Helpers
{
    public class UnzipFiles
    {
        private readonly DBContext _db;
        private readonly IConfiguration _configuration;

        public UnzipFiles(DBContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }
        public List<GroupedClientData> ExtractData(List<string> zippedFiles)
        {
            string xmlFile = "C:\\Users\\kevin\\Documents";
            string xmlFilePath = Path.Combine(xmlFile, "SmallClients.xml");
            string destinationPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\SmallData\\ConsolidateData";
            List<string> unzipped = new List<string> { "Unzip1", "Unzip2" };

            // Step 1: Unzip the files
             foreach (var zippedFile in zippedFiles)
             {
                 string zipFilePath = $"C:\\Users\\kevin\\OneDrive\\Desktop\\SmallData\\{zippedFile}.zip";
                 string extractPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\SmallData";
                 Unzipper.UnzipFiles(zipFilePath, extractPath);
             }

            // Step 2: Parse the XML file
            XmlParser xmlParser = new XmlParser(_db);
            List<Files> files = xmlParser.ParseXml(xmlFilePath);

            var groupedClientData = files
              .GroupBy(f => f.ClientName)
              .Select(g => new GroupedClientData
              {
                  ClientName = g.Key,
                  Years = g
                      .GroupBy(f => f.Year).OrderByDescending(f => f.Key)
                      .Select(yg => new Years
                      {
                          Year = yg.Key,
                          ClientFiles = yg.Select(f => f.FileName).ToList()
                      })
                      .ToList()
              })
              .ToList();

            FileConsolidator.ConsolidateFiles( destinationPath, files, zippedFiles);

            return groupedClientData;
        }

        //public List<GroupedData> ExtractDataAgain(List<string> zippedFiles)
        //{
        //    string xmlFile = "C:\\Users\\kevin\\Documents";
        //    string xmlFilePath = Path.Combine(xmlFile, "SmallClientsNew.xml");
        //    string destinationPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\ClientIdData\\ConsolidateData";
        //    List<string> unzipped = new List<string> { "Unzip1", "Unzip2" };

        //    // Step 1: Unzip the files
        //    //foreach (var zippedFile in zippedFiles)
        //    //{
        //    //    string zipFilePath = $"C:\\Users\\kevin\\OneDrive\\Desktop\\ClientIdData\\{zippedFile}.zip";
        //    //    string extractPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\ClientIdData";
        //    //    Unzipper.UnzipFiles(zipFilePath, extractPath);
        //    //}

        //    // Step 2: Parse the XML file
        //    XmlParser xmlParser = new XmlParser(_db);
        //    var files = xmlParser.ParseClientXml(xmlFilePath);

        //    var groupedClientData = files.ClientFiles
        //      .GroupBy(f => f.EntityName)
        //      .Select(g => new GroupedData
        //      {
        //          EntityName = g.Key,
        //          Years = g
        //              .GroupBy(f => f.Year).OrderByDescending(f => f.Key)
        //              .Select(yg => new FolderYears
        //              {
        //                  Year = yg.Key,
        //                  FileName = yg.Select(f => f.FileName).ToList()
        //              })
        //              .ToList()
        //      })
        //      .ToList();

        //    //FileConsolidator.ConsolidateFiles(destinationPath, files, zippedFiles);

        //    return groupedClientData;
        //}


        public List<GroupedData> ExtractClientData(List<string> zippedFiles)
        {
            string xmlFile = "C:\\Users\\kevin\\Documents";
            string xmlFilePath = Path.Combine(xmlFile, "Metadata_1.xml");
            string destinationPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\SmallData\\ConsolidateData";
            List<string> unzipped = new List<string> { "Unzip1", "Unzip2" };

            // Step 1: Unzip the files
            //foreach (var zippedFile in zippedFiles)
            //{
            //    string zipFilePath = $"C:\\Users\\kevin\\OneDrive\\Desktop\\SmallData\\{zippedFile}.zip";
            //    string extractPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\SmallData";
            //    Unzipper.UnzipFiles(zipFilePath, extractPath);
            //}

            // Step 2: Parse the XML file
            XmlParser xmlParser = new XmlParser(_db);
            var files = xmlParser.ParseClientXml(xmlFilePath);


            var groupedClientData = files.ClientFiles
              .GroupBy(f => f.EntityName)
              .Select(g => new GroupedData
              {
                  EntityName = g.Key,
                  Years = g
                      .GroupBy(f => f.FolderName).OrderByDescending(f => f.Key)
                      .Select(yg => new FolderYears
                      {
                          Year = yg.Key,
                          FileName = yg.Select(f => f.FileName).ToList()
                      })
                      .ToList()
              })
              .ToList();

            //FileConsolidator.ConsolidateFilesNew(destinationPath, files, zippedFiles);

            return groupedClientData;
        }

        public async Task<List<GroupedClientData>> ExtractDataNew(List<string> zippedFiles)
        {
            string xmlFile = "C:\\Users\\kevin\\Documents";
            string xmlFilePath = Path.Combine(xmlFile, "SmallClientsNew.xml");
            string destinationPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\NewClientIdData\\ConsolidateData";
            List<string> unzipped = new List<string> { "Unzip1", "Unzip2" };

            // Step 1: Unzip the files
            foreach (var zippedFile in zippedFiles)
            {
                string zipFilePath = $"C:\\Users\\kevin\\OneDrive\\Desktop\\NewClientIdData\\{zippedFile}.zip";
                string extractPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\NewClientIdData";
                Unzipper.UnzipFiles(zipFilePath, extractPath);
            }

            // Step 2: Parse the XML file
            XmlParser xmlParser = new XmlParser(_db);
            var files = xmlParser.ParseXml(xmlFilePath);

            var groupedClientData = files
            .GroupBy(f => f.ClientName)
            .Select(g => new GroupedClientData
            {
                ClientName = g.Key,
                Years = g
                    .GroupBy(f => f.Year).OrderByDescending(f => f.Key)
                    .Select(yg => new Years
                    {
                        Year = yg.Key,
                        ClientFiles = yg.Select(f => f.FileName).ToList()
                    })
                    .ToList()
            })
            .ToList();

            FileConsolidator.ConsolidateFilesNew( destinationPath, files, zippedFiles);
            bool isValid = FileConsolidator.ValidateConsolidatedFiles(destinationPath, files, unzipped);

            if (isValid)
            {
                try
                {
                   await UploadToSharePoint();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    DeleteFolders();
                }
            }

            return groupedClientData;
        }

        public List<GroupedData> ExtractActualData(ClientFileInfo fileInfo)
        {
            string extractPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\CainWattersTestClients";
            string xmlFilePath = Path.Combine(extractPath, $"{fileInfo.Metadata}.xml");
            string destinationPath = Path.Combine(extractPath, "ConsolidateData");
            bool isFileLocked = true;

            // Step 1: Unzip the files
            foreach (var zippedFile in fileInfo.Files)
            {
                try
                {
                    string zipFilePath = $"{extractPath}\\{zippedFile}.zip";
                    FileInfo fileInfor1 = new FileInfo(zipFilePath);
                    Unzipper.UnzipFiles(zipFilePath, extractPath);
                    while (IsFileLocked(fileInfor1)) { }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // Step 2: Parse the XML file
            XmlParser xmlParser = new XmlParser(_db);
            var files = xmlParser.ParseClientXml(xmlFilePath);

            var groupedClientData = files.ClientFiles
              .GroupBy(f => f.EntityName)
              .Select(g => new GroupedData
              {
                  EntityName = g.Key,
                  Years = g
                      .GroupBy(f => f.FolderName).OrderByDescending(f => f.Key)
                      .Select(yg => new FolderYears
                      {
                          Year = yg.Key,
                          FileName = yg.Select(f => f.FileName).ToList()
                      })
                      .ToList()
              })
              .ToList();

            FileConsolidator.ConsolidateActualFiles(destinationPath, files, fileInfo.Files);
            bool isValid = FileConsolidator.ValidateConsolidatedActualFiles(destinationPath, files, fileInfo.Files);

            //if (isValid)
            //{
            //    try
            //    {
            //        await UploadToSharePoint();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //        DeleteFolders();
            //    }
            //}

            return groupedClientData;
        }

        public void DeleteFolders()
        {
            Directory.Delete("C:\\Users\\kevin\\OneDrive\\Desktop\\NewClientIdData\\Unzip2", true);
            Directory.Delete("C:\\Users\\kevin\\OneDrive\\Desktop\\NewClientIdData\\Unzip1", true);
            var di = new DirectoryInfo("C:\\Users\\kevin\\OneDrive\\Desktop\\NewClientIdData\\ConsolidateData");
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }

        public void DeleteActualFolders()
        {
            Directory.Delete("C:\\Users\\kevin\\OneDrive\\Desktop\\CainWattersTestClients\\Test1", true);
            var di = new DirectoryInfo("C:\\Users\\kevin\\OneDrive\\Desktop\\CainWattersTestClients\\ConsolidateData");
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }

        private async Task UploadToSharePoint()
        {
            string siteUrl = "https://zealitconsulltants.sharepoint.com/sites/Test";
            // string siteUrl = "https://zealitconsulltants.sharepoint.com/sites/Test/Shared%20Documents/Forms/AllItems.aspx";
            string username = "KevinSchwert@ZealITConsulltants.onmicrosoft.com";
            string password = "1th@nkGod";
            string localFolderPath = @"C:\Users\kevin\OneDrive\Desktop\NewClientIdData";
            string documentLibraryName = "ConsolidateData";

            SharePointUploader uploader = new SharePointUploader(siteUrl, username, password, _configuration);
            await uploader.UploadFolder(localFolderPath, documentLibraryName);

            Console.WriteLine("All files uploaded successfully.");
        }

        private bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
