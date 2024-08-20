using FileSorter.Models;

namespace FileSorter.Interfaces
{
    public interface IUnzipFiles
    {
        List<GroupedData> ExtractData(ClientFileInfo fileInfo);
        public void DeleteFolders();
    }
}
