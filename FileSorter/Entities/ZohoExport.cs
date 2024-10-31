using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FileSorter.Entities
{
    [Table("ZohoExport")]
    public class ZohoExport
    {
        [Key]
        public long ZohoExportId { get; set; }
        public string? ZohoId { get; set; }
        public string ClientName { get; set; }
        public string? CWAId { get; set; }
    }
}
