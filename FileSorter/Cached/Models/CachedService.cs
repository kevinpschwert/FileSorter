using FileSorter.Cached.Interfaces;
using FileSorter.Entities;

namespace FileSorter.Cached.Models
{
    public class CachedService : ICachedService
    {
        public List<FolderMapping> FolderMapping { get; set; }
        public List<Clients> Clients { get; set; }
        public List<SharePointFolders> SharePointsFolders { get; set; }
    }
}
