using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using TradeDataHub.Core.Helpers;
using Microsoft.Extensions.Configuration;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;
using TradeDataHub.Core.Services;
using TradeDataHub.Config;

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
        
        // Performance optimization components
        private readonly StringPool _stringPool;
        private readonly PerformanceSettings _performanceSettings;
        
        // Enhanced timestamp caching for better performance
        private DateTime _lastTimestampCache = DateTime.Now;
        private long _lastTickCount = Environment.TickCount64;
        private readonly object _timestampLock = new object();
        
        // Pre-allocated StringBuilder for better performance
        private readonly StringBuilder _logBuilder = new StringBuilder(8192);

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
            
            // Initialize performance optimization components
            _stringPool = StringPool.Instance;
            _performanceSettings = ConfigurationCacheService.GetPerformanceSettings();
            
            // Load shared database config for log directory
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder().SetBasePath(basePath)
                .AddJsonFile("Config/database.appsettings.json", optional: false);
            var cfg = builder.Build();
            _logDirectory = cfg["DatabaseConfig:LogDirectory"] ?? Path.Combine(basePath, "Logs");
            Directory.CreateDirectory(_logDirectory);

            // Use performance settings for flush interval
            _flushIntervalSeconds = Math.Max(1, _performanceSettings.Logging.FlushIntervalMs / 1000);
            UpdateLogFileName();
            
            _flushTimer = new Timer(async _ => await FlushLogsAsync(), null,
                TimeSpan.FromSeconds(_flushIntervalSeconds), TimeSpan.FromSeconds(_flushIntervalSeconds));
        }

        public string GenerateProcessId()
        {
            return $"P{Interlocked.Increment(ref _processCounter):D4}";
        }

        // Enhanced performance optimization: Get timestamp with reduced DateTime.Now calls
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DateTime GetOptimizedTimestamp()
        {
            var currentTicks = Environment.TickCount64;
            
            // Only update timestamp cache if more than 50ms have passed for higher precision
            if (currentTicks - _lastTickCount > 50)
            {
                lock (_timestampLock)
                {
                    if (currentTicks - _lastTickCount > 50) // Double-check pattern
                    {
                        _lastTimestampCache = DateTime.Now;
                        _lastTickCount = currentTicks;
                    }
                }
            }
            
            // Return cached timestamp with tick-based offset
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
            var separator = _stringPool.GetPooled(new string('=', 80));
            var dashSeparator = _stringPool.GetPooled(new string('-', 80));
            var startMessage = _stringPool.GetPooledFormat("🚀 PROCESS START: {0}", processName);
            var paramMessage = _stringPool.GetPooledFormat("📋 Parameters: {0}", parameters);
            
            EnqueueLog(LogLevel.INFO, separator, null, null);
            EnqueueLog(LogLevel.INFO, startMessage, null, processId);
            EnqueueLog(LogLevel.INFO, paramMessage, null, processId);
            EnqueueLog(LogLevel.INFO, dashSeparator, null, null);
        }

        public void LogProcessComplete(string processName, TimeSpan elapsed, string result, string processId)
        {
            var dashSeparator = _stringPool.GetPooled(new string('-', 80));
            var separator = _stringPool.GetPooled(new string('=', 80));
            var completeMessage = _stringPool.CreateProcessCompleteMessage(processName, elapsed, result);
            
            EnqueueLog(LogLevel.INFO, dashSeparator, null, null);
            EnqueueLog(LogLevel.INFO, completeMessage, null, processId);
            EnqueueLog(LogLevel.INFO, separator, null, null);
        }

        public void LogStep(string stepName, string details, string processId)
        {
            var stepMessage = _performanceSettings.Logging.EnableStringPooling 
                ? _stringPool.CreateStepMessage(stepName, details)
                : $"➤ {stepName}: {details}";
            EnqueueLog(LogLevel.INFO, $"  {stepMessage}", null, processId);
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
            var message = _stringPool.GetPooledFormat("  📋 Creating Excel file: {0}", fileName);
            EnqueueLog(LogLevel.INFO, message, null, processId);
        }

        public void LogExcelFileCreationComplete(string fileName, int recordCount, string processId)
        {
            var message = _stringPool.GetPooledFormat("  ✅ Excel file created: {0} ({1:N0} records)", fileName, recordCount);
            EnqueueLog(LogLevel.INFO, message, null, processId);
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
            var message = _stringPool.CreateFileResultMessage(fileName, elapsed, recordCount);
            EnqueueLog(LogLevel.INFO, $"  {message}", null, processId);
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

                // Use pre-allocated StringBuilder for better performance
                _logBuilder.Clear();
                
                var batchSize = _performanceSettings.Logging.BatchSize;
                var entriesProcessed = 0;

                // Process in batches for better memory management
                while (_logQueue.TryDequeue(out var entry) && entriesProcessed < batchSize)
                {
                    FormatLogEntryOptimized(entry, _logBuilder);
                    entriesProcessed++;
                }

                if (_logBuilder.Length > 0)
                {
                    // Use optimized file I/O with proper buffer size
                    await File.AppendAllTextAsync(_currentLogFile, _logBuilder.ToString());
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

        /// <summary>
        /// Optimized log entry formatting with StringBuilder for better performance
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FormatLogEntryOptimized(LogEntry entry, StringBuilder sb)
        {
            sb.Append('[')
              .Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
              .Append("] ")
              .Append(entry.Level.ToString());

            if (!string.IsNullOrEmpty(entry.ProcessId))
            {
                sb.Append(" [").Append(entry.ProcessId).Append(']');
            }

            sb.Append(' ').AppendLine(entry.Message);

            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                sb.AppendLine(entry.StackTrace);
            }
        }

        // Keep original method for backward compatibility
        private static string FormatLogEntry(LogEntry entry)
        {
            var sb = new StringBuilder(256);
            FormatLogEntryOptimized(entry, sb);
            return sb.ToString().TrimEnd();
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
