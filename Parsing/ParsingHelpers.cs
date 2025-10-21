using System.Text.RegularExpressions;

namespace CucumberParser.Parsing
{
    // Helper methods for common parsing operations
    public static class ParsingHelpers
    {
        /// <summary>
        /// Removes a prefix from a string if it exists
        /// </summary>
        public static string RemovePrefix(string text, string prefix)
        {
            if (text.StartsWith(prefix))
            {
                return text.Substring(prefix.Length).Trim();
            }
            return text;
        }

        /// <summary>
        /// Extracts count from regex match group
        /// </summary>
        public static int ExtractCount(string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return 0;
        }

        /// <summary>
        /// Parses scenario or step statistics from detail text
        /// </summary>
        public static (int failed, int passed) ParseStatistics(string detailText)
        {
            int failed = 0;
            int passed = 0;

            var failedMatch = Regex.Match(detailText, ParsingConstants.REGEX_FAILED_COUNT);
            if (failedMatch.Success)
            {
                failed = int.Parse(failedMatch.Groups[1].Value);
            }

            var passedMatch = Regex.Match(detailText, ParsingConstants.REGEX_PASSED_COUNT);
            if (passedMatch.Success)
            {
                passed = int.Parse(passedMatch.Groups[1].Value);
            }

            return (failed, passed);
        }
    }
}
