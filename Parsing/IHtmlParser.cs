using CucumberParser.Models;

namespace CucumberParser.Parsing
{
    /// <summary>
    /// Interface for parsing Cucumber HTML reports.
    /// Enables dependency injection and unit testing by abstracting HTML parsing logic.
    /// </summary>
    public interface IHtmlParser
    {
        /// <summary>
        /// Gets the parsed report after Feed() has been called.
        /// </summary>
        CucumberReport Report { get; }

        /// <summary>
        /// Parses HTML content and populates the Report property.
        /// </summary>
        /// <param name="htmlContent">The HTML content to parse</param>
        void Feed(string htmlContent);
    }
}
