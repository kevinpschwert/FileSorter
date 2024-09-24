namespace FileSorter.Models
{
    public class SharePointFileUpload
    {
        public long FileIntId { get; set; }
        public string DriveFilePath { get; set; }
        public string SharePointFilePath { get; set; }
        public string? FileName { get; set; }
        public string ClientName { get; set; }
        public string UploadSessionGuid { get; set; }
    }
}
