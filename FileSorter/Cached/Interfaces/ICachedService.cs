using FileSorter.Entities;

namespace FileSorter.Cached.Interfaces
{
    public interface ICachedService
    {
        List<FolderMapping> FolderMapping { get; set; }
        List<Clients> Clients{ get; set; }
        List<SharePointFolders> SharePointsFolders { get; set; }
    }
}
