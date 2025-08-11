using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradeDataHub.Core.Helpers;
using Microsoft.Extensions.Configuration;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;

namespace TradeDataHub.Core.Logging
{
    /// <summary>
    /// Module-specific logger that creates separate log files for different modules (Export/Import)
    /// </summary>
    public sealed class ModuleLogger : IDisposable
    {
        private readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _flushSemaphore = new(1, 1);
        private readonly string _logDirectory;
        private readonly string _modulePrefix;
        private readonly string _logFileExtension;
        private readonly int _flushIntervalSeconds;
        private string _currentLogFile = string.Empty;
        private DateTime _currentLogDate = DateTime.MinValue;
        private bool _disposed = false;
        
        // Performance optimization: Cache DateTime.Now for reduced syscalls
        private DateTime _lastTimestampCache = DateTime.Now;
        private long _lastTickCount = Environment.TickCount64;

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? StackTrace { get; set; }
            public string? ProcessId { get; set; }
        }

        private static int _processCounter = 0;

        public ModuleLogger(string modulePrefix, string logFileExtension = ".txt")
        {
            _modulePrefix = modulePrefix ?? throw new ArgumentNullException(nameof(modulePrefix));
            _logFileExtension = logFileExtension.StartsWith('.') ? logFileExtension : "." + logFileExtension;
            
            // Load shared database config for log directory
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder().SetBasePath(basePath)
                .AddJsonFile("Config/database.appsettings.json", optional: false);
            var cfg = builder.Build();
            _logDirectory = cfg["DatabaseConfig:LogDirectory"] ?? Path.Combine(basePath, "Logs");
            Directory.CreateDirectory(_logDirectory);

            _flushIntervalSeconds = 1;
            UpdateLogFileName();
            
            _flushTimer = new Timer(async _ => await FlushLogsAsync(), null,
                TimeSpan.FromSeconds(_flushIntervalSeconds), TimeSpan.FromSeconds(_flushIntervalSeconds));
        }

        public string GenerateProcessId()
        {
            return $"P{Interlocked.Increment(ref _processCounter):D4}";
        }

        // Performance optimization: Get timestamp with reduced DateTime.Now calls
        private DateTime GetOptimizedTimestamp()
        {
            var currentTicks = Environment.TickCount64;
            // Only call DateTime.Now if more than 100ms have passed
            if (currentTicks - _lastTickCount > 100)
            {
                _lastTimestampCache = DateTime.Now;
                _lastTickCount = currentTicks;
            }
            return _lastTimestampCache.AddMilliseconds(currentTicks - _lastTickCount);
        }

        private void UpdateLogFileName()
        {
            var today = _lastTimestampCache.Date; // Use cached timestamp instead of DateTime.Now
            if (_currentLogDate != today)
            {
                _currentLogDate = today;
                _currentLogFile = Path.Combine(_logDirectory, $"{_modulePrefix}_{today:yyyyMMdd}{_logFileExtension}");
            }
        }

