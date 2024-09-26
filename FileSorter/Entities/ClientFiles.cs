using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace FileSorter.Entities
{
    [Table("ClientFiles")]
    public class ClientFiles
    {
        [Key]
        public long ClientFilesId { get; set; }
        [XmlElement(ElementName = "Mode", Namespace = "http://schemas.cch.com/ProSystemfx/FAM/EntityObject")]
        public string Mode { get; set; }
        public string BusinessUnit { get; set; }
        public Guid BusinessUnitGuid { get; set; }
        public int BusinessUnitIntID { get; set; }
        [XmlElement(IsNullable = true)]
        public DateTime? CheckInDate { get; set; }
        [XmlElement(IsNullable = true)]
        public DateTime? CheckOutDate { get; set; }
        public string CheckedOutUser { get; set; }
        public string CheckedOutUserName { get; set; }
        public string Class { get; set; }
        public int ClassIntID { get; set; }
        public string ClientID { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByUser { get; set; }
        public string CustomFolderName { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public int DaysOverDue { get; set; }
        public string DocumentType { get; set; }
        public Guid EntityGuid { get; set; }
        public string EntityID { get; set; }
        public string EntityImage { get; set; }
        public int EntityIntID { get; set; }
        public string EntityName { get; set; }
        public string EntityType { get; set; }
        [XmlElement(IsNullable = true)]
        public DateTime? ExpectedCheckinDate { get; set; }
        [XmlElement(IsNullable = true)]
        public DateTime? ExpirationDate { get; set; }
        public string FileDiscussion { get; set; }
        public Guid FileGuid { get; set; }
        public string FileID { get; set; }
        public long FileIntID { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int FirmIntID { get; set; }
        public Guid FolderGuid { get; set; }
        public string FolderName { get; set; }
        public int FolderYearIntID { get; set; }
        [XmlElement(IsNullable = true)]
        public string? FullTextValue { get; set; }
        public bool HasClientPortal { get; set; }
        public string? ImageType { get; set; }
        public string? Keywords { get; set; }
        public string? LinkType { get; set; }
        public string? ModifiedBy { get; set; }
        public string? ModifiedByUser { get; set; }
        public string? NewDiscussionCount { get; set; }
        public string Office { get; set; }
        public Guid OfficeGuid { get; set; }
        public int OfficeIntID { get; set; }
        public bool Permanent { get; set; }
        public string PhysicalFileName { get; set; }
        public string Publish { get; set; }
        public string SensitivityLevel { get; set; }
        public int Size { get; set; }
        public string Source { get; set; }
        public string Status { get; set; }
        public string StatusImage { get; set; }
        public string StorageCategory { get; set; }
        public int SubClassIntID { get; set; }
        public string Subclass { get; set; }
        public string Type { get; set; }
        public int Version { get; set; }
        public string VirtualFolderPath { get; set; }
        public int Year { get; set; }
        [XmlElement(IsNullable = true)]
        public int? YearIntID { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        [ForeignKey("StatusId")]
        public long StatusId { get; set; } = 1;
        [ForeignKey("FolderMappingId")]
        public long FolderMappingId { get; set; } = 99;
        public string UploadSessionGuid { get; set; }
        public string XMLFIle { get; set; }
        public string? DriveFilePath { get; set; }
        public string? SharePointFilePath { get; set; }
    }
}