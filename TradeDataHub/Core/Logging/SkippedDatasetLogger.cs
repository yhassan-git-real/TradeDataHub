using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace TradeDataHub.Core.Logging
{
    public static class SkippedDatasetLogger
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

        private static string CurrentLogFileName => $"SkippedDatasets_{DateTime.Now:yyyyMMdd}.log";
        
        public static void LogSkippedDataset(int combinationNumber, long rowCount, string fromMonth, string toMonth, 
            string hsCode, string product, string iec, string exporterOrImporter, string country, string name, string port, string reason = "RowLimit")
        {
            try
            {
                var logPath = Path.Combine(_logDirectory.Value, CurrentLogFileName);
                
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SKIPPED DATASET");
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
            catch (Exception ex)
            {
                // Don't break the main process if logging fails
                System.Diagnostics.Debug.WriteLine($"Failed to log skipped dataset: {ex.Message}");
            }
        }
        
        public static void LogProcessingSummary(int totalCombinations, int filesGenerated, int combinationsSkipped)
        {
            try
            {
                var logPath = Path.Combine(_logDirectory.Value, CurrentLogFileName);
                
                var summary = new StringBuilder();
                summary.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] PROCESSING SUMMARY");
                summary.AppendLine($"Total Combinations: {totalCombinations}");
                summary.AppendLine($"Files Generated: {filesGenerated}");
                summary.AppendLine($"Combinations Skipped: {combinationsSkipped}");
                summary.AppendLine($"Success Rate: {((double)filesGenerated / totalCombinations * 100):F1}%");
                summary.AppendLine(new string('=', 80));
                summary.AppendLine();
                
                File.AppendAllText(logPath, summary.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to log processing summary: {ex.Message}");
            }
        }
    }
}