        public void LogInfo(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.INFO, message, null, processId);
        }

        public void LogWarning(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.WARNING, message, null, processId);
        }

        public void LogError(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.ERROR, message, null, processId);
        }

        public void LogError(string message, Exception ex, string? processId = null)
        {
            var errorMessage = $"{message} - {ex.Message}";
            EnqueueLog(LogLevel.ERROR, errorMessage, ex.StackTrace, processId);
        }

        public void LogProcessStart(string processName, string parameters, string processId)
        {
            EnqueueLog(LogLevel.INFO, new string('=', 80), null, null);
            EnqueueLog(LogLevel.INFO, $"🚀 PROCESS START: {processName}", null, processId);
            EnqueueLog(LogLevel.INFO, $"📋 Parameters: {parameters}", null, processId);
            EnqueueLog(LogLevel.INFO, new string('-', 80), null, null);
        }

        public void LogProcessComplete(string processName, TimeSpan elapsed, string result, string processId)
        {
            EnqueueLog(LogLevel.INFO, new string('-', 80), null, null);
            EnqueueLog(LogLevel.INFO, $"✅ PROCESS COMPLETE: {processName}", null, processId);
            EnqueueLog(LogLevel.INFO, $"⏱️  Total Time: {elapsed:mm\\:ss\\.fff}", null, processId);
            EnqueueLog(LogLevel.INFO, $"📊 Result: {result}", null, processId);
            EnqueueLog(LogLevel.INFO, new string('=', 80), null, null);
        }

        public void LogStep(string stepName, string details, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"  ➤ {stepName}: {details}", null, processId);
        }

        public void LogDetailedParameters(string fromMonth, string toMonth, string hsCode, string product, 
            string iec, string exporter, string forCount, string forName, string port, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"    📊 Period: {fromMonth} to {toMonth}", null, processId);
            EnqueueLog(LogLevel.INFO, $"    🏷️  HS Code: {hsCode}", null, processId);
            EnqueueLog(LogLevel.INFO, $"    📦 Product: {product}", null, processId);
            EnqueueLog(LogLevel.INFO, $"    🏢 IEC: {iec}", null, processId);
            EnqueueLog(LogLevel.INFO, $"    🏪 Entity: {exporter}", null, processId);
            EnqueueLog(LogLevel.INFO, $"    🌍 Country: {forCount}", null, processId);
            EnqueueLog(LogLevel.INFO, $"    👤 Name: {forName}", null, processId);
            EnqueueLog(LogLevel.INFO, $"    🚢 Port: {port}", null, processId);
        }

        public void LogExcelFileCreationStart(string fileName, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"  📋 Creating Excel file: {fileName}", null, processId);
        }

        public void LogExcelFileCreationComplete(string fileName, int recordCount, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"  ✅ Excel file created: {fileName} ({recordCount:N0} records)", null, processId);
        }

        public void LogStoredProcedure(string spName, string parameters, TimeSpan elapsed, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"  🔍 SP: {spName} | ⏱️ {elapsed:mm\\:ss\\.fff} | 📊 {parameters}", null, processId);
        }

        public void LogDataReader(string viewName, string orderColumn, long recordCount, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"  📋 View: {viewName} | 📊 Order: {orderColumn} | 📈 Records: {recordCount:N0}", null, processId);
        }

        public void LogSkipped(string fileName, long recordCount, string reason, string processId)
        {
            EnqueueLog(LogLevel.WARNING, $"  ⚠️ SKIPPED: {fileName} | 📊 Rows: {recordCount} | 🚫 Reason: {reason}", null, processId);
        }

        public void LogFileSave(string status, TimeSpan elapsed, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"  💾 File Save {status} | ⏱️ {elapsed:mm\\:ss\\.fff}", null, processId);
        }

        public void LogExcelResult(string fileName, TimeSpan elapsed, long recordCount, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"  ✅ Excel Complete: {fileName} | ⏱️ {elapsed:mm\\:ss\\.fff} | 📊 {recordCount} records", null, processId);
        }

        public TimerHelper StartTimer(string operationName, string processId)
        {
            return new TimerHelper(operationName, processId, this);
        }

        private void EnqueueLog(LogLevel level, string message, string? stackTrace, string? processId)
        {
            if (_disposed) return;

            _logQueue.Enqueue(new LogEntry
            {
                Timestamp = GetOptimizedTimestamp(), // Use optimized timestamp instead of DateTime.Now
                Level = level,
                Message = message,
                StackTrace = stackTrace,
                ProcessId = processId
            });

            // Send only essential logs to MonitoringService for clean display
            try
            {
                var monitoringService = MonitoringService.Instance;
                var monitoringLevel = ConvertToMonitoringLogLevel(level);
                
                // Filter to only send essential information
                bool isEssentialLog = IsEssentialLogMessage(message);
                
                if (isEssentialLog)
                {
                    monitoringService.AddLog(monitoringLevel, message, _modulePrefix, stackTrace ?? "");
                }
                
                // Update status based on key process events
                if (message.Contains("PROCESS START"))
                {
                    monitoringService.UpdateStatus(StatusType.Running, "Process started");
                }
                else if (message.Contains("PROCESS COMPLETE"))
                {
                    monitoringService.UpdateStatus(StatusType.Completed, "Process completed");
                }
                else if (message.Contains("Excel Complete"))
                {
                    monitoringService.UpdateStatus(StatusType.Completed, message);
                }
                else if (message.Contains("Process failed"))
                {
                    monitoringService.UpdateStatus(StatusType.Error, "Process failed");
                }
            }
            catch (Exception)
            {
                // Don't let monitoring failures affect main logging
                // Silently continue
            }
        }

        private TradeDataHub.Features.Monitoring.Models.LogLevel ConvertToMonitoringLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.INFO => TradeDataHub.Features.Monitoring.Models.LogLevel.Info,
                LogLevel.WARNING => TradeDataHub.Features.Monitoring.Models.LogLevel.Warning,
                LogLevel.ERROR => TradeDataHub.Features.Monitoring.Models.LogLevel.Error,
                _ => TradeDataHub.Features.Monitoring.Models.LogLevel.Info
            };
        }

        private bool IsEssentialLogMessage(string message)
        {
            // Only show these essential log types for clean monitoring
            return message.Contains("📋 Parameters:") ||           // Parameters info
                   message.Contains("✅ Excel Complete:") ||       // Excel completion with filename
                   message.Contains("⏱️  Total Time:") ||         // Total time
                   message.Contains("➤ Validation: Row count:") || // Record count
                   message.Contains("Process failed") ||           // Error messages
                   message.Contains("ERROR");                      // Any error logs
        }

        private async Task FlushLogsAsync()
        {
            if (_disposed || !await _flushSemaphore.WaitAsync(100)) return;

            try
            {
                UpdateLogFileName();

                if (_logQueue.IsEmpty) return;

                var logs = new StringBuilder();
                while (_logQueue.TryDequeue(out var entry))
                {
                    var logLine = FormatLogEntry(entry);
                    logs.AppendLine(logLine);
                }

                if (logs.Length > 0)
                {
                    await File.AppendAllTextAsync(_currentLogFile, logs.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to flush logs: {ex.Message}");
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }

        private static string FormatLogEntry(LogEntry entry)
        {
            var processInfo = string.IsNullOrEmpty(entry.ProcessId) ? "" : $" [{entry.ProcessId}]";
            var logMessage = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {entry.Level}{processInfo} {entry.Message}";

            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                logMessage += Environment.NewLine + entry.StackTrace;
            }

            return logMessage;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _flushTimer?.Dispose();
            Task.Run(async () => await FlushLogsAsync()).Wait(5000);
            _flushSemaphore?.Dispose();
        }
    }

    public enum LogLevel
    {
        INFO,
        WARNING,
        ERROR
    }
}
