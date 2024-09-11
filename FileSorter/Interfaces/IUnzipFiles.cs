using FileSorter.Models;

namespace FileSorter.Interfaces
{
    public interface IUnzipFiles
    {
        Task<List<GroupedData>> ExtractData(List<string> zipFiles);
    }
}
