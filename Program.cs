/*
 * Utility program for parsing Ruby Cucumber (v3.2.0) HTML test output files.
 * Uses HtmlAgilityPack for HTML parsing.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CucumberParser.Models;
using CucumberParser.Parsing;

namespace CucumberParser
{
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
