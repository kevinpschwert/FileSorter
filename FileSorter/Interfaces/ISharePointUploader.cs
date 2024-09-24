using FileSorter.Models;

namespace FileSorter.Interfaces
{
    public interface ISharePointUploader
    {
        Task Upload(List<SharePointFileUpload> sharePointFileUploads, string UploadSessionGuid);
    }
}
