using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSorter.Entities
{
    [Table("FileStatus")]
    public class FileStatus
    {
        [Key]
        public long StatusId { get; set; }
        public string Status { get; set; }
    }
}
