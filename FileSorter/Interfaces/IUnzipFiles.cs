using FileSorter.Models;

namespace FileSorter.Interfaces
{
    public interface IUnzipFiles
    {
        List<GroupedData> ExtractData(List<string> zipFiles);
    }
}
