using System.Collections.Generic;
using System.Linq;

namespace CucumberParser.Models
{
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
}
