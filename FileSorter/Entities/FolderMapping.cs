using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSorter.Entities
{
    [Table("FolderMapping")]
    public class FolderMapping
    {
        [Key]
        public long FolderMappingId { get; set; }
        public string? Class { get; set; }
        public string? Subclass { get; set; }
        public string Level2 { get; set; }
        public string? Level3 { get; set; }
        public string? Level4 { get; set; }
        public string? AccountType { get; set; }
    }
}
