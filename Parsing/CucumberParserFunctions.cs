using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CucumberParser.Models;

namespace CucumberParser.Parsing
{
    // Parser functions
    public static class CucumberParserFunctions
    {
        /// <summary>
        /// Parses Cucumber HTML content and returns a report.
        /// </summary>
        /// <param name="htmlContent">The HTML content to parse</param>
        /// <param name="parser">Optional parser implementation (defaults to CucumberHTMLParser)</param>
        /// <returns>Parsed CucumberReport</returns>
        public static CucumberReport ParseCucumberHtml(string htmlContent, IHtmlParser? parser = null)
        {
            parser ??= new CucumberHTMLParser();
            parser.Feed(htmlContent);
            return parser.Report;
        }

        public static Dictionary<string, object?> ParseFilenameMetadata(string filepath)
        {
            // Get just the filename without path
            var filename = Path.GetFileName(filepath);

            // Check if it's a retest file
            var isRetest = filename.Contains(ParsingConstants.FILE_SUFFIX_RETEST);

            // Remove .htm extension but keep (retest) for the report file name
            var basename = filename.Replace(ParsingConstants.FILE_EXTENSION_HTM, "");

            // For parsing, remove (retest) temporarily
            var basenameForParsing = basename.Replace(ParsingConstants.FILE_SUFFIX_RETEST, "");

            // Parse the pattern: region-YYYYDDMM-HHmm
            var parts = basenameForParsing.Split('-');

            var metadata = new Dictionary<string, object?>
            {
                { "region", null },
                { "run_date", null },
                { "run_time", null },
                { "retest", isRetest },
                { "report_file_name", basename }
            };

            if (parts.Length >= 3)
            {
                metadata["region"] = parts[0];
                metadata["run_date"] = parts[1];
                metadata["run_time"] = parts[2];
            }

            return metadata;
        }

        /// <summary>
        /// Parses a Cucumber HTML file and returns a report.
        /// </summary>
        /// <param name="filepath">Path to the HTML file</param>
        /// <param name="debug">Enable debug logging</param>
        /// <param name="fileReader">Optional file reader implementation (defaults to FileReader)</param>
        /// <param name="parser">Optional parser implementation (defaults to CucumberHTMLParser)</param>
        /// <returns>Parsed CucumberReport</returns>
        public static CucumberReport ParseCucumberHtmlFile(
            string filepath,
            bool debug = false,
            IFileReader? fileReader = null,
            IHtmlParser? parser = null)
        {
            fileReader ??= new FileReader();
            parser ??= new CucumberHTMLParser();

            var report = new CucumberReport();

            // Add metadata from filename first
            try
            {
                var metadata = ParseFilenameMetadata(filepath);
                report.Region = metadata["region"]?.ToString();
                report.RunDate = metadata["run_date"]?.ToString();
                report.RunTime = metadata["run_time"]?.ToString();
                report.Retest = metadata["retest"] as bool? ?? false;
                report.ReportFileName = metadata["report_file_name"]?.ToString();
            }
            catch (Exception e)
            {
                if (debug)
                {
                    Console.WriteLine($"Debug: Error parsing filename metadata: {e.Message}");
                }
                report.ValidRun = false;
                return report;
            }

            // Try to parse the HTML content
            try
            {
                var htmlContent = fileReader.ReadAllText(filepath);

                if (debug)
                {
                    Console.WriteLine($"Debug: File size: {htmlContent.Length} bytes");
                    var featureDivMatches = Regex.Matches(htmlContent, @"<div[^>]*class=""[^""]*feature[^""]*""[^>]*>");
                    Console.WriteLine($"Debug: Found {featureDivMatches.Count} feature div(s) in HTML");
                    if (featureDivMatches.Count > 0)
                    {
                        Console.WriteLine($"Debug: First feature div: {featureDivMatches[0].Value}");
                    }
                }

                parser.Feed(htmlContent);
                var parsedReport = parser.Report;

                if (debug)
                {
                    Console.WriteLine($"Debug: Parsed {parsedReport.Features.Count} feature(s)");
                    for (int i = 0; i < parsedReport.Features.Count; i++)
                    {
                        var feat = parsedReport.Features[i];
                        Console.WriteLine($"Debug: Feature {i + 1}: '{feat.FeatureName}' with {feat.Scenarios.Count} scenario(s)");
                        for (int j = 0; j < feat.Scenarios.Count; j++)
                        {
                            var scen = feat.Scenarios[j];
                            Console.WriteLine($"Debug:   Scenario {j + 1}: '{scen.ScenarioName}' (ID: {scen.ScenarioIdNum})");
                        }
                    }
                }

                // Copy parsed data to our report
                report.Duration = parsedReport.Duration;
                report.ScenariosTotal = parsedReport.ScenariosTotal;
                report.ScenariosPassed = parsedReport.ScenariosPassed;
                report.ScenariosFailed = parsedReport.ScenariosFailed;
                report.StepsTotal = parsedReport.StepsTotal;
                report.StepsPassed = parsedReport.StepsPassed;
                report.StepsFailed = parsedReport.StepsFailed;
                report.Features = parsedReport.Features;
            }
            catch (Exception e)
            {
                if (debug)
                {
                    Console.WriteLine($"Debug: Error parsing HTML: {e.Message}");
                    Console.WriteLine(e.StackTrace);
                }
                report.ValidRun = false;
            }

            return report;
        }

        /// <summary>
        /// Finds related Cucumber HTML files (base and retest) for a given basename.
        /// </summary>
        /// <param name="basename">The base filename (with or without .htm extension)</param>
        /// <param name="searchPath">Optional directory to search in</param>
        /// <param name="fileReader">Optional file reader implementation (defaults to FileReader)</param>
        /// <returns>Tuple of (baseFile path, retestFile path) or null if not found</returns>
        public static (string? baseFile, string? retestFile) FindRelatedFiles(
            string basename,
            string? searchPath = null,
            IFileReader? fileReader = null)
        {
            fileReader ??= new FileReader();

            // Remove .htm extension if present
            if (basename.EndsWith(ParsingConstants.FILE_EXTENSION_HTM))
            {
                basename = basename.Substring(0, basename.Length - ParsingConstants.FILE_EXTENSION_HTM.Length);
            }

            var baseFile = $"{basename}{ParsingConstants.FILE_EXTENSION_HTM}";
            var retestFile = $"{basename}{ParsingConstants.FILE_SUFFIX_RETEST}{ParsingConstants.FILE_EXTENSION_HTM}";

            // If search_path provided, prepend it
            if (!string.IsNullOrEmpty(searchPath))
            {
                baseFile = Path.Combine(searchPath, baseFile);
                retestFile = Path.Combine(searchPath, retestFile);
            }

            var baseExists = fileReader.FileExists(baseFile);
            var retestExists = fileReader.FileExists(retestFile);

            return (baseExists ? baseFile : null, retestExists ? retestFile : null);
        }
    }
}
