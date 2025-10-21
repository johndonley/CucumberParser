using System.IO;

namespace CucumberParser.Parsing
{
    /// <summary>
    /// Default implementation of IFileReader that uses System.IO.File.
    /// This is the production implementation that performs actual file I/O.
    /// </summary>
    public class FileReader : IFileReader
    {
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }
    }
}
