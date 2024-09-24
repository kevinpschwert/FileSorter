using FileSorter.Models;

namespace FileSorter.Cached.Interfaces
{
    public interface IFileConsolidator
    {
        Task<List<SharePointFileUpload>> ConsolidateFiles(string destinationPath, ArrayOfExportFileMetadata files, string unzipped, string UploadSessionGuid);
        bool ValidateConsolidatedFiles(string destinationPath, ArrayOfExportFileMetadata files, string unzipped);
    }
}
