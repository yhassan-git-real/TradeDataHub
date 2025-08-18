namespace TradeDataHub.Config
{
    public class PerformanceSettingsRoot
    {
        public required PerformanceSettings PerformanceSettings { get; set; }
    }

    public class PerformanceSettings
    {
        public required ExcelProcessingSettings ExcelProcessing { get; set; }
        public required FileIOSettings FileIO { get; set; }
        public required LoggingSettings Logging { get; set; }
    }

    public class ExcelProcessingSettings
    {
        public int ChunkSize { get; set; } = 25000;
        public int MaxMemoryUsageMB { get; set; } = 2048;
        public bool EnableObjectPooling { get; set; } = true;
        public bool DeferAutoFitColumns { get; set; } = true;
        public double MemoryPressureThreshold { get; set; } = 0.75;
        public bool EnableProgressiveGC { get; set; } = true;
        public int GCCollectionInterval { get; set; } = 50000;
    }

    public class FileIOSettings
    {
        public int BufferSize { get; set; } = 131072;
        public bool EnableCompression { get; set; } = false;
        public bool AsyncFileOperations { get; set; } = true;
        public int TemporaryFileThreshold { get; set; } = 100000;
        public int MemoryStreamThreshold { get; set; } = 75000;
    }

    public class LoggingSettings
    {
        public int BatchSize { get; set; } = 200;
        public int FlushIntervalMs { get; set; } = 3000;
        public bool EnableHighPerformanceMode { get; set; } = true;
        public string LogLevelThreshold { get; set; } = "Info";
        public bool EnableStringPooling { get; set; } = true;
    }
}

