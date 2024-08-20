namespace FileSorter.Models
{
    public class GroupedClientData
    {
        public string ClientName { get; set; }
        public List<Years> Years { get; set; } = new List<Years>();
    }

    public class Years
    {
        public int Year { get; set; }
        public List<string> ClientFiles { get; set; }
    }
}
