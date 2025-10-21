namespace CucumberParser.Parsing
{
    /// <summary>
    /// Interface for file reading operations.
    /// Enables dependency injection and unit testing by abstracting file I/O.
    /// </summary>
    public interface IFileReader
    {
        /// <summary>
        /// Reads all text from a file at the specified path.
        /// </summary>
        /// <param name="path">The file path to read from</param>
        /// <returns>The contents of the file as a string</returns>
        string ReadAllText(string path);

        /// <summary>
        /// Checks if a file exists at the specified path.
        /// </summary>
        /// <param name="path">The file path to check</param>
        /// <returns>True if the file exists, false otherwise</returns>
        bool FileExists(string path);
    }
}
