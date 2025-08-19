using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using TradeDataHub.Config;
using TradeDataHub.Core.Services;

namespace TradeDataHub.Core.Logging
{
    /// <summary>
    /// High-performance logging implementation with minimal allocations and lock-free operations
    /// </summary>
    public sealed class HighPerformanceLogger : IDisposable
    {
        private static readonly Lazy<HighPerformanceLogger> _instance = new(() => new HighPerformanceLogger());
        public static HighPerformanceLogger Instance => _instance.Value;

        // Lock-free logging queue for maximum throughput
        private readonly ConcurrentQueue<LogEntry> _logQueue = new();
        
        // String pools to reduce allocations
        private readonly ConcurrentDictionary<string, string> _stringPool = new();
        private readonly ConcurrentDictionary<string, StringBuilder> _stringBuilderPool = new();
        
        // Performance settings
        private readonly PerformanceSettings _performanceSettings;
        
        // Background processing
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task _backgroundProcessor;
        
        // File management
        private readonly string _logDirectory;
        private string _currentLogFile = string.Empty;
        private DateTime _currentLogDate = DateTime.MinValue;
        private FileStream? _currentFileStream;
        private StreamWriter? _currentWriter;
        
        // Performance optimization: Cached timestamp with reduced syscalls
        private DateTime _lastTimestampCache = DateTime.Now;
        private long _lastTickCount = Environment.TickCount64;
        private readonly object _timestampLock = new object();
        
        // Counters for monitoring
        private long _totalLogEntries = 0;
        private long _droppedLogEntries = 0;
        
        private bool _disposed = false;
        
        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? ProcessId { get; set; }
            public string? Module { get; set; }
        }
        
        private HighPerformanceLogger()
        {
            _performanceSettings = ConfigurationCacheService.GetPerformanceSettings();
            
            // Load shared database config for log directory
            var basePath = Directory.GetCurrentDirectory();
            var dbSettings = ConfigurationCacheService.GetSharedDatabaseSettings();
            _logDirectory = dbSettings.LogDirectory;
            Directory.CreateDirectory(_logDirectory);
            
            UpdateLogFileName();
            
            // Start background processing task
            _backgroundProcessor = Task.Run(ProcessLogEntriesAsync, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// High-performance logging method with minimal allocations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message, string? processId = null, string? module = null)
        {
            if (_disposed) return;
            
            EnqueueLogEntry(LogLevel.INFO, message, processId, module);
        }

        /// <summary>
        /// High-performance error logging
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, string? processId = null, string? module = null)
        {
            if (_disposed) return;
            
            EnqueueLogEntry(LogLevel.ERROR, message, processId, module);
        }

        /// <summary>
        /// High-performance warning logging
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message, string? processId = null, string? module = null)
        {
            if (_disposed) return;
            
            EnqueueLogEntry(LogLevel.WARNING, message, processId, module);
        }

        /// <summary>
        /// Optimized process logging with string pooling
        /// </summary>
        public void LogProcessStart(string processName, string parameters, string processId, string? module = null)
        {
            if (_disposed) return;

            var pooledMessage = GetPooledString($"🚀 PROCESS START: {processName} | 📋 Parameters: {parameters}");
            EnqueueLogEntry(LogLevel.INFO, pooledMessage, processId, module);
        }

        /// <summary>
        /// Optimized process completion logging
        /// </summary>
        public void LogProcessComplete(string processName, TimeSpan elapsed, string result, string processId, string? module = null)
        {
            if (_disposed) return;

            var pooledMessage = GetPooledString($"✅ PROCESS COMPLETE: {processName} | ⏱️ {elapsed:mm\\:ss\\.fff} | 📊 {result}");
            EnqueueLogEntry(LogLevel.INFO, pooledMessage, processId, module);
        }

        /// <summary>
        /// High-performance step logging
        /// </summary>
        public void LogStep(string stepName, string details, string? processId = null, string? module = null)
        {
            if (_disposed) return;

            var pooledMessage = GetPooledString($"➤ {stepName}: {details}");
            EnqueueLogEntry(LogLevel.INFO, pooledMessage, processId, module);
        }

        /// <summary>
        /// Enqueue log entry with minimal allocation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnqueueLogEntry(LogLevel level, string message, string? processId, string? module)
        {
            try
            {
                var entry = new LogEntry
                {
                    Timestamp = GetOptimizedTimestamp(),
                    Level = level,
                    Message = message,
                    ProcessId = processId,
                    Module = module
                };

                _logQueue.Enqueue(entry);
                Interlocked.Increment(ref _totalLogEntries);
            }
            catch
            {
                // Increment dropped counter if enqueueing fails
                Interlocked.Increment(ref _droppedLogEntries);
            }
        }

        /// <summary>
        /// Get pooled string to reduce allocations for repeated messages
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetPooledString(string message)
        {
            if (!_performanceSettings.Logging.EnableStringPooling)
                return message;

            return _stringPool.GetOrAdd(message, msg => msg);
        }

