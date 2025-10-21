using System.Collections.Generic;

namespace CucumberParser.Models
{
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
}
