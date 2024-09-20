using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Interfaces;
using FileSorter.Logging.Interfaces;
using FileSorter.Models;
using System.IO.Compression;

namespace FileSorter.Helpers
{
    public class ValidateClients : IValidateClients
    {
        private readonly DBContext _db;
        private readonly ILogging _logging;

        public ValidateClients(DBContext db, ILogging logging)
        {
            _db = db;
            _logging = logging;
        }

        public List<string> FindMissingClients(List<string> zipFiles)
        {
            List<MissingClients> clientCsv = System.IO.File.ReadAllLines("C:\\Users\\cchdoc\\Desktop\\ClientCSV\\NLC.csv")
                                         .Skip(1)
                                         .Select(v => MissingClients.FromCsv(v))
                                          .Where(x => !string.IsNullOrEmpty(x.ClientId) && !x.NotInList)
                                         .ToList();
            string extractPath = "C:\\Users\\cchdoc\\Desktop\\Clients";
            string destinationPath = "C:\\Users\\cchdoc\\Desktop\\ClientCSV";
            string destinationFolder = "C:\\Users\\cchdoc\\Desktop\\ClientCSV\\Clients";
            IEnumerable<ZipArchiveEntry>? xmlFile = null;
            List<string> missingClientsList = new List<string>();
            List<string> clientList = new List<string>();
            foreach (var zippedFile in zipFiles)
            {
                try
                {
                    string xmlFilePath = string.Empty;
                    string zipFilePath = $"{extractPath}\\{zippedFile}.zip";
                    using var openZip = ZipFile.OpenRead(zipFilePath);
                    xmlFile = openZip.Entries.Where(x => x.Name.Contains("Metadata")) ?? null;
                    Unzipper.UnzipFiles(zipFilePath, destinationFolder);
                    string clientFolderPath = $"{destinationPath}\\Clients\\{zippedFile}\\Clients\\Clients in Main Office Office - Main BU";
                    var clientFolders = Directory.GetDirectories(clientFolderPath).Select(Path.GetFileName).ToList();
                    clientList.AddRange(clientFolders);
                }
                catch (Exception ex)
                {
                    _logging.Log(ex.Message);
                }
                finally
                {
                    Directory.Delete($"{destinationFolder}\\{zippedFile}", true);
                    var di = new DirectoryInfo(destinationFolder);
                    var xmlFileToDelete = di.GetFiles().FirstOrDefault(x => x.Name == xmlFile.FirstOrDefault().FullName);
                    xmlFileToDelete.Delete();
                }
            }
            var distinctClients = clientList.Distinct().ToList().OrderBy(x => x); // This is the list we need to validate because it could have missing clients - clientCsv is master list
            foreach (var client in clientCsv)
            {
                if (!distinctClients.Any(y => y.Contains(client.ClientId)) || !distinctClients.Any(y => y.Contains(client.LastName)))
                {
                    missingClientsList.Add($"{client.ClientId} - {client.FirstName} {client.LastName}");
                }
            }
            return missingClientsList;
        }
    }
}
