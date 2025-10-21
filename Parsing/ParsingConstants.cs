namespace CucumberParser.Parsing
{
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
}
