namespace FileSorter.Models
{
    public class MissingClients
    {
        public string ClientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool NotInList { get; set; }
        public static MissingClients FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(",");
            MissingClients clients = new MissingClients();
            clients.ClientId = values[0].ToString();
            clients.FirstName = values[1].ToString();
            clients.LastName = values[2].ToString();
            clients.NotInList = values[4] == "TRUE" ? true : false;
            return clients;
        }
    }
}
