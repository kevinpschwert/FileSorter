using FileSorter.Cached.Interfaces;
using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Interfaces;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;
using System.Diagnostics;
using System.IO.Compression;
using static FileSorter.Common.Constants;

namespace FileSorter.Helpers
{
    public class UnzipFiles : IUnzipFiles
    {
        private readonly DBContext _db;
        private readonly IConfiguration _configuration;
        private readonly IFileConsolidator _fileConsolidator;
        private readonly ILogging _logging;
        private readonly ISharePointUploader _sharePointUploader;
        ICachedService _cachedService;

        public UnzipFiles(DBContext db, IConfiguration configuration, IFileConsolidator fileConsolidator, ILogging logging, ISharePointUploader sharePointUploader, ICachedService cachedService)
        {
            _db = db;
            _configuration = configuration;
            _fileConsolidator = fileConsolidator;
            _logging = logging;
            _sharePointUploader = sharePointUploader;
            _cachedService = cachedService;
        }

        public async Task<List<GroupedData>> ExtractData(List<string> zipFiles)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string extractPath = "C:\\Users\\kevin\\OneDrive\\Desktop\\ExportClients";
            string destinationPath = Path.Combine(extractPath, "ConsolidateData");
            List<ClientFiles> clientFileList = new List<ClientFiles>();
            var files = new ArrayOfExportFileMetadata();
            IEnumerable<ZipArchiveEntry>? xmlFile = null;
            List<SharePointFileUpload> sharePointFileUploads = new List<SharePointFileUpload>();

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
                        XmlParser xmlParser = new XmlParser(_db, _logging, _cachedService);
                        files = xmlParser.ParseClientXml(xmlFilePath);
                    }
                    else
                    {
                        throw new Exception("There is no XML file in this zipped folder");
                    }

                    var filesToUpload = await _fileConsolidator.ConsolidateFiles(destinationPath, files, zippedFile);
                    sharePointFileUploads.AddRange(filesToUpload);
                    bool isValid = _fileConsolidator.ValidateConsolidatedFiles(destinationPath, files, zippedFile);
                    clientFileList.AddRange(files.ClientFiles);
                }
                catch (Exception ex)
                {
                    _logging.Log(ex.Message);
                }
                finally
                {
                    Directory.Delete($"{extractPath}\\{zippedFile}", true);
                    var di = new DirectoryInfo(extractPath);
                    var xmlFileToDelete = di.GetFiles().FirstOrDefault(x => x.Name == xmlFile.FirstOrDefault().FullName);
                    xmlFileToDelete.Delete();
                }
            }

            await _sharePointUploader.Upload(sharePointFileUploads);
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
                      SubClasses = cc.Key != FileClass.PERMANENT ? new List<SubClass>() :
                                   cc.GroupBy(s => s.Subclass)
                                   .Select(sc => new SubClass
                                   {
                                       SubClassName = sc.Key,
                                       FileName = sc.Select(f => f.FileName).ToList()
                                   }).ToList(),
                      Years = cc.Key == FileClass.PERMANENT ? new List<FolderYears>() :
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

            sw.Stop();
            var time = sw.Elapsed.TotalSeconds;

            return groupedClientData;
        }
    }

    public class UploadZohoClientMapping
    {
        DBContext _db;

        public UploadZohoClientMapping(DBContext db)
        {
            _db = db;
        }

        public void UploadCsv()
        {
            List<Pod> values = System.IO.File.ReadAllLines("C:\\Users\\kevin\\OneDrive\\Desktop\\ClientCSV\\Pod5.csv")
                                           .Skip(1)
                                           .Select(v => Pod.FromCsv(v))
                                           .ToList();

            foreach (var value in values)
            {
                ZohoClientIdMapping zohoClientIdMapping = new ZohoClientIdMapping
                {
                    ZohoId = value.ZohoId.Replace("zcrm_", string.Empty),
                    ClientId = value.ClientId,
                    ClientName = value.Client
                };
                _db.ZohoClientIdMappings.Add(zohoClientIdMapping);
            }
            _db.SaveChanges();
        }
    }

    public class Pod
    {
        public string ZohoId { get; set; }
        public string ClientId { get; set; }
        public string Client { get; set; }
        public static Pod FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(",");
            Pod pod = new Pod();
            pod.ZohoId = values[0].ToString();
            pod.ClientId = values[1].ToString();
            pod.Client = values[2].ToString();
            return pod;
        }
    }
}