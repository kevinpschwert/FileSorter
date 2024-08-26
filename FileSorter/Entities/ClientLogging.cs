using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSorter.Entities
{
    [Table("ClientLogging")]
    public class ClientLogging
    {
        [Key]
        public long LoggingId { get; set; }
        public string LoggingMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ClientName { get; set; }
        public string? ClientFile { get; set; }
    }
}
