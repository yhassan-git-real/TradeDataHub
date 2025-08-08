namespace TradeDataHub.Config
{
    public class AppSettings
    {
        public required DatabaseSettings Database { get; set; }
        public required FileSettings Files { get; set; }
        public required LoggingSettings Logging { get; set; }
    }

    public class DatabaseSettings
    {
        public required string ConnectionString { get; set; }
        public required string StoredProcedureName { get; set; }
        public required string ViewName { get; set; }
        public required string OrderByColumn { get; set; }
        public required string WorksheetName { get; set; } 
    }

    public class FileSettings
    {
        public required string OutputDirectory { get; set; }
    }

    public class LoggingSettings
    {
        public required string LogDirectory { get; set; }
        public required string LogFilePrefix { get; set; }
        public required string LogFileExtension { get; set; }
        public int FlushIntervalSeconds { get; set; } = 1;
        public bool EnableDebugLogging { get; set; } = true;
    }
}
