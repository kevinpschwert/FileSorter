using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSorter.Entities
{
    [Table("UploadSession")]
    public class UploadSession
    {
        [Key]
        public long UploadSessionId { get; set; }
        public string UploadSessionGuid { get; set; }
        public string XMLFile { get; set; }
    }
}
