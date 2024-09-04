namespace FileSorter.Interfaces
{
    public interface IValidateClients
    {
        List<string> FindMissingClients(List<string> zipFiles);
    }
}
