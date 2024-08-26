using FileSorter.Data;
using FileSorter.Entities;
using FileSorter.Logging.Interfaces;

namespace FileSorter.Logging.Repositories
{
    public class Logging : ILogging
    {
        private readonly DBContext _db;

        public Logging(DBContext db)
        {
            _db = db;
        }

        public void Log(string message, string? clientName, string? clientFile)
        {
            _db.ClientLoggings.Add(new ClientLogging
            {
                LoggingMessage = message,
                CreatedDate = DateTime.Now,
                ClientName = clientName,
                ClientFile = clientFile
            });
            _db.SaveChanges();
        }
    }
}
