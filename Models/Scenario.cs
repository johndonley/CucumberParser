using System.Collections.Generic;
using System.Linq;
using CucumberParser.Parsing;

namespace CucumberParser.Models
{
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

        /// <summary>
        /// Calculates and sets the scenario status based on step results.
        /// Delegates to ScenarioStatusCalculator for the actual logic.
        /// </summary>
        public void CalculateStatus()
        {
            ScenarioStatus = ScenarioStatusCalculator.CalculateStatus(this);
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
}
