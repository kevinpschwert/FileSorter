using FileSorter.Cached.Interfaces;
using FileSorter.Common;
using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Interfaces;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;
using Microsoft.VisualBasic.FileIO;
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
        private string _uploadSessionGuid;

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
            string extractPath = "C:\\Users\\cchdoc\\Desktop\\ExportClients";
            string destinationPath = Path.Combine(extractPath, "ConsolidateData");
            List<ClientFiles> clientFileList = new List<ClientFiles>();
            var files = new ArrayOfExportFileMetadata();
            IEnumerable<ZipArchiveEntry>? xmlFile = null;
            List<SharePointFileUpload> sharePointFileUploads = new List<SharePointFileUpload>();
            _uploadSessionGuid = Guid.NewGuid().ToString();

            foreach (var zippedFile in zipFiles)
            {
                try
                {
                    UploadSession uploadSession = new UploadSession
                    {
                        UploadSessionGuid = _uploadSessionGuid,
                        XMLFile = zippedFile
                    };
                    _db.UploadSessions.Add(uploadSession);
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
                        files = xmlParser.ParseClientXml(xmlFilePath, _uploadSessionGuid, zippedFile);
                    }
                    else
                    {
                        throw new Exception("There is no XML file in this zipped folder");
                    }

                    var filesToUpload = await _fileConsolidator.ConsolidateFiles(destinationPath, files, zippedFile, _uploadSessionGuid);
                    clientFileList.AddRange(files.ClientFiles);
                    await _sharePointUploader.Upload(filesToUpload, _uploadSessionGuid);
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
                  }).ToList(),
                  UploadSessionGuid = _uploadSessionGuid
              })
              .ToList();

            sw.Stop();
            var time = sw.Elapsed.TotalSeconds;

            return groupedClientData;
        }

        public async Task RetryUploadFiles(List<string> files)
        {
            foreach (var file in files)
            {
                var uploadSessionId = _db.UploadSessions.FirstOrDefault(x => x.XMLFile == file).UploadSessionGuid;
                var cf = _db.ClientFiles.ToList();
                var filesToUpload = cf.Where(x => x.UploadSessionGuid == uploadSessionId && x.StatusId == (int)Status.Processed && !string.IsNullOrEmpty(x.SharePointFilePath) && (x.FolderName == FileClass.PERMANENT || (int.TryParse(x.FolderName, out int year) && year >= 2020))).ToList();
                var sharePointFileUpload = filesToUpload.Select(x => new SharePointFileUpload
                {
                    DriveFilePath = x.DriveFilePath,
                    SharePointFilePath = x.SharePointFilePath,
                    FileIntId = x.FileIntID,
                    ClientName = x.EntityName,
                    UploadSessionGuid = x.UploadSessionGuid,
                    FileName = x.FileName
                }).ToList();
                await _sharePointUploader.Upload(sharePointFileUpload, uploadSessionId);
            }
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
            //List<Pod> values = System.IO.File.ReadAllLines("C:\\Users\\cchdoc\\Desktop\\ClientCSV\\NLC_New.csv")
            //                               .Skip(1)
            //                               .Select(v => Pod.FromCsv(v))
            //                               .ToList();

            //foreach (var value in values)
            //{
            //    Clients clients = new Clients
            //    {
            //        ZohoId = value.ZohoId.Replace("zcrm_", string.Empty),
            //        CWAId = value.CWAId,
            //        ClientName = value.Client,
            //        XCMId = value.XCMId
            //    };
            //    _db.Clients.Add(clients);
            //}

            List<Mapping> values = System.IO.File.ReadAllLines("C:\\Users\\cchdoc\\Desktop\\ClientCSV\\MappingAccountType.csv")
                                         .Skip(0)
                                          .Select(v => Mapping.FromCsv(v))
                                          .ToList();

            long counter = 1;

            foreach (var value in values)
            {
                Entities.FolderMapping folderMapping = new Entities.FolderMapping()
                {
                    FolderMappingId = counter,
                    AccountType = value.AccountType,
                    Class = value.Class,
                    Subclass = value.SubClass,
                    Level2 = value.Level2,
                    Level3 = value.Level3,
                    Level4 = value.Level4,
                };
                _db.FolderMappings.Add(folderMapping);
                counter++;
            }

            _db.SaveChanges();
        }
    }

    public class Pod
    {
        public string ZohoId { get; set; }
        public string CWAId { get; set; }
        public string Client { get; set; }
        public string XCMId { get; set; }
        public static Pod FromCsv(string csvLine)
        {
            using (TextFieldParser parser = new TextFieldParser(new StringReader(csvLine)))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                string[] values = parser.ReadFields();
                Pod pod = new Pod();
                pod.ZohoId = values[0].ToString();
                pod.Client = values[1].ToString();
                pod.CWAId = values[2].ToString();
                pod.XCMId = values[4].ToString();
                return pod;
            }
        }
    }

    public class Mapping
    {
        public string Class { get; set; }
        public string SubClass { get; set; }
        public string Level2 { get; set; }
        public string Level3 { get; set; }
        public string? Level4 { get; set; }
        public string? AccountType { get; set; }
        public static Mapping FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(",");
            Mapping folderMapping = new Mapping();
            folderMapping.AccountType = string.IsNullOrEmpty(values[0].ToString()) ? null : values[0].ToString();
            folderMapping.Class = values[1].ToString();
            folderMapping.SubClass = values[2].ToString();
            folderMapping.Level2 = values[3].ToString();
            folderMapping.Level3 = values[4].ToString();
            folderMapping.Level4 = string.IsNullOrEmpty(values[5].ToString()) ? null : values[5].ToString();
            return folderMapping;
        }
    }
}