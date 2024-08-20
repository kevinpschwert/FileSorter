namespace FileSorter.Models
{
    public class GroupedData
    {
        public string EntityName { get; set; }
        public List<FolderYears> Years { get; set; } = new List<FolderYears>();
    }

    public class FolderYears
    {
        public string Year { get; set; }
        public List<string> FileName { get; set; }
    }
}

