# Cucumber HTML Parser - C# Version

This is a C# conversion of the Python `cucumber_parser.py` program. It parses Ruby Cucumber (v3.2.0) HTML test output files and extracts test metrics.

## Requirements

- .NET 6.0 or higher
- HtmlAgilityPack NuGet package (automatically installed when building)

## Building

```bash
dotnet build CucumberParser.csproj
```

## Running

```bash
# Build and run with dotnet run
dotnet run --project CucumberParser.csproj -- [arguments]

# Or run the compiled executable directly
dotnet CucumberParser.dll [arguments]

# Or on Windows after publishing:
CucumberParser.exe [arguments]
```

## Usage Examples

```bash
# Parse single file from current directory
dotnet run -- prod-20252008-1012.htm

# Parse files from archive directory (dev environment - default)
dotnet run -- prod-20252008-1012 --env dev

# Parse files from archive directory (prod environment)
dotnet run -- prod-20252008-1012 --env prod

# Parse from custom directory
dotnet run -- prod-20252008-1012 --dir /path/to/files

# Output as JSON
dotnet run -- prod-20252008-1012 --format json

# Extract specific fields
dotnet run -- prod-20252008-1012 --field duration
dotnet run -- prod-20252008-1012 --field scenarios_total --field steps_failed

# Show help
dotnet run -- --help
```

## Key Differences from Python Version

1. **HTML Parsing Library**: Uses HtmlAgilityPack instead of Python's HTMLParser
2. **Property Naming**: C# uses PascalCase for properties (e.g., `StepName` instead of `step_name`)
3. **JSON Output**: Uses System.Text.Json for JSON serialization
4. **Command-line Parsing**: Uses manual parsing instead of argparse (to keep dependencies minimal)

## Features

- Parses Cucumber HTML test reports
- Extracts test metrics (scenarios, steps, duration)
- Supports both base and retest files
- Hierarchical data structure: Run -> Features -> Scenarios -> Steps
- Multiple output formats (text, JSON)
- Field-specific extraction
- Debug mode for troubleshooting

## Output Format

The program maintains the same output format as the Python version, including:

- Run-level metadata (region, date, time, retest flag)
- Test metrics (scenarios, steps, pass/fail counts)
- Hierarchical feature/scenario/step data
- Identical JSON structure for compatibility

## API Usage

The C# version can also be used as a library:

```csharp
using CucumberParser;

// Parse HTML string
var html = File.ReadAllText("report.htm");
var report = CucumberParserFunctions.ParseCucumberHtml(html);

// Parse HTML file
var report = CucumberParserFunctions.ParseCucumberHtmlFile("path/to/report.htm");

// Get specific metrics
var duration = CucumberParserFunctions.GetDuration(html);
var totalScenarios = CucumberParserFunctions.GetScenariosTotal(html);
var failedSteps = CucumberParserFunctions.GetStepsFailed(html);

// Access report data
Console.WriteLine($"Duration: {report.Duration}");
Console.WriteLine($"Scenarios: {report.ScenariosTotal}");
foreach (var feature in report.Features)
{
    Console.WriteLine($"Feature: {feature.FeatureName}");
    foreach (var scenario in feature.Scenarios)
    {
        Console.WriteLine($"  Scenario: {scenario.ScenarioName} - {scenario.ScenarioStatus}");
    }
}
```

## Publishing (Optional)

To create a standalone executable:

```bash
# Publish for Windows
dotnet publish -c Release -r win-x64 --self-contained true

# Publish for Linux
dotnet publish -c Release -r linux-x64 --self-contained true

# Publish for macOS
dotnet publish -c Release -r osx-x64 --self-contained true
```

The executable will be in `bin/Release/net6.0/[runtime]/publish/`
