using System;
using System.ComponentModel;

namespace TradeDataHub.Features.Monitoring.Models
{
    public enum StatusType
    {
        Idle,
        Running,
        Completed,
        Error,
        Cancelled,
        Warning
    }

    public class MonitorStatus : INotifyPropertyChanged
    {
        private StatusType _currentStatus;
        private string _statusMessage;
        private DateTime _lastUpdated;
        private int _progressPercentage;
        private string _currentOperation;

        public StatusType CurrentStatus
        {
            get => _currentStatus;
            set
            {
                _currentStatus = value;
                LastUpdated = DateTime.Now;
                OnPropertyChanged(nameof(CurrentStatus));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusDisplayText));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                _lastUpdated = value;
                OnPropertyChanged(nameof(LastUpdated));
            }
        }

        public int ProgressPercentage
        {
            get => _progressPercentage;
            set
            {
                _progressPercentage = Math.Max(0, Math.Min(100, value));
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        public string CurrentOperation
        {
            get => _currentOperation;
            set
            {
                _currentOperation = value;
                OnPropertyChanged(nameof(CurrentOperation));
            }
        }

        // Enhanced detail properties for better UI information display
        private string _currentFileName;
        private string _recordCount;
        private string _elapsedTime;
        private string _currentStep;

        public string CurrentFileName
        {
            get => _currentFileName;
            set
            {
                _currentFileName = value;
                OnPropertyChanged(nameof(CurrentFileName));
            }
        }

        public string RecordCount
        {
            get => _recordCount;
            set
            {
                _recordCount = value;
                OnPropertyChanged(nameof(RecordCount));
            }
        }

        public string ElapsedTime
        {
            get => _elapsedTime;
            set
            {
                _elapsedTime = value;
                OnPropertyChanged(nameof(ElapsedTime));
            }
        }

        public string CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                OnPropertyChanged(nameof(CurrentStep));
            }
        }

        // UI-friendly properties
        public string StatusColor
        {
            get
            {
                return CurrentStatus switch
                {
                    StatusType.Idle => "#6C757D",      // Gray
                    StatusType.Running => "#007BFF",   // Blue
                    StatusType.Completed => "#28A745", // Green
                    StatusType.Error => "#DC3545",     // Red
                    StatusType.Cancelled => "#FFC107", // Yellow
                    StatusType.Warning => "#FD7E14",   // Orange
                    _ => "#6C757D"
                };
            }
        }

        public string StatusDisplayText
        {
            get
            {
                return CurrentStatus switch
                {
                    StatusType.Idle => "Idle",
                    StatusType.Running => "Running",
                    StatusType.Completed => "Completed",
                    StatusType.Error => "Error",
                    StatusType.Cancelled => "Cancelled",
                    StatusType.Warning => "Warning",
                    _ => "Unknown"
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MonitorStatus()
        {
            CurrentStatus = StatusType.Idle;
            StatusMessage = "Ready";
            LastUpdated = DateTime.Now;
            ProgressPercentage = 0;
            CurrentOperation = string.Empty;
            CurrentFileName = string.Empty;
            RecordCount = string.Empty;
            ElapsedTime = string.Empty;
            CurrentStep = string.Empty;
        }
    }
}
