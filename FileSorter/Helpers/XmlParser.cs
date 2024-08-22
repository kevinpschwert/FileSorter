using FileSorter.Models;
using System.Xml.Serialization;
using System.Xml;
using FileSorter.Data;
using System.Xml.Linq;
using System.Text;
using FileSorter.Entities;

namespace FileSorter.Helpers
{
    public class XmlParser
    {
        private readonly DBContext _db;

        public XmlParser(DBContext db)
        {
            _db = db;
        }        

        public ArrayOfExportFileMetadata ParseClientXml(string xmlFilePath)
        {
            ArrayOfExportFileMetadata result = new ArrayOfExportFileMetadata();
            XmlDocument doc = new XmlDocument();

            doc.Load(xmlFilePath);
            string xmlcontents = doc.InnerXml;
            foreach (var c in ReplaceChars())
            {
                xmlcontents = xmlcontents.Replace(c.Key, c.Value);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ArrayOfExportFileMetadata));

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add("i", "http://www.w3.org/2001/XMLSchema-instance");

            using (StringReader reader = new StringReader(xmlcontents))
            {
                result = (ArrayOfExportFileMetadata)serializer.Deserialize(reader);
            }

            foreach (var file in result.ClientFiles.OrderBy(x => x.EntityName).ThenBy(y => y.Year))
            {
                _db.Add(file);
            }
            _db.SaveChanges();

            return result;
        }

        static T DeserializeXmlString<T>(string xmlString)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringReader reader = new StringReader(xmlString))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        private static Dictionary<string, string> ReplaceChars()
        {
            return new Dictionary<string, string>
            {
                { "?", "_" },
                { "’", "'" },
                { "", "_" },
                { "·", "�" }
            };
        }
    }
}
