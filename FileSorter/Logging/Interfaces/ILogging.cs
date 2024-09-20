namespace FileSorter.Logging.Interfaces
{
    public interface ILogging
    {
        void Log(string message, string? clientName = null, string? clientFile = null, string? xmlFile = null);
    }
}
