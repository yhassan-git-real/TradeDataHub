using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace TradeDataHub.Core.Logging
{
    /// <summary>
    /// Module-aware logger for skipped datasets, supporting separate logs for Export and Import operations
    /// </summary>
    public static class ModuleSkippedDatasetLogger
    {
        private static readonly Lazy<string> _logDirectory = new Lazy<string>(() =>
        {
            try
            {
                var basePath = Directory.GetCurrentDirectory();
                var cfg = new ConfigurationBuilder().SetBasePath(basePath)
                    .AddJsonFile("Config/database.appsettings.json", optional: false)
                    .Build();
                var dir = cfg["DatabaseConfig:LogDirectory"];
                if (string.IsNullOrWhiteSpace(dir)) return Path.Combine(basePath, "Logs");
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch
            {
                return "Logs";
            }
        });

        /// <summary>
        /// Logs a skipped dataset for Export operations
        /// </summary>
        public static void LogExportSkippedDataset(int combinationNumber, long rowCount, string fromMonth, string toMonth, 
            string hsCode, string product, string iec, string exporterOrImporter, string country, string name, string port, string reason = "RowLimit")
        {
            LogSkippedDataset("Export", combinationNumber, rowCount, fromMonth, toMonth, hsCode, product, iec, exporterOrImporter, country, name, port, reason);
        }

        /// <summary>
        /// Logs a skipped dataset for Import operations
        /// </summary>
        public static void LogImportSkippedDataset(int combinationNumber, long rowCount, string fromMonth, string toMonth, 
            string hsCode, string product, string iec, string exporterOrImporter, string country, string name, string port, string reason = "RowLimit")
        {
            LogSkippedDataset("Import", combinationNumber, rowCount, fromMonth, toMonth, hsCode, product, iec, exporterOrImporter, country, name, port, reason);
        }

        /// <summary>
        /// Logs processing summary for Export operations
        /// </summary>
        public static void LogExportProcessingSummary(int totalCombinations, int filesGenerated, int combinationsSkipped)
        {
            LogProcessingSummary("Export", totalCombinations, filesGenerated, combinationsSkipped);
        }

        /// <summary>
        /// Logs processing summary for Import operations
        /// </summary>
        public static void LogImportProcessingSummary(int totalCombinations, int filesGenerated, int combinationsSkipped)
        {
            LogProcessingSummary("Import", totalCombinations, filesGenerated, combinationsSkipped);
        }

        private static void LogSkippedDataset(string moduleType, int combinationNumber, long rowCount, string fromMonth, string toMonth, 
            string hsCode, string product, string iec, string exporterOrImporter, string country, string name, string port, string reason)
        {
            try
            {
                var logFileName = $"{moduleType}_SkippedDatasets_{DateTime.Now:yyyyMMdd}.txt";
                var logPath = Path.Combine(_logDirectory.Value, logFileName);
                
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SKIPPED DATASET - {moduleType.ToUpper()}");
                logEntry.AppendLine($"Combination Number: {combinationNumber}");
                if (string.Equals(reason, "RowLimit", StringComparison.OrdinalIgnoreCase))
                    logEntry.AppendLine($"Row Count: {rowCount:N0} (Exceeds Excel limit of 1,048,576)");
                else if (string.Equals(reason, "NoData", StringComparison.OrdinalIgnoreCase))
                    logEntry.AppendLine("Row Count: 0 (No data returned)");
                else
                    logEntry.AppendLine($"Row Count: {rowCount:N0}");
                logEntry.AppendLine($"Reason: {reason}");
                logEntry.AppendLine($"Period: {fromMonth} to {toMonth}");
                logEntry.AppendLine($"Filters:");
                logEntry.AppendLine($"  HS Code: {hsCode}");
                logEntry.AppendLine($"  Product: {product}");
                logEntry.AppendLine($"  IEC: {iec}");
                logEntry.AppendLine($"  Party: {exporterOrImporter}");
                logEntry.AppendLine($"  Country: {country}");
                logEntry.AppendLine($"  Name: {name}");
                logEntry.AppendLine($"  Port: {port}");
                logEntry.AppendLine(new string('-', 80));
                
                File.AppendAllText(logPath, logEntry.ToString());
            }
            catch (Exception)
            {
            }
        }
        
        private static void LogProcessingSummary(string moduleType, int totalCombinations, int filesGenerated, int combinationsSkipped)
        {
            try
            {
                var logFileName = $"{moduleType}_SkippedDatasets_{DateTime.Now:yyyyMMdd}.txt";
                var logPath = Path.Combine(_logDirectory.Value, logFileName);
                
                var summary = new StringBuilder();
                summary.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] PROCESSING SUMMARY - {moduleType.ToUpper()}");
                summary.AppendLine($"Total Combinations: {totalCombinations}");
                summary.AppendLine($"Files Generated: {filesGenerated}");
                summary.AppendLine($"Combinations Skipped: {combinationsSkipped}");
                summary.AppendLine($"Success Rate: {((double)filesGenerated / totalCombinations * 100):F1}%");
                summary.AppendLine(new string('=', 80));
                summary.AppendLine();
                
                File.AppendAllText(logPath, summary.ToString());
            }
            catch (Exception)
            {
            }
        }
    }
}
