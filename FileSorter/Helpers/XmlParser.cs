using FileSorter.Models;
using System.Xml.Serialization;
using System.Xml;
using FileSorter.Data;
using System.Xml.Linq;
using System.Text;
using FileSorter.Entities;
using FileSorter.Logging.Interfaces;

namespace FileSorter.Helpers
{
    public class XmlParser
    {
        private readonly DBContext _db;
        private readonly ILogging _logging;

        public XmlParser(DBContext db, ILogging logging)
        {
            _db = db;
            _logging = logging;
        }        

        public ArrayOfExportFileMetadata ParseClientXml(string xmlFilePath)
        {
            try
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

                var orderedFiles = result.ClientFiles.OrderBy(x => x.EntityName).ThenBy(y => y.Year);
                orderedFiles.ToList().ForEach(y => y.CreateDate = DateTime.Now);
                _db.BulkInsert(orderedFiles);

                return result;
            }
            catch (Exception ex)
            {
                _logging.Log(ex.Message, null,null);
                throw;
            }
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
                { "·", "�" },
                { "–", "-" }
            };
        }
    }
}
