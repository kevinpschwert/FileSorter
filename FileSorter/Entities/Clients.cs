using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FileSorter.Entities
{
    [Table("Clients")]
    public class Clients
    {
        [Key]
        public long ClientId { get; set; }
        public string ClientName { get; set; }
    }
}
