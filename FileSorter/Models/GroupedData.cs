namespace FileSorter.Models
{
    public class GroupedData
    {
        public string EntityName { get; set; }
        public List<ClientClass> Clients { get; set; } = new List<ClientClass>();
    }

    public class ClientClass
    {
        public string ClassName { get; set; }
        public List<FolderYears>? Years { get; set; } = new List<FolderYears>();
        public List<SubClass>? SubClasses { get; set; } = new List<SubClass>();
    }

    public class FolderYears
    {
        public string Year { get; set; }
        public List<SubClass> SubClasses { get; set; } = new List<SubClass>();
    }

    public class SubClass
    {
        public string SubClassName { get; set; }
        public List<string> FileName { get; set; }
    }
}

