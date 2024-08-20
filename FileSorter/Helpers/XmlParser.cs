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

        public List<Files> ParseXml(string xmlFilePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);
            string xmlcontents = doc.InnerText;
            string split = "</clientdata>";
            string splitXml = xmlcontents.Substring(xmlcontents.IndexOf(split) + split.Length);
            var splitArr = xmlcontents.Split(splitXml);
            List<Files> files = new List<Files>();
            ClientData clientDataDeserialize = DeserializeXmlString<ClientData>(splitArr[0]);
            var orderedClients = clientDataDeserialize.Files.OrderBy(f => f.ClientName).ThenBy(y => y.Year).ToList();

            var groupedClient = orderedClients.Select(x => x.ClientName).Distinct().ToList();
            List< Clients> newClients = new List<Clients>();

            foreach (var gc in groupedClient)
            {
                var client = _db.Clients.FirstOrDefault(x => x.ClientName == gc);
                if (client == null)
                {
                    client = new Clients();
                    client.ClientName = gc;
                    _db.Add(client);
                    _db.SaveChanges();
                }
                newClients.Add(client);
            }

            foreach (var fileElement in orderedClients)
            {
                var newFile = new Files
                {
                    ClientId = newClients.FirstOrDefault(x => x.ClientName == fileElement.ClientName).ClientId,
                    ClientName = fileElement.ClientName,
                    EntityId = fileElement.ClientId,
                    Year = fileElement.Year,
                    FileName = fileElement.FileName
                };

                files.Add(newFile);
                _db.Add(newFile);
            }
            _db.SaveChanges();
            return files;
        }

        //public List<Files> ParseXmlNew(string xmlFilePath)
        //{
        //    XmlDocument doc = new XmlDocument();
        //    doc.Load(xmlFilePath);
        //    string xmlcontents = doc.InnerText;
        //    string split = "</clientdata>";
        //    string splitXml = xmlcontents.Substring(xmlcontents.IndexOf(split) + split.Length);
        //    var splitArr = xmlcontents.Split(splitXml);
        //    List<Files> files = new List<Files>();
        //    ClientData clientDataDeserialize = DeserializeXmlString<ClientData>(splitArr[0]);
        //    var orderedClients = clientDataDeserialize.Files.OrderBy(f => f.ClientName).ThenBy(y => y.Year).ToList();

        //    foreach (var fileElement in orderedClients)
        //    {
        //        var newFile = new Files
        //        {
        //            ClientName = fileElement.ClientName,
        //            ClientId = fileElement.ClientId,
        //            Year = fileElement.Year,
        //            FileName = fileElement.FileName
        //        };

        //        files.Add(newFile);
        //        _db.Add(newFile);
        //    }
        //    _db.SaveChanges();
        //    return files;
        //}

        public ArrayOfExportFileMetadata ParseClientXml(string xmlFilePath)
        {
            ArrayOfExportFileMetadata result = new ArrayOfExportFileMetadata();
            XmlDocument doc = new XmlDocument();

            doc.Load(xmlFilePath);
            string xmlcontents = doc.InnerXml;
            xmlcontents = xmlcontents.Replace("?", "_");

            XmlSerializer serializer = new XmlSerializer(typeof(ArrayOfExportFileMetadata));

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add("i", "http://www.w3.org/2001/XMLSchema-instance");

            using (StringReader reader = new StringReader(xmlcontents))
            {
                result = (ArrayOfExportFileMetadata)serializer.Deserialize(reader);
                // Use the deserialized object
            }

            foreach (var file in result.ClientFiles.OrderBy(x => x.EntityName).ThenBy(y => y.Year))
            {
                _db.Add(file);
            }
            _db.SaveChanges();

            return result;
            //var response = XElement.Parse(xmlcontents);
            //
            //doc.LoadXml(xmlcontents);

            //string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            //if (xmlcontents.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
            //{
            //    var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length - 1;
            //    xmlcontents = xmlcontents.Remove(0, _byteOrderMarkUtf8.Length - 1);
            //}

            //string split = "</ArrayOfExportFileMetadata>";
            //string splitXml = xmlcontents.Substring(xmlcontents.IndexOf(split) + split.Length);
            //var splitArr = xmlcontents.Split(splitXml);
            //List<ExportFileMetadata> files = new List<ExportFileMetadata>();
            //ArrayOfExportFileMetadata clientDataDeserialize = DeserializeXmlString<ArrayOfExportFileMetadata>(splitArr[0]);
            ////var orderedClients = clientDataDeserialize.Files.OrderBy(f => f.ClientName).ThenBy(y => y.Year).ToList();

            //foreach (var fileElement in clientDataDeserialize.ExportFileMetadata)
            //{
            //    var newFile = new ExportFileMetadata
            //    {
            //        BusinessUnit = fileElement.BusinessUnit,
            //        BusinessUnitGuid = fileElement.BusinessUnitGuid,
            //        BusinessUnitIntID = fileElement.BusinessUnitIntID,
            //        CheckInDate = fileElement.CheckInDate,
            //        Class = fileElement.Class,
            //        ClassIntID = fileElement.ClassIntID,
            //        CreatedBy = fileElement.CreatedBy,
            //        CheckedOutUser = fileElement.CheckedOutUser,
            //        CheckedOutUserName = fileElement.CheckedOutUserName,
            //        CheckOutDate = fileElement.CheckOutDate,
            //        ClientID = fileElement.ClientID
            //    };

            //    files.Add(newFile);
            //    // _db.Add(newFile);
            //}
            //// _db.SaveChanges();
            //return files;
        }

        static T DeserializeXmlString<T>(string xmlString)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringReader reader = new StringReader(xmlString))
            {
                return (T)serializer.Deserialize(reader);
            }
        }
    }
}
