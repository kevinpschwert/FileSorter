using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Interfaces;
using FileSorter.Models;

namespace FileSorter.Helpers
{
    public class UnzipFiles : IUnzipFiles
    {
        private readonly DBContext _db;
        private readonly IConfiguration _configuration;

        public UnzipFiles(DBContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public List<GroupedData> ExtractData(ClientFileInfo fileInfo)
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
                  Clients = g
                  .GroupBy(c => c.Class)
                  .Select(cc => new ClientClass
                  {
                      ClassName = cc.Key,
                      SubClasses = cc.Key != "Permanent" ? new List<SubClass>() :
                                   cc.GroupBy(s => s.Subclass)
                                   .Select(sc => new SubClass
                                   {
                                       SubClassName = sc.Key,
                                       FileName = sc.Select(f => f.FileName).ToList()
                                   }).ToList(),
                      Years = cc.Key == "Permanent" ? new List<FolderYears>() :
                              cc.GroupBy(s => s.Year)
                              .Select(y => new FolderYears
                              {
                                  Year = y.Key.ToString(),
                                  SubClasses = y
                                  .GroupBy(y => y.Subclass)
                                  .Select(sc => new SubClass
                                  {
                                      SubClassName = sc.Key,
                                      FileName = sc.Select(sc => sc.FileName).ToList()
                                  }).ToList()
                              }).ToList()
                  }).ToList()
              })
              .ToList();

            FileConsolidator.ConsolidateFiles(destinationPath, files, fileInfo.Files);
            bool isValid = FileConsolidator.ValidateConsolidatedFiles(destinationPath, files, fileInfo.Files);

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
            Directory.Delete("C:\\Users\\kevin\\OneDrive\\Desktop\\CainWattersTestClients\\Test1", true);
            var di = new DirectoryInfo("C:\\Users\\kevin\\OneDrive\\Desktop\\CainWattersTestClients\\ConsolidateData");
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }

        private async Task UploadToSharePoint()
        {
            string siteUrl = "*INSERT SHAREPOINT SITE URL*";
            string username = "*INSERT USERNAME*";
            string password = "*INSERT PASSWORD*";
            string localFolderPath = @"*INSERT FOLDER PATH*";
            string documentLibraryName = "*INSERT FOLDER NAME*";

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
