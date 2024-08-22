using FileSorter.Models;

namespace FileSorter.Cached.Interfaces
{
    public interface IFileConsolidator
    {
        void ConsolidateFiles(string destinationPath, ArrayOfExportFileMetadata files, string unzipped);
        bool ValidateConsolidatedFiles(string destinationPath, ArrayOfExportFileMetadata files, string unzipped);
    }
}
