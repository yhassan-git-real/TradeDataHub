namespace TradeDataHub
{
    public class AppSettings
    {
        public DatabaseSettings Database { get; set; }
        public FileSettings Files { get; set; }
        public LoggingSettings Logging { get; set; }
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string StoredProcedureName { get; set; }
    }

    public class FileSettings
    {
        public string TemplatePath { get; set; }
        public string OutputDirectory { get; set; }
    }

    public class LoggingSettings
    {
        public string LogPath { get; set; }
    }
}
