using System;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Windows.Threading;
using TradeDataHub.Features.Monitoring.Models;

namespace TradeDataHub.Features.Monitoring.Services
{
    public class MonitoringService : INotifyPropertyChanged
    {
        private readonly object _lockObject = new object();
        private readonly ConcurrentQueue<LogEntry> _pendingLogs = new ConcurrentQueue<LogEntry>();
        private readonly DispatcherTimer _updateTimer;
        private const int MAX_LOG_ENTRIES = 1000; // Performance optimization - limit entries
        private const int UPDATE_INTERVAL_MS = 250; // Smooth real-time updates

        private MonitorStatus _currentStatus;
        private ObservableCollection<LogEntry> _logEntries;
        private bool _isAutoScrollEnabled = true;
        private bool _isRealTimeDisplayEnabled = false; // Default to stopped/paused state

        public MonitorStatus CurrentStatus
        {
            get => _currentStatus;
            private set
            {
                _currentStatus = value;
                OnPropertyChanged(nameof(CurrentStatus));
            }
        }

        public ObservableCollection<LogEntry> LogEntries
        {
            get => _logEntries;
            private set
            {
                _logEntries = value;
                OnPropertyChanged(nameof(LogEntries));
            }
        }

        public bool IsAutoScrollEnabled
        {
            get => _isAutoScrollEnabled;
            set
            {
                _isAutoScrollEnabled = value;
                OnPropertyChanged(nameof(IsAutoScrollEnabled));
            }
        }

        public bool IsRealTimeDisplayEnabled
        {
            get => _isRealTimeDisplayEnabled;
            set
            {
                _isRealTimeDisplayEnabled = value;
                OnPropertyChanged(nameof(IsRealTimeDisplayEnabled));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<LogEntry> LogAdded;
        public event EventHandler<MonitorStatus> StatusChanged;

        private static MonitoringService _instance;
        private static readonly object _instanceLock = new object();

        public static MonitoringService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MonitoringService();
                        }
                    }
                }
                return _instance;
            }
        }

        private MonitoringService()
        {
            CurrentStatus = new MonitorStatus();
            LogEntries = new ObservableCollection<LogEntry>();
            
            // Setup timer for batched updates (performance optimization)
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MS)
            };
            _updateTimer.Tick += ProcessPendingLogs;
            _updateTimer.Start();

            // Initial log entry
            AddLog(LogLevel.Info, "Monitoring system initialized", "MonitoringService");
        }

        public void UpdateStatus(StatusType status, string message, string operation = "")
        {
            lock (_lockObject)
            {
                CurrentStatus.CurrentStatus = status;
                CurrentStatus.StatusMessage = message;
                if (!string.IsNullOrEmpty(operation))
                {
                    CurrentStatus.CurrentOperation = operation;
                }
                
                // Extract detailed information from message using regex patterns
                ExtractDetailedInformation(message);
                
                StatusChanged?.Invoke(this, CurrentStatus);
                
                // Auto-log status changes
                AddLog(LogLevel.Info, $"Status: {message}", "System");
            }
        }

        private void ExtractDetailedInformation(string message)
        {
            // Extract filename from Excel Complete messages: ✅ Excel Complete: 16_JAN25EXP.xlsx
            var excelCompleteMatch = System.Text.RegularExpressions.Regex.Match(message, @"Excel Complete:\s*([A-Z0-9_]+\.xlsx)");
            if (excelCompleteMatch.Success)
            {
                CurrentStatus.CurrentFileName = excelCompleteMatch.Groups[1].Value;
                CurrentStatus.CurrentStep = "Completed";
            }

            // Extract record count from validation: ➤ Validation: Row count: 1,098
            var validationMatch = System.Text.RegularExpressions.Regex.Match(message, @"Validation: Row count:\s*([\d,]+)");
            if (validationMatch.Success)
            {
                CurrentStatus.RecordCount = validationMatch.Groups[1].Value;
            }

            // Extract total time: ⏱️ Total Time: 00:04.207
            var totalTimeMatch = System.Text.RegularExpressions.Regex.Match(message, @"Total Time:\s*(\d{2}:\d{2}\.\d{3})");
            if (totalTimeMatch.Success)
            {
                CurrentStatus.ElapsedTime = totalTimeMatch.Groups[1].Value;
            }

            // Extract parameters info for display
            if (message.Contains("📋 Parameters:"))
            {
                CurrentStatus.CurrentStep = "Processing";
            }
        }

        public void UpdateProgress(int percentage)
        {
            CurrentStatus.ProgressPercentage = percentage;
        }

        public void AddLog(LogLevel level, string message, string source = "", string details = "")
        {
            var logEntry = new LogEntry(level, message, source, details);
            
            // Use concurrent queue for thread-safe adding (performance optimization)
            _pendingLogs.Enqueue(logEntry);
        }

        private void ProcessPendingLogs(object sender, EventArgs e)
        {
            // Only process UI updates if real-time display is enabled
            if (!_isRealTimeDisplayEnabled)
            {
                return; // Backend logging continues, but UI display is paused
            }

            // Process batched log updates (performance optimization)
            var logsToAdd = new List<LogEntry>();
            
            while (_pendingLogs.TryDequeue(out LogEntry logEntry) && logsToAdd.Count < 50)
            {
                logsToAdd.Add(logEntry);
            }

            if (logsToAdd.Count > 0)
            {
                foreach (var log in logsToAdd)
                {
                    // Ensure we don't exceed max entries (memory optimization)
                    if (LogEntries.Count >= MAX_LOG_ENTRIES)
                    {
                        // Remove oldest entries
                        for (int i = 0; i < Math.Min(100, LogEntries.Count - MAX_LOG_ENTRIES + 100); i++)
                        {
                            LogEntries.RemoveAt(0);
                        }
                    }

                    LogEntries.Add(log);
                    LogAdded?.Invoke(this, log);
                }
            }
        }

        public void ClearLogs()
        {
            LogEntries.Clear();
            AddLog(LogLevel.Info, "Log entries cleared", "MonitoringService");
        }

        public void SetIdle()
        {
            UpdateStatus(StatusType.Idle, "Ready");
            CurrentStatus.ProgressPercentage = 0;
            CurrentStatus.CurrentOperation = "";
        }

        public void SetRunning(string operation)
        {
            UpdateStatus(StatusType.Running, "Processing...", operation);
        }

        public void SetCompleted(string message = "Operation completed successfully")
        {
            UpdateStatus(StatusType.Completed, message);
            CurrentStatus.ProgressPercentage = 100;
        }

        public void SetError(string errorMessage)
        {
            UpdateStatus(StatusType.Error, errorMessage);
            AddLog(LogLevel.Error, errorMessage, "System");
        }

        public void SetCancelled()
        {
            UpdateStatus(StatusType.Cancelled, "Operation cancelled by user");
            AddLog(LogLevel.Warning, "Operation cancelled by user", "System");
        }

        public void SetWarning(string warningMessage)
        {
            UpdateStatus(StatusType.Warning, warningMessage);
            AddLog(LogLevel.Warning, warningMessage, "System");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Tick -= ProcessPendingLogs;
            }
        }
    }
}
