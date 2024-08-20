using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSorter.Entities
{
    [Table("Files")]
    public class Files
    {
        [Key]
        public long FieldId { get; set; }
        public string ClientName { get; set; }
        public string EntityId { get; set; }
        public long ClientId { get; set; }
        public int Year { get; set; }
        public string FileName { get; set; }
    }
}
