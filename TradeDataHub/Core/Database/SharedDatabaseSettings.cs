namespace TradeDataHub.Core.Database
{
    public class SharedDatabaseSettingsRoot
    {
        public required SharedDatabaseSettings DatabaseConfig { get; set; }
    }

    public class SharedDatabaseSettings
    {
        public required string ConnectionString { get; set; }
        public required string LogDirectory { get; set; }
        public int CommandTimeoutSeconds { get; set; } = 3600; // Default 60 minutes for long-running operations
    }
}
