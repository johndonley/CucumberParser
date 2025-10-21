using System.Collections.Generic;
using System.Linq;

namespace CucumberParser.Models
{
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
}
