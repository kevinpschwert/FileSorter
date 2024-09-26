using FileSorter.Models;
using System.Xml.Serialization;
using System.Xml;
using FileSorter.Data;
using FileSorter.Logging.Interfaces;
using FileSorter.Cached.Interfaces;
using FileSorter.Entities;
using FileSorter.Common;

namespace FileSorter.Helpers
{
    public class XmlParser
    {
        private readonly DBContext _db;
        private readonly ILogging _logging;
        private readonly ICachedService _cachedService;

        public XmlParser(DBContext db, ILogging logging, ICachedService cachedService)
        {
            _db = db;
            _logging = logging;
            _cachedService = cachedService;
        }

        public ArrayOfExportFileMetadata ParseClientXml(string xmlFilePath, string uploadSessionGuid, string xmlFile)
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
                orderedFiles.ToList().ForEach(y => { 
                                                    y.CreateDate = DateTime.Now; 
                                                    y.UploadSessionGuid = uploadSessionGuid;
                                                    y.XMLFIle = xmlFile;
                                                    y.StatusId = (int)Status.InitialLoad;
                });

                _db.BulkInsert(orderedFiles);
                return result;
            }
            catch (Exception ex)
            {
                _logging.Log(ex.Message);
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
