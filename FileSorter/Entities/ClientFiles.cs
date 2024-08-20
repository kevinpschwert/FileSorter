using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace FileSorter.Entities
{
    //public class ExportFileMetadata
    //{
    //[XmlElement("BusinessUnit")]
    //public string? BusinessUnit { get; set; }


    //[XmlElement("BusinessUnitGuid")]
    //public Guid? BusinessUnitGuid { get; set; }


    //[XmlElement("BusinessUnitIntID")]
    //public int? BusinessUnitIntID { get; set; }


    //[XmlElement("CheckInDate")]
    //public string? CheckInDate { get; set; }


    //[XmlElement("CheckOutDate")]
    //public string? CheckOutDate { get; set; }


    //[XmlElement("CheckedOutUser")]
    //public string? CheckedOutUser { get; set; }


    //[XmlElement("CheckedOutUserName")]
    //public string? CheckedOutUserName { get; set; }


    //[XmlElement("Class")]
    //public string? Class { get; set; }


    //[XmlElement("ClassIntID")]
    //public int? ClassIntID { get; set; }


    //[XmlElement("ClientID")]
    //public int? ClientID { get; set; }


    //[XmlElement("CreatedBy")]
    //public int? CreatedBy { get; set; }
    //}
    //}

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
        public string FullTextValue { get; set; }
        public bool HasClientPortal { get; set; }
        public string ImageType { get; set; }
        public string Keywords { get; set; }
        public string LinkType { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedByUser { get; set; }
        public string NewDiscussionCount { get; set; }
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
    }
}