using FileSorter.Cached.Interfaces;
using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Interfaces;
using FileSorter.Models;
using System.Diagnostics;
using System.IO.Compression;

namespace FileSorter.Helpers
{
    public class UnzipFiles : IUnzipFiles
    {
        private readonly DBContext _db;
        private readonly IConfiguration _configuration;
        private readonly IFileConsolidator _fileConsolidator;

        public UnzipFiles(DBContext db, IConfiguration configuration, IFileConsolidator fileConsolidator)
        {
            _db = db;
            _configuration = configuration;
            _fileConsolidator = fileConsolidator;
        }

        public List<GroupedData> ExtractData(List<string> zipFiles)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string extractPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\ExportClients";
            string destinationPath = Path.Combine(extractPath, "ConsolidateData");
            List<ClientFiles> clientFileList = new List<ClientFiles>();
            var files = new ArrayOfExportFileMetadata();
            IEnumerable<ZipArchiveEntry>? xmlFile = null;

            // Step 1: Unzip the files
            foreach (var zippedFile in zipFiles)
            {
                try
                {
                    string xmlFilePath = string.Empty;
                    string zipFilePath = $"{extractPath}\\{zippedFile}.zip";
                    using var openZip = ZipFile.OpenRead(zipFilePath);
                    xmlFile = openZip.Entries.Where(x => x.Name.Contains("Metadata")) ?? null;
                    FileInfo fileInfor1 = new FileInfo(zipFilePath);
                    Unzipper.UnzipFiles(zipFilePath, extractPath);
                    if (xmlFile != null)
                    {
                        xmlFilePath = Path.Combine(extractPath, $"{xmlFile.FirstOrDefault().FullName}");
                        XmlParser xmlParser = new XmlParser(_db);
                        files = xmlParser.ParseClientXml(xmlFilePath);
                    }
                    else
                    {
                        throw new Exception("There is no XML file in this zipped folder");
                    }

                    _fileConsolidator.ConsolidateFiles(destinationPath, files, zippedFile);
                    bool isValid = _fileConsolidator.ValidateConsolidatedFiles(destinationPath, files, zippedFile);
                    clientFileList.AddRange(files.ClientFiles);                   
                    //while (IsFileLocked(fileInfor1)) { }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    Directory.Delete($"{extractPath}\\{zippedFile}", true);
                    var di = new DirectoryInfo(extractPath);
                    var xmlFileToDelete = di.GetFiles().FirstOrDefault(x => x.Name == xmlFile.FirstOrDefault().FullName);
                    xmlFileToDelete.Delete();
                }
            }

            var groupedClientData = clientFileList
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

            sw.Stop();
            var time = sw.Elapsed.TotalSeconds;
            
            return groupedClientData;
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