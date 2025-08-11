using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.ComponentModel;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;

namespace TradeDataHub.Features.Monitoring.Controls
{
    public partial class MonitoringPanel : UserControl, INotifyPropertyChanged
    {
        private readonly MonitoringService _monitoringService;
        private bool _isAutoScrollEnabled = true;
        private bool _isExpanded = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                    AnimateExpandCollapseIcon();
                }
            }
        }

        public MonitoringPanel()
        {
            InitializeComponent();
            _monitoringService = MonitoringService.Instance;
            
            // Set up data binding
            DataContext = this; // Changed to bind to this control for IsExpanded property
            LogsListView.ItemsSource = _monitoringService.LogEntries;
            
            // Subscribe to events
            _monitoringService.StatusChanged += OnStatusChanged;
            _monitoringService.LogAdded += OnLogAdded;
            _monitoringService.PropertyChanged += OnMonitoringServicePropertyChanged;
            
            // Initialize UI
            UpdateStatusDisplay();
            UpdateLogCounts();
        }

        private void OnStatusChanged(object sender, MonitorStatus status)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatusDisplay();
                UpdateDetailedInformation();
            });
        }

        private void OnLogAdded(object sender, LogEntry logEntry)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateLogCounts();
                
                // Auto-scroll to bottom if enabled
                if (_isAutoScrollEnabled && LogsListView.Items.Count > 0)
                {
                    LogsListView.ScrollIntoView(LogsListView.Items[LogsListView.Items.Count - 1]);
                }
            });
        }

        private void OnMonitoringServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(MonitoringService.CurrentStatus))
                {
                    UpdateStatusDisplay();
                }
            });
        }

        private void UpdateStatusDisplay()
        {
            var status = _monitoringService.CurrentStatus;
            
            // Update status badge
            StatusText.Text = status.StatusDisplayText;
            StatusMessage.Text = status.StatusMessage;
            
            // Update status badge color
            var color = (SolidColorBrush)new BrushConverter().ConvertFromString(status.StatusColor);
            StatusBadge.Background = color;
            StatusIndicator.Fill = color;
            
            // Update progress
            ProgressBar.Value = status.ProgressPercentage;
            ProgressText.Text = $"{status.ProgressPercentage}%";
            
            // Show/hide progress based on status
            var showProgress = status.CurrentStatus == StatusType.Running;
            ProgressBar.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
            ProgressText.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
            
            // Update detailed information
            UpdateDetailedInformation();
        }

        private void UpdateLogCounts()
        {
            LogCountText.Text = $"{_monitoringService.LogEntries.Count} entries";
            // LastUpdateText was removed in UI modernization - timestamp now handled by status message
            // LastUpdateText.Text = $"Last: {DateTime.Now:HH:mm:ss}";
        }

        private void UpdateDetailedInformation()
        {
            var status = _monitoringService.CurrentStatus;
            
            // Update detailed information with clean text (icons are in XAML)
            CurrentFileText.Text = !string.IsNullOrEmpty(status.CurrentFileName) 
                ? status.CurrentFileName 
                : "No file active";
                
            RecordCountText.Text = !string.IsNullOrEmpty(status.RecordCount) 
                ? $"{status.RecordCount} records" 
                : "0 records";
                
            ElapsedTimeText.Text = !string.IsNullOrEmpty(status.ElapsedTime) 
                ? status.ElapsedTime 
                : "00:00.000";
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            _monitoringService.ClearLogs();
        }

        private void AutoScrollToggle_Click(object sender, RoutedEventArgs e)
        {
            _isAutoScrollEnabled = AutoScrollToggle.IsChecked ?? false;
            _monitoringService.IsAutoScrollEnabled = _isAutoScrollEnabled;
        }

        // Public methods for external control
        public void UpdateStatus(StatusType status, string message, string operation = "")
        {
            _monitoringService.UpdateStatus(status, message, operation);
        }

        public void UpdateProgress(int percentage)
        {
            _monitoringService.UpdateProgress(percentage);
        }

        public void AddLog(LogLevel level, string message, string source = "", string details = "")
        {
            _monitoringService.AddLog(level, message, source, details);
        }

        public void SetIdle()
        {
            _monitoringService.SetIdle();
        }

        public void SetRunning(string operation)
        {
            _monitoringService.SetRunning(operation);
        }

        public void SetCompleted(string message = "Operation completed successfully")
        {
            _monitoringService.SetCompleted(message);
        }

        private void ExpandCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        private void AnimateExpandCollapseIcon()
        {
            var iconElement = ExpandCollapseIcon;
            if (iconElement?.RenderTransform is RotateTransform rotateTransform)
            {
                var rotateAnimation = new DoubleAnimation
                {
                    To = IsExpanded ? 0 : 180,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            }
        }

        public void SetError(string errorMessage)
        {
            _monitoringService.SetError(errorMessage);
        }

        public void SetCancelled()
        {
            _monitoringService.SetCancelled();
        }

        public void SetWarning(string warningMessage)
        {
            _monitoringService.SetWarning(warningMessage);
        }
    }
}
