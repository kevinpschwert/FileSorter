using System.Xml.Serialization;
using FileSorter.Entities;

namespace FileSorter.Models
{
    [XmlRoot(ElementName = "ArrayOfExportFileMetadata", Namespace = "http://Pfx.Net/Document/ExportFileMetadata")]
    public class ArrayOfExportFileMetadata
    {
        [XmlElement("ExportFileMetadata")]
        public List<ClientFiles> ClientFiles { get; set; } = new List<ClientFiles>();
    }
}
