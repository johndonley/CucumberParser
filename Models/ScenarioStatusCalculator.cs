using CucumberParser.Parsing;

namespace CucumberParser.Models
{
    // Service class for calculating scenario status based on step results
    public static class ScenarioStatusCalculator
    {
        /// <summary>
        /// Calculates the overall status of a scenario based on its steps.
        /// If any step failed or was skipped, the scenario is considered failed.
        /// </summary>
        public static string CalculateStatus(Scenario scenario)
        {
            // If any step failed or skipped, scenario is failed
            foreach (var step in scenario.Steps)
            {
                if (!string.IsNullOrEmpty(step.StepStatus) &&
                    (step.StepStatus.ToLower().Contains(ParsingConstants.STATUS_FAILED) ||
                     step.StepStatus.ToLower().Contains(ParsingConstants.STATUS_SKIPPED)))
                {
                    return ParsingConstants.STATUS_FAILED;
                }
            }

            // All steps passed
            return ParsingConstants.STATUS_PASSED;
        }
    }
}
