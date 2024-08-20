using System.Xml.Serialization;

namespace FileSorter.Models
{
    public class Metadata
    {
        [XmlElement("clientname")]
        public string ClientName { get; set; }

        [XmlElement("clientid")]
        public string ClientId { get; set; }

        [XmlElement("year")]
        public int Year { get; set; }

        [XmlElement("file_name")]
        public string FileName { get; set; }
    }
}
