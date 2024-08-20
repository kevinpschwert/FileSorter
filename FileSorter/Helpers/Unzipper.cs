using System.IO.Compression;

namespace FileSorter.Helpers
{
    public class Unzipper
    {
        public static void UnzipFiles(string zipFilePath, string extractPath)
        {
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        }
    }
}
