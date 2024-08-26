namespace FileSorter.Logging.Interfaces
{
    public interface ILogging
    {
        void Log(string message, string? clientName, string? clientFile);
    }
}
