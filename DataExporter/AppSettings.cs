namespace TradeDataHub
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
    }

    public class FileSettings
    {
        public required string TemplatePath { get; set; }
        public required string OutputDirectory { get; set; }
    }

    public class LoggingSettings
    {
        public required string LogPath { get; set; }
    }
}
