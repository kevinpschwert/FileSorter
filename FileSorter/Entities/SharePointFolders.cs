using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSorter.Entities
{
    [Table("SharePointFolders")]
    public class SharePointFolders
    {
        [Key]
        public long SharePointFolderId { get; set; }
        public string Client { get; set; }
        public string ClientFolderId { get; set; }
    }
}
