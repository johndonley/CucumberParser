using System;
using System.Linq;
using System.Text.RegularExpressions;
using CucumberParser.Models;
using HtmlAgilityPack;

namespace CucumberParser.Parsing
{
    // HTML Parser for Cucumber test reports
    public class CucumberHTMLParser : IHtmlParser
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
                var (failed, passed) = ParsingHelpers.ParseStatistics(scenarioDetails);
                Report.ScenariosFailed = failed;
                Report.ScenariosPassed = passed;
            }

            // Parse steps: "4 steps (4 passed)"
            var stepMatch = Regex.Match(totalsText, ParsingConstants.REGEX_STEPS, RegexOptions.IgnoreCase);
            if (stepMatch.Success)
            {
                Report.StepsTotal = int.Parse(stepMatch.Groups[1].Value);
                var stepDetails = stepMatch.Groups[2].Value;
                var (failed, passed) = ParsingHelpers.ParseStatistics(stepDetails);
                Report.StepsFailed = failed;
                Report.StepsPassed = passed;
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
                feature.FeatureName = ParsingHelpers.RemovePrefix(featureName, ParsingConstants.PREFIX_FEATURE);
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
                    scenarioName = ParsingHelpers.RemovePrefix(scenarioName, ParsingConstants.PREFIX_SCENARIO_OUTLINE);
                    scenarioName = ParsingHelpers.RemovePrefix(scenarioName, ParsingConstants.PREFIX_SCENARIO);
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
}
