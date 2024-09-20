using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSorter.Entities
{
    [Table("ZohoClientIdMapping")]
    public class ZohoClientIdMapping
    {
        [Key]
        public long ZohoClientIdMappingId { get; set; }
        public string ZohoId { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
    }
}
