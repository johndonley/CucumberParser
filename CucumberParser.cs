/*
 * Utility program for parsing Ruby Cucumber (v3.2.0) HTML test output files.
 * Uses HtmlAgilityPack for HTML parsing.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CucumberParser
{
    // Directory configuration
    public static class DirectoryConfig
    {
        public const string DEV_BASE_DIR = "C:/Dev-Ruby/TestReports/PRAPay-UK/";
        public const string PROD_BASE_DIR = "//TestReports/PRAPay-UK/";
        public const string ARCHIVE_DIR = "archive";
        public const string PROCESSED_DIR = "PROCESSED";
        public const string COMPLETE_DIR = "COMPLETE";
        public const string STATS_DIR = "stats";

        public static string GetBaseDir(string environment = "dev")
        {
            if (environment.ToLower() == "prod")
                return PROD_BASE_DIR;
            return DEV_BASE_DIR;
        }

        public static string GetArchivePath(string environment = "dev")
        {
            return Path.Combine(GetBaseDir(environment), ARCHIVE_DIR);
        }

        public static string GetProcessedPath(string environment = "dev")
        {
            return Path.Combine(GetBaseDir(environment), PROCESSED_DIR);
        }

        public static string GetCompletePath(string environment = "dev")
        {
            return Path.Combine(GetBaseDir(environment), COMPLETE_DIR);
        }

        public static string GetStatsPath(string environment = "dev")
        {
            return Path.Combine(GetBaseDir(environment), STATS_DIR);
        }
    }

    // Constants for parsing and status values
    public static class ParsingConstants
    {
        // Status values
        public const string STATUS_PASSED = "passed";
        public const string STATUS_FAILED = "failed";
        public const string STATUS_SKIPPED = "skipped";
        public const string STATUS_PENDING = "pending";
        public const string STATUS_UNDEFINED = "undefined";

        public static readonly string[] VALID_STATUSES = new[]
        {
            STATUS_PASSED, STATUS_FAILED, STATUS_SKIPPED, STATUS_PENDING, STATUS_UNDEFINED
        };

        // HTML element IDs and selectors
        public const string XPATH_FEATURE_DIVS = "//div[contains(@class, 'feature')]";
        public const string XPATH_SCENARIO_DIVS = ".//div[contains(@class, 'scenario')]";
        public const string XPATH_FEATURE_NAME = ".//h2//span[@class='val']";
        public const string XPATH_SCENARIO_FILE = ".//span[@class='scenario_file']";
        public const string XPATH_SCENARIO_TAG = ".//span[@class='tag']";
        public const string XPATH_SCENARIO_H3 = ".//h3[starts-with(@id, 'scenario_')]";
        public const string XPATH_SCENARIO_NAME = ".//span[@class='val']";
        public const string XPATH_STEP_LIS = ".//li[contains(@class, 'step')]";
        public const string XPATH_STEP_NAME_DIV = ".//div[@class='step_name']";
        public const string XPATH_STEP_KEYWORD = ".//span[contains(@class, 'keyword')]";
        public const string XPATH_STEP_VAL = ".//span[contains(@class, 'val')]";
        public const string XPATH_STEP_FILE = ".//div[@class='step_file']//span";
        public const string XPATH_SCRIPT_TAGS = "//script";
        public const string XPATH_DURATION_P = "//p[@id='duration']";
        public const string XPATH_TOTALS_P = "//p[@id='totals']";
        public const string XPATH_DURATION_STRONG = ".//strong";

        // Regex patterns
        public const string REGEX_GET_ELEMENT_BY_ID = @"getElementById\(['""]({0})['""]\)\.innerHTML\s*=\s*['""]([^'""]+)['""]";
        public const string REGEX_SCENARIOS = @"(\d+)\s+scenarios?\s*\((.*?)\)";
        public const string REGEX_STEPS = @"(\d+)\s+steps?\s*\((.*?)\)";
        public const string REGEX_FAILED_COUNT = @"(\d+)\s+failed";
        public const string REGEX_PASSED_COUNT = @"(\d+)\s+passed";
        public const string REGEX_STRONG_TAG = @"<strong>([^<]+)</strong>";

        // Prefixes to remove
        public const string PREFIX_FEATURE = "Feature:";
        public const string PREFIX_SCENARIO_OUTLINE = "Scenario Outline:";
        public const string PREFIX_SCENARIO = "Scenario:";

        // HTML element IDs
        public const string ELEMENT_ID_TOTALS = "totals";
        public const string ELEMENT_ID_DURATION = "duration";

        // File extension
        public const string FILE_EXTENSION_HTM = ".htm";
        public const string FILE_SUFFIX_RETEST = "(retest)";

        // HTML tag replacements
        public const string HTML_BR = "<br>";
        public const string HTML_BR_SLASH = "<br />";
        public const string NEWLINE = "\n";

        // Duration suffix
        public const string DURATION_SUFFIX = " seconds";
    }

    // Class to hold step-level data
    public class Step
    {
        public string? StepName { get; set; }
        public string? StepStatus { get; set; }
        public string? StepFile { get; set; }

        public Dictionary<string, object?> ToDict()
        {
            return new Dictionary<string, object?>
            {
                { "step_name", StepName },
                { "step_status", StepStatus },
                { "step_file", StepFile }
            };
        }
    }

    // Class to hold scenario-level data
    public class Scenario
    {
        public string? ScenarioIdNum { get; set; }
        public string? ScenarioName { get; set; }
        public string? ScenarioStatus { get; set; }
        public string? ScenarioFile { get; set; }
        public string? ScenarioTag { get; set; }
        public List<Step> Steps { get; set; } = new List<Step>();

        public void AddStep(Step step)
        {
            Steps.Add(step);
        }

        public void CalculateStatus()
        {
            // If any step failed or skipped, scenario is failed
            foreach (var step in Steps)
            {
                if (!string.IsNullOrEmpty(step.StepStatus) &&
                    (step.StepStatus.ToLower().Contains(ParsingConstants.STATUS_FAILED) ||
                     step.StepStatus.ToLower().Contains(ParsingConstants.STATUS_SKIPPED)))
                {
                    ScenarioStatus = ParsingConstants.STATUS_FAILED;
                    return;
                }
            }
            // All steps passed
            ScenarioStatus = ParsingConstants.STATUS_PASSED;
        }

        public Dictionary<string, object?> ToDict()
        {
            return new Dictionary<string, object?>
            {
                { "scenario_id_num", ScenarioIdNum },
                { "scenario_name", ScenarioName },
                { "scenario_status", ScenarioStatus },
                { "scenario_file", ScenarioFile },
                { "scenario_tag", ScenarioTag },
                { "steps", Steps.Select(s => s.ToDict()).ToList() }
            };
        }
    }

    // Class to hold feature-level data
    public class Feature
    {
        public string? FeatureName { get; set; }
        public List<Scenario> Scenarios { get; set; } = new List<Scenario>();

        public void AddScenario(Scenario scenario)
        {
            Scenarios.Add(scenario);
        }

        public Dictionary<string, object?> ToDict()
        {
            return new Dictionary<string, object?>
            {
                { "feature_name", FeatureName },
                { "scenarios", Scenarios.Select(s => s.ToDict()).ToList() }
            };
        }
    }

    // Class to hold parsed Cucumber report data with hierarchical structure
    public class CucumberReport
    {
        public string? Region { get; set; }
        public string? RunDate { get; set; }
        public string? RunTime { get; set; }
        public bool Retest { get; set; }
        public string? ReportFileName { get; set; }
        public bool ValidRun { get; set; } = true;
        public string? Duration { get; set; }
        public int ScenariosTotal { get; set; }
        public int ScenariosPassed { get; set; }
        public int ScenariosFailed { get; set; }
        public int StepsTotal { get; set; }
        public int StepsPassed { get; set; }
        public int StepsFailed { get; set; }
        public List<Feature> Features { get; set; } = new List<Feature>();

        public void AddFeature(Feature feature)
        {
            Features.Add(feature);
        }

        public Dictionary<string, object?> ToDict()
        {
            var run = new Dictionary<string, object?>
            {
                { "region", Region },
                { "run_date", RunDate },
                { "run_time", RunTime },
                { "retest", Retest },
                { "report_file_name", ReportFileName },
                { "valid_run", ValidRun },
                { "duration", Duration },
                { "scenarios_total", ScenariosTotal },
                { "scenarios_passed", ScenariosPassed },
                { "scenarios_failed", ScenariosFailed },
                { "steps_total", StepsTotal },
                { "steps_passed", StepsPassed },
                { "steps_failed", StepsFailed }
            };

            return new Dictionary<string, object?>
            {
                { "run", run },
                { "features", Features.Select(f => f.ToDict()).ToList() }
            };
        }

        public override string ToString()
        {
            return $"CucumberReport(region='{Region}', run_date='{RunDate}', " +
                   $"run_time='{RunTime}', retest={Retest}, " +
                   $"report_file_name='{ReportFileName}', valid_run={ValidRun}, " +
                   $"duration='{Duration}', " +
                   $"scenarios={ScenariosTotal} " +
                   $"({ScenariosPassed} passed, {ScenariosFailed} failed), " +
                   $"steps={StepsTotal} " +
                   $"({StepsPassed} passed, {StepsFailed} failed), " +
                   $"features={Features.Count})";
        }
    }

    // HTML Parser for Cucumber test reports
    public class CucumberHTMLParser
    {
        public CucumberReport Report { get; private set; } = new CucumberReport();

        public void Feed(string htmlContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Parse duration and totals from script tags first
            ParseScriptTags(doc);

            // Parse features
            var featureDivs = doc.DocumentNode.SelectNodes(ParsingConstants.XPATH_FEATURE_DIVS);
            if (featureDivs != null)
            {
                foreach (var featureDiv in featureDivs)
                {
                    var feature = ParseFeature(featureDiv);
                    if (feature != null && !string.IsNullOrEmpty(feature.FeatureName))
                    {
                        Report.AddFeature(feature);
                    }
                }
            }
        }

        private void ParseScriptTags(HtmlDocument doc)
        {
            var scriptNodes = doc.DocumentNode.SelectNodes(ParsingConstants.XPATH_SCRIPT_TAGS);
            if (scriptNodes != null)
            {
                foreach (var scriptNode in scriptNodes)
                {
                    var scriptContent = scriptNode.InnerText;

                    // Look for: document.getElementById('totals').innerHTML = "..."
                    var totalsPattern = string.Format(ParsingConstants.REGEX_GET_ELEMENT_BY_ID, ParsingConstants.ELEMENT_ID_TOTALS);
                    var totalsMatch = Regex.Match(scriptContent, totalsPattern);
                    if (totalsMatch.Success)
                    {
                        var totalsText = totalsMatch.Groups[2].Value
                            .Replace(ParsingConstants.HTML_BR_SLASH, ParsingConstants.NEWLINE)
                            .Replace(ParsingConstants.HTML_BR, ParsingConstants.NEWLINE);
                        ParseTotalsText(totalsText);
                    }

                    // Look for: document.getElementById('duration').innerHTML = "..."
                    var durationPattern = string.Format(ParsingConstants.REGEX_GET_ELEMENT_BY_ID, ParsingConstants.ELEMENT_ID_DURATION);
                    var durationMatch = Regex.Match(scriptContent, durationPattern);
                    if (durationMatch.Success)
                    {
                        var durationHtml = durationMatch.Groups[2].Value;
                        // Extract text from <strong> tags
                        var strongMatch = Regex.Match(durationHtml, ParsingConstants.REGEX_STRONG_TAG);
                        if (strongMatch.Success)
                        {
                            var durationText = strongMatch.Groups[1].Value.Trim();
                            // Remove 'seconds' suffix if present
                            Report.Duration = durationText.Replace(ParsingConstants.DURATION_SUFFIX, "").Trim();
                        }
                    }
                }
            }

            // Fallback: try parsing from p tags if script parsing didn't work
            if (string.IsNullOrEmpty(Report.Duration))
            {
                var durationNode = doc.DocumentNode.SelectSingleNode(ParsingConstants.XPATH_DURATION_P);
                if (durationNode != null)
                {
                    var strongNode = durationNode.SelectSingleNode(ParsingConstants.XPATH_DURATION_STRONG);
                    if (strongNode != null)
                    {
                        Report.Duration = strongNode.InnerText.Trim();
                    }
                }
            }

            if (Report.ScenariosTotal == 0)
            {
                var totalsNode = doc.DocumentNode.SelectSingleNode(ParsingConstants.XPATH_TOTALS_P);
                if (totalsNode != null)
                {
                    ParseTotalsText(totalsNode.InnerText);
                }
            }
        }

        private void ParseTotalsText(string totalsText)
        {
            // Parse scenarios: "1 scenario (1 passed)"
            var scenarioMatch = Regex.Match(totalsText, ParsingConstants.REGEX_SCENARIOS, RegexOptions.IgnoreCase);
            if (scenarioMatch.Success)
            {
                Report.ScenariosTotal = int.Parse(scenarioMatch.Groups[1].Value);
                var scenarioDetails = scenarioMatch.Groups[2].Value;

                // Extract failed scenarios
                var failedMatch = Regex.Match(scenarioDetails, ParsingConstants.REGEX_FAILED_COUNT);
                if (failedMatch.Success)
                {
                    Report.ScenariosFailed = int.Parse(failedMatch.Groups[1].Value);
                }

                // Extract passed scenarios
                var passedMatch = Regex.Match(scenarioDetails, ParsingConstants.REGEX_PASSED_COUNT);
                if (passedMatch.Success)
                {
                    Report.ScenariosPassed = int.Parse(passedMatch.Groups[1].Value);
                }
            }

            // Parse steps: "4 steps (4 passed)"
            var stepMatch = Regex.Match(totalsText, ParsingConstants.REGEX_STEPS, RegexOptions.IgnoreCase);
            if (stepMatch.Success)
            {
                Report.StepsTotal = int.Parse(stepMatch.Groups[1].Value);
                var stepDetails = stepMatch.Groups[2].Value;

                // Extract failed steps
                var failedMatch = Regex.Match(stepDetails, ParsingConstants.REGEX_FAILED_COUNT);
                if (failedMatch.Success)
                {
                    Report.StepsFailed = int.Parse(failedMatch.Groups[1].Value);
                }

                // Extract passed steps
                var passedMatch = Regex.Match(stepDetails, ParsingConstants.REGEX_PASSED_COUNT);
                if (passedMatch.Success)
                {
                    Report.StepsPassed = int.Parse(passedMatch.Groups[1].Value);
                }
            }
        }

        private Feature ParseFeature(HtmlNode featureDiv)
        {
            var feature = new Feature();

            // Get feature name from h2 > span.val
            var featureNameNode = featureDiv.SelectSingleNode(ParsingConstants.XPATH_FEATURE_NAME);
            if (featureNameNode != null)
            {
                var featureName = featureNameNode.InnerText.Trim();
                // Remove "Feature: " prefix if present
                if (featureName.StartsWith(ParsingConstants.PREFIX_FEATURE))
                {
                    featureName = featureName.Substring(ParsingConstants.PREFIX_FEATURE.Length).Trim();
                }
                feature.FeatureName = featureName;
            }

            // Parse scenarios
            var scenarioDivs = featureDiv.SelectNodes(ParsingConstants.XPATH_SCENARIO_DIVS);
            if (scenarioDivs != null)
            {
                foreach (var scenarioDiv in scenarioDivs)
                {
                    var scenario = ParseScenario(scenarioDiv);
                    if (scenario != null)
                    {
                        scenario.CalculateStatus();
                        feature.AddScenario(scenario);
                    }
                }
            }

            return feature;
        }

        private Scenario ParseScenario(HtmlNode scenarioDiv)
        {
            var scenario = new Scenario();

            // Get scenario file from span.scenario_file (before h3)
            var scenarioFileNode = scenarioDiv.SelectSingleNode(ParsingConstants.XPATH_SCENARIO_FILE);
            if (scenarioFileNode != null)
            {
                scenario.ScenarioFile = scenarioFileNode.InnerText.Trim();
            }

            // Get scenario tag from span.tag
            var scenarioTagNode = scenarioDiv.SelectSingleNode(ParsingConstants.XPATH_SCENARIO_TAG);
            if (scenarioTagNode != null)
            {
                scenario.ScenarioTag = scenarioTagNode.InnerText.Trim();
            }

            // Get scenario ID and name from h3
            var h3Node = scenarioDiv.SelectSingleNode(ParsingConstants.XPATH_SCENARIO_H3);
            if (h3Node != null)
            {
                scenario.ScenarioIdNum = h3Node.GetAttributeValue("id", null);

                var scenarioNameNode = h3Node.SelectSingleNode(ParsingConstants.XPATH_SCENARIO_NAME);
                if (scenarioNameNode != null)
                {
                    var scenarioName = scenarioNameNode.InnerText.Trim();
                    // Remove "Scenario:" or "Scenario Outline:" prefix if present
                    if (scenarioName.StartsWith(ParsingConstants.PREFIX_SCENARIO_OUTLINE))
                    {
                        scenarioName = scenarioName.Substring(ParsingConstants.PREFIX_SCENARIO_OUTLINE.Length).Trim();
                    }
                    else if (scenarioName.StartsWith(ParsingConstants.PREFIX_SCENARIO))
                    {
                        scenarioName = scenarioName.Substring(ParsingConstants.PREFIX_SCENARIO.Length).Trim();
                    }
                    scenario.ScenarioName = scenarioName;
                }
            }

            // Parse steps
            var stepLis = scenarioDiv.SelectNodes(ParsingConstants.XPATH_STEP_LIS);
            if (stepLis != null)
            {
                foreach (var stepLi in stepLis)
                {
                    var step = ParseStep(stepLi);
                    if (step != null)
                    {
                        scenario.AddStep(step);
                    }
                }
            }

            return scenario;
        }

        private Step? ParseStep(HtmlNode stepLi)
        {
            var step = new Step();

            // Get step status from class attribute
            var classes = stepLi.GetAttributeValue("class", "").Split(' ');
            var status = classes.FirstOrDefault(c => ParsingConstants.VALID_STATUSES.Contains(c));

            if (status == null)
            {
                return null; // Skip steps without valid status
            }

            step.StepStatus = status;

            // Get step name from div.step_name
            var stepNameDiv = stepLi.SelectSingleNode(ParsingConstants.XPATH_STEP_NAME_DIV);
            if (stepNameDiv != null)
            {
                var keywordNode = stepNameDiv.SelectSingleNode(ParsingConstants.XPATH_STEP_KEYWORD);
                var valNode = stepNameDiv.SelectSingleNode(ParsingConstants.XPATH_STEP_VAL);

                var keyword = keywordNode?.InnerText.Trim() ?? "";
                var val = valNode?.InnerText.Trim() ?? "";

                if (!string.IsNullOrEmpty(keyword) && !string.IsNullOrEmpty(val))
                {
                    step.StepName = $"{keyword} {val}";
                }
                else if (!string.IsNullOrEmpty(val))
                {
                    step.StepName = val;
                }
            }

            // Get step file from div.step_file > span
            var stepFileSpan = stepLi.SelectSingleNode(ParsingConstants.XPATH_STEP_FILE);
            if (stepFileSpan != null)
            {
                step.StepFile = stepFileSpan.InnerText.Trim();
            }

            return step;
        }
    }

    // Parser functions
    public static class CucumberParserFunctions
    {
        public static CucumberReport ParseCucumberHtml(string htmlContent)
        {
            var parser = new CucumberHTMLParser();
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

        public static CucumberReport ParseCucumberHtmlFile(string filepath, bool debug = false)
        {
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
                var htmlContent = File.ReadAllText(filepath);

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

                var parser = new CucumberHTMLParser();
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

        public static (string? baseFile, string? retestFile) FindRelatedFiles(string basename, string? searchPath = null)
        {
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

            var baseExists = File.Exists(baseFile);
            var retestExists = File.Exists(retestFile);

            return (baseExists ? baseFile : null, retestExists ? retestFile : null);
        }

        // Convenience functions for direct field access
        public static string? GetDuration(string htmlContent)
        {
            return ParseCucumberHtml(htmlContent).Duration;
        }

        public static int GetScenariosTotal(string htmlContent)
        {
            return ParseCucumberHtml(htmlContent).ScenariosTotal;
        }

        public static int GetScenariosPassed(string htmlContent)
        {
            return ParseCucumberHtml(htmlContent).ScenariosPassed;
        }

        public static int GetScenariosFailed(string htmlContent)
        {
            return ParseCucumberHtml(htmlContent).ScenariosFailed;
        }

        public static int GetStepsTotal(string htmlContent)
        {
            return ParseCucumberHtml(htmlContent).StepsTotal;
        }

        public static int GetStepsPassed(string htmlContent)
        {
            return ParseCucumberHtml(htmlContent).StepsPassed;
        }

        public static int GetStepsFailed(string htmlContent)
        {
            return ParseCucumberHtml(htmlContent).StepsFailed;
        }
    }

    // Main program
    class Program
    {
        static void Main(string[] args)
        {
            // Parse command line arguments
            var file = "";
            var environment = "dev";
            var customDir = "";
            var format = "text";
            var debug = false;
            var fields = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--env" && i + 1 < args.Length)
                {
                    environment = args[++i];
                }
                else if (args[i] == "--dir" && i + 1 < args.Length)
                {
                    customDir = args[++i];
                }
                else if (args[i] == "--format" && i + 1 < args.Length)
                {
                    format = args[++i];
                }
                else if (args[i] == "--debug")
                {
                    debug = true;
                }
                else if (args[i] == "--field" && i + 1 < args.Length)
                {
                    fields.Add(args[++i]);
                }
                else if (args[i] == "--help" || args[i] == "-h")
                {
                    ShowHelp();
                    return;
                }
                else if (!args[i].StartsWith("--"))
                {
                    file = args[i];
                }
            }

            // Declare reports dictionary at the top level
            Dictionary<string, CucumberReport> reports;

            // If no file provided, show example
            if (string.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file provided. Running with example data...\n");
                var exampleHtml = @"
                <div id=""summary"">
                    <p id=""totals"">8 scenarios (2 failed, 6 passed)<br>104 steps (2 failed, 4 skipped, 98 passed)</p>
                    <p id=""duration"">Finished in <strong>9m15.076s seconds</strong></p>
                </div>
                ";
                var report = CucumberParserFunctions.ParseCucumberHtml(exampleHtml);
                reports = new Dictionary<string, CucumberReport> { { "example", report } };
                OutputReports(reports, format, fields);
                return;
            }

            // Determine search directory
            string? searchDir;
            if (!string.IsNullOrEmpty(customDir))
            {
                searchDir = customDir;
                Console.WriteLine($"Using custom directory: {searchDir}");
            }
            else
            {
                searchDir = DirectoryConfig.GetArchivePath(environment);
                Console.WriteLine($"Using {environment.ToUpper()} environment");
                Console.WriteLine($"Archive directory: {searchDir}");
            }

            // Check if search directory exists
            if (!Directory.Exists(searchDir))
            {
                Console.WriteLine($"Warning: Directory does not exist: {searchDir}");
                Console.WriteLine($"Searching in current directory instead.\n");
                searchDir = null;
            }
            else
            {
                Console.WriteLine();
            }

            // Try to find related files
            var (baseFile, retestFile) = CucumberParserFunctions.FindRelatedFiles(file, searchDir);

            if (baseFile == null && retestFile == null)
            {
                Console.Error.WriteLine($"Error: No files found for '{file}'");
                if (!string.IsNullOrEmpty(searchDir))
                {
                    Console.Error.WriteLine($"  Searched in: {searchDir}");
                }
                Console.Error.WriteLine($"  Looking for: {file}.htm or {file}(retest).htm");
                Environment.Exit(1);
            }

            // Parse the files
            reports = new Dictionary<string, CucumberReport>();

            if (baseFile != null)
            {
                var displayPath = baseFile.Replace("\\", "/");
                Console.WriteLine($"Parsing base file: {displayPath}");
                reports["base"] = CucumberParserFunctions.ParseCucumberHtmlFile(baseFile, debug);
                if (!reports["base"].ValidRun)
                {
                    Console.WriteLine($"  Warning: Exception occurred while parsing {displayPath}");
                }
            }

            if (retestFile != null)
            {
                var displayPath = retestFile.Replace("\\", "/");
                Console.WriteLine($"Parsing retest file: {displayPath}");
                reports["retest"] = CucumberParserFunctions.ParseCucumberHtmlFile(retestFile, debug);
                if (!reports["retest"].ValidRun)
                {
                    Console.WriteLine($"  Warning: Exception occurred while parsing {displayPath}");
                }
            }

            Console.WriteLine(); // Empty line after file parsing messages

            OutputReports(reports, format, fields);
        }

        static void ShowHelp()
        {
            Console.WriteLine(@"Parse Cucumber HTML test reports and extract metrics.

Usage: CucumberParser [FILE] [OPTIONS]

Arguments:
  FILE                  Path to Cucumber HTML report file or basename (e.g., prod-20252008-1012)

Options:
  --env ENV             Environment (dev or prod, default: dev). Determines base directory path.
  --dir PATH            Custom directory to search for files (overrides --env)
  --format FORMAT       Output format (text or json, default: text)
  --debug               Enable debug output during parsing
  --field FIELD         Specific field(s) to extract (can be specified multiple times)
                        Valid fields: region, run_date, run_time, retest, report_file_name,
                        valid_run, duration, scenarios_total, scenarios_passed, scenarios_failed,
                        steps_total, steps_passed, steps_failed
  --help, -h            Show this help message

Examples:
  # Parse single file from current directory
  CucumberParser prod-20252008-1012.htm

  # Parse files from archive directory (dev environment - default)
  CucumberParser prod-20252008-1012 --env dev

  # Parse files from archive directory (prod environment)
  CucumberParser prod-20252008-1012 --env prod

  # Parse from custom directory
  CucumberParser prod-20252008-1012 --dir /path/to/files

  # Output as JSON
  CucumberParser prod-20252008-1012 --format json

  # Extract specific fields
  CucumberParser prod-20252008-1012 --field duration
  CucumberParser prod-20252008-1012 --field scenarios_total --field steps_failed
");
        }

        static void OutputReports(Dictionary<string, CucumberReport> reports, string format, List<string> fields)
        {
            if (format == "json")
            {
                if (fields.Count > 0)
                {
                    // Output specific fields for each report
                    var output = new Dictionary<string, Dictionary<string, object?>>();
                    foreach (var kvp in reports)
                    {
                        var fieldData = new Dictionary<string, object?>();
                        foreach (var field in fields)
                        {
                            fieldData[field] = GetFieldValue(kvp.Value, field);
                        }
                        output[kvp.Key] = fieldData;
                    }
                    Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    // Output all fields for each report
                    var output = new Dictionary<string, Dictionary<string, object?>>();
                    foreach (var kvp in reports)
                    {
                        output[kvp.Key] = kvp.Value.ToDict();
                    }
                    Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            else
            {
                // Text format
                foreach (var kvp in reports)
                {
                    if (reports.Count > 1)
                    {
                        Console.WriteLine(new string('=', 60));
                        Console.WriteLine($"=== {kvp.Key.ToUpper()} REPORT ===");
                        Console.WriteLine(new string('=', 60));
                    }

                    var report = kvp.Value;

                    if (fields.Count > 0)
                    {
                        // Output specific fields only
                        foreach (var field in fields)
                        {
                            var value = GetFieldValue(report, field);
                            Console.WriteLine($"{field}: {value}");
                        }
                    }
                    else
                    {
                        // RUN SECTION
                        Console.WriteLine("\n--- RUN DATA ---");
                        Console.WriteLine($"  Region: {report.Region}");
                        Console.WriteLine($"  Run Date: {report.RunDate}");
                        Console.WriteLine($"  Run Time: {report.RunTime}");
                        Console.WriteLine($"  Retest: {report.Retest}");
                        Console.WriteLine($"  Report File Name: {report.ReportFileName}");
                        Console.WriteLine($"  Valid Run: {report.ValidRun}");
                        Console.WriteLine($"  Duration: {report.Duration}");
                        Console.WriteLine($"  Scenarios: {report.ScenariosTotal} total, {report.ScenariosPassed} passed, {report.ScenariosFailed} failed");
                        Console.WriteLine($"  Steps: {report.StepsTotal} total, {report.StepsPassed} passed, {report.StepsFailed} failed");

                        // FEATURES SECTION
                        Console.WriteLine($"\n--- FEATURES ({report.Features.Count}) ---");
                        if (report.Features.Count > 0)
                        {
                            for (int i = 0; i < report.Features.Count; i++)
                            {
                                var feature = report.Features[i];
                                Console.WriteLine($"\n  Feature {i + 1}:");
                                Console.WriteLine($"    Name: {feature.FeatureName}");
                                Console.WriteLine($"    Scenarios: {feature.Scenarios.Count}");

                                // Display scenarios
                                if (feature.Scenarios.Count > 0)
                                {
                                    for (int j = 0; j < feature.Scenarios.Count; j++)
                                    {
                                        var scenario = feature.Scenarios[j];
                                        Console.WriteLine($"\n    Scenario {j + 1}:");
                                        Console.WriteLine($"      ID: {scenario.ScenarioIdNum}");
                                        Console.WriteLine($"      Name: {scenario.ScenarioName}");
                                        Console.WriteLine($"      Tag: {scenario.ScenarioTag}");
                                        Console.WriteLine($"      Status: {scenario.ScenarioStatus}");
                                        Console.WriteLine($"      File: {scenario.ScenarioFile}");
                                        Console.WriteLine($"      Steps: {scenario.Steps.Count}");

                                        // Display steps
                                        if (scenario.Steps.Count > 0)
                                        {
                                            for (int k = 0; k < scenario.Steps.Count; k++)
                                            {
                                                var step = scenario.Steps[k];
                                                Console.WriteLine($"        Step {k + 1}:");
                                                Console.WriteLine($"          Name: {step.StepName}");
                                                Console.WriteLine($"          Status: {step.StepStatus}");
                                                Console.WriteLine($"          File: {step.StepFile}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("  No features found");
                        }
                    }

                    if (reports.Count > 1)
                    {
                        Console.WriteLine(); // Empty line between reports
                    }
                }
            }
        }

        static object? GetFieldValue(CucumberReport report, string field)
        {
            return field switch
            {
                "region" => report.Region,
                "run_date" => report.RunDate,
                "run_time" => report.RunTime,
                "retest" => report.Retest,
                "report_file_name" => report.ReportFileName,
                "valid_run" => report.ValidRun,
                "duration" => report.Duration,
                "scenarios_total" => report.ScenariosTotal,
                "scenarios_passed" => report.ScenariosPassed,
                "scenarios_failed" => report.ScenariosFailed,
                "steps_total" => report.StepsTotal,
                "steps_passed" => report.StepsPassed,
                "steps_failed" => report.StepsFailed,
                _ => null
            };
        }
    }
}
