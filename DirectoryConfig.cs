using System.IO;

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
}
