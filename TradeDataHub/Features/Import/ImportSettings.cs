namespace TradeDataHub.Features.Import
{
    public class ImportSettingsRoot
    {
        public required ImportSettings ImportSettings { get; set; }
    }

    public class ImportSettings
    {
        public required ImportDatabaseSettings Database { get; set; }
        public required ImportFileSettings Files { get; set; }
        public required ImportLoggingSettings Logging { get; set; }
    }

    public class ImportDatabaseSettings
    {
        public required string StoredProcedureName { get; set; }
        public required string ViewName { get; set; }
        public required string OrderByColumn { get; set; }
        public string WorksheetName { get; set; } = "Import Data";
    }

    public class ImportFileSettings
    {
        public required string OutputDirectory { get; set; }
        public string FileSuffix { get; set; } = "IMP";
    }

    public class ImportLoggingSettings
    {
        public string OperationLabel { get; set; } = "Excel Import Generation";
    public string LogFilePrefix { get; set; } = "ImportLog";
    public string LogFileExtension { get; set; } = ".txt";
    }
}
