using FileSorter.Entities;

namespace FileSorter.Cached.Interfaces
{
    public interface ICachedService
    {
        List<FolderMapping> FolderMapping { get; set; }
        List<ZohoClientIdMapping> ZohoClientIdMappings { get; set; }
    }
}
