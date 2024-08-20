using System.Xml.Serialization;

namespace FileSorter.Models
{
    [XmlRoot("clientdata")]
    public class ClientData
    {
        [XmlElement("file")]
        public List<Metadata> Files { get; set; }
    }
}