        /// <summary>
        /// Optimized timestamp generation with caching
        /// </summary>
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

        /// <summary>
        /// Background task for processing log entries with high performance
        /// </summary>
        private async Task ProcessLogEntriesAsync()
        {
            var batchSize = _performanceSettings.Logging.BatchSize;
            var flushInterval = TimeSpan.FromMilliseconds(_performanceSettings.Logging.FlushIntervalMs);
            var entries = new LogEntry[batchSize];
            var stringBuilder = new StringBuilder(8192); // Pre-allocated large buffer
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var entriesProcessed = 0;
                    
                    // Collect batch of log entries
                    while (entriesProcessed < batchSize && _logQueue.TryDequeue(out var entry))
                    {
                        entries[entriesProcessed++] = entry;
                    }
                    
                    if (entriesProcessed > 0)
                    {
                        // Process batch efficiently
                        await ProcessLogBatchAsync(entries, entriesProcessed, stringBuilder);
                    }
                    else
                    {
                        // No entries, wait briefly
                        await Task.Delay(Math.Min(100, _performanceSettings.Logging.FlushIntervalMs / 10), _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // Normal shutdown
                }
                catch (Exception)
                {
                }
            }
            
            // Final flush on shutdown
            await FlushRemainingEntriesAsync(stringBuilder);
        }

        /// <summary>
        /// Process a batch of log entries efficiently
        /// </summary>
        private async Task ProcessLogBatchAsync(LogEntry[] entries, int count, StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
            
            UpdateLogFileName(); // Handle day rollover
            EnsureFileStreamOpen();
            
            // Format all entries in batch
            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                FormatLogEntry(entry, stringBuilder);
                entries[i] = null!; // Clear reference for GC
            }
            
            // Write entire batch to file asynchronously
            if (_currentWriter != null && stringBuilder.Length > 0)
            {
                await _currentWriter.WriteAsync(stringBuilder.ToString());
                await _currentWriter.FlushAsync();
            }
        }

        /// <summary>
        /// Format log entry with minimal allocations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FormatLogEntry(LogEntry entry, StringBuilder sb)
        {
            sb.Append('[')
              .Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
              .Append("] ")
              .Append(entry.Level.ToString())
              .Append(' ');

            if (!string.IsNullOrEmpty(entry.ProcessId))
            {
                sb.Append('[').Append(entry.ProcessId).Append("] ");
            }

            if (!string.IsNullOrEmpty(entry.Module))
            {
                sb.Append('[').Append(entry.Module).Append("] ");
            }

            sb.AppendLine(entry.Message);
        }

        /// <summary>
        /// Update log file name for day rollover
        /// </summary>
        private void UpdateLogFileName()
        {
            var today = _lastTimestampCache.Date;
            if (_currentLogDate != today)
            {
                CloseCurrentFile();
                
                _currentLogDate = today;
                _currentLogFile = Path.Combine(_logDirectory, $"HighPerformance_Log_{today:yyyyMMdd}.txt");
            }
        }

        /// <summary>
        /// Ensure file stream is open and ready
        /// </summary>
        private void EnsureFileStreamOpen()
        {
            if (_currentFileStream == null || _currentWriter == null)
            {
                try
                {
                    _currentFileStream = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read, 
                                                       _performanceSettings.FileIO.BufferSize);
                    _currentWriter = new StreamWriter(_currentFileStream, Encoding.UTF8, _performanceSettings.FileIO.BufferSize);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Close current file streams
        /// </summary>
        private void CloseCurrentFile()
        {
            try
            {
                _currentWriter?.Flush();
                _currentWriter?.Dispose();
                _currentWriter = null;
                
                _currentFileStream?.Dispose();
                _currentFileStream = null;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Flush any remaining entries during shutdown
        /// </summary>
        private async Task FlushRemainingEntriesAsync(StringBuilder stringBuilder)
        {
            var remainingEntries = new LogEntry[1000];
            var count = 0;
            
            while (_logQueue.TryDequeue(out var entry) && count < remainingEntries.Length)
            {
                remainingEntries[count++] = entry;
            }
            
            if (count > 0)
            {
                await ProcessLogBatchAsync(remainingEntries, count, stringBuilder);
            }
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public (long TotalEntries, long DroppedEntries, int QueueSize, int StringPoolSize) GetStatistics()
        {
            return (
                Interlocked.Read(ref _totalLogEntries),
                Interlocked.Read(ref _droppedLogEntries),
                _logQueue.Count,
                _stringPool.Count
            );
        }

        /// <summary>
        /// Clear string pools to free memory
        /// </summary>
        public void ClearPools()
        {
            _stringPool.Clear();
            _stringBuilderPool.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            try
            {
                _cancellationTokenSource.Cancel();
                _backgroundProcessor?.Wait(TimeSpan.FromSeconds(5));
                
                CloseCurrentFile();
                
                _cancellationTokenSource.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}

