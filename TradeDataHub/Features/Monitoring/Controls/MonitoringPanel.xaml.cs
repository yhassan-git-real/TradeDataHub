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
            DataContext = this;
            
            // Subscribe to events
            _monitoringService.StatusChanged += OnStatusChanged;
            _monitoringService.LogAdded += OnLogAdded;
            _monitoringService.PropertyChanged += OnMonitoringServicePropertyChanged;
            
            // Initialize UI components that exist
            InitializeUIComponents();
        }

        private void InitializeUIComponents()
        {
            try
            {
                // Try to initialize components that may exist
                if (this.FindName("LogsListView") is ListView logsListView)
                {
                    logsListView.ItemsSource = _monitoringService.LogEntries;
                }
                
                // Wire up search functionality after initialization
                if (this.FindName("LogSearchTextBox") is TextBox searchTextBox)
                {
                    searchTextBox.TextChanged += LogSearchTextBox_TextChanged;
                    // Initialize with placeholder text
                    searchTextBox.Text = "Search logs...";
                    searchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                }
                
                // Initialize StartStopToggle to default 'Start' state (paused)
                if (this.FindName("StartStopToggle") is System.Windows.Controls.Primitives.ToggleButton startStopToggle)
                {
                    startStopToggle.IsChecked = false; // Default to unchecked (Start/paused state)
                    startStopToggle.Content = "Start";
                    startStopToggle.ToolTip = "Start real-time log display";
                    // Ensure the monitoring service starts in paused state
                    _monitoringService.IsRealTimeDisplayEnabled = false;
                }
                
                // Initialize other UI elements
                UpdateStatusDisplay();
                UpdateLogCounts();
            }
            catch (Exception ex)
            {
                // Log error but don't crash - UI should continue to function
                _monitoringService?.AddLog(LogLevel.Error, 
                    $"UI initialization failed: {ex.Message}", 
                    "MonitoringPanel", 
                    ex.ToString());
            }
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
                if (_isAutoScrollEnabled && this.FindName("LogsListView") is ListView logsListView && logsListView.Items.Count > 0)
                {
                    logsListView.ScrollIntoView(logsListView.Items[logsListView.Items.Count - 1]);
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
            
            try
            {
                // Update status badge
                if (this.FindName("StatusText") is TextBlock statusText)
                    statusText.Text = status.StatusDisplayText;
                if (this.FindName("StatusMessage") is TextBlock statusMessage)
                    statusMessage.Text = status.StatusMessage;
                
                // Update status badge color
                var color = (SolidColorBrush?)new BrushConverter().ConvertFromString(status.StatusColor);
                if (color != null)
                {
                    if (this.FindName("StatusBadge") is Border statusBadge)
                        statusBadge.Background = color;
                    if (this.FindName("StatusIndicator") is System.Windows.Shapes.Ellipse statusIndicator)
                        statusIndicator.Fill = color;
                }
                
                // Update progress
                if (this.FindName("ProgressBar") is ProgressBar progressBar)
                {
                    progressBar.Value = status.ProgressPercentage;
                    // Show/hide progress based on status
                    var showProgress = status.CurrentStatus == StatusType.Running;
                    progressBar.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
                }
                
                if (this.FindName("ProgressText") is TextBlock progressText)
                {
                    progressText.Text = $"{status.ProgressPercentage}%";
                    var showProgress = status.CurrentStatus == StatusType.Running;
                    progressText.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
            }
            
            // Update detailed information
            UpdateDetailedInformation();
        }

        private void UpdateLogCounts()
        {
            try
            {
                if (this.FindName("LogCountText") is TextBlock logCountText)
                    logCountText.Text = $"{_monitoringService.LogEntries.Count} entries";
            }
            catch (Exception)
            {
            }
        }

        private void UpdateDetailedInformation()
        {
            var status = _monitoringService.CurrentStatus;
            
            try
            {
                // Update detailed information with clean text (icons are in XAML)
                if (this.FindName("CurrentFileText") is TextBlock currentFileText)
                    currentFileText.Text = !string.IsNullOrEmpty(status.CurrentFileName) 
                        ? status.CurrentFileName 
                        : "No file active";
                    
                if (this.FindName("RecordCountText") is TextBlock recordCountText)
                    recordCountText.Text = !string.IsNullOrEmpty(status.RecordCount) 
                        ? $"{status.RecordCount} records" 
                        : "0 records";
                    
                if (this.FindName("ElapsedTimeText") is TextBlock elapsedTimeText)
                    elapsedTimeText.Text = !string.IsNullOrEmpty(status.ElapsedTime) 
                        ? status.ElapsedTime 
                        : "00:00.000";
            }
            catch (Exception)
            {
            }
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            _monitoringService.ClearLogs();
        }

        private void AutoScrollToggle_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("AutoScrollToggle") is System.Windows.Controls.Primitives.ToggleButton autoScrollToggle)
            {
                _isAutoScrollEnabled = autoScrollToggle.IsChecked ?? false;
                _monitoringService.IsAutoScrollEnabled = _isAutoScrollEnabled;
            }
        }

        private void StartStopToggle_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("StartStopToggle") is System.Windows.Controls.Primitives.ToggleButton startStopToggle)
            {
                bool isStarted = startStopToggle.IsChecked ?? false;
                
                // Update the monitoring service to control real-time display
                _monitoringService.IsRealTimeDisplayEnabled = isStarted;
                
                // Update button content and tooltip based on state
                if (isStarted)
                {
                    startStopToggle.Content = "Stop";
                    startStopToggle.ToolTip = "Stop real-time log display";
                }
                else
                {
                    startStopToggle.Content = "Start";
                    startStopToggle.ToolTip = "Start real-time log display";
                }
            }
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
            try
            {
                if (this.FindName("ExpandCollapseIcon") is FrameworkElement iconElement && iconElement.RenderTransform is RotateTransform rotateTransform)
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
            catch (Exception)
            {
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

        private void LogSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchBox = sender as TextBox;
            var searchText = searchBox?.Text ?? string.Empty;
            
            // Don't apply filter if showing placeholder text
            if (searchText == "Search logs..." || searchBox?.Foreground == System.Windows.Media.Brushes.Gray)
            {
                if (this.FindName("ClearSearchButton") is Button clearSearchButton)
                    clearSearchButton.Visibility = Visibility.Collapsed;
                // Remove any existing filter
                if (this.FindName("LogsListView") is ListView logsListView && logsListView.ItemsSource != null)
                {
                    var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(logsListView.ItemsSource);
                    if (collectionView != null)
                        collectionView.Filter = null;
                }
                return;
            }
            
            // Show/hide clear button based on text content
            if (this.FindName("ClearSearchButton") is Button clearBtn)
                clearBtn.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Collapsed : Visibility.Visible;
            
            // Apply filter to the ListView
            ApplyLogFilter(searchText);
        }

        private void LogSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == "Search logs..." && textBox.Foreground == System.Windows.Media.Brushes.Gray)
            {
                textBox.Text = "";
                textBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void LogSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Search logs...";
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("LogSearchTextBox") is TextBox logSearchTextBox)
            {
                logSearchTextBox.Text = "";
                logSearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
                logSearchTextBox.Focus();
                
                // Clear the search filter
                ApplyLogFilter("");
                
                // Hide the clear button
                if (this.FindName("ClearSearchButton") is Button clearSearchButton)
                    clearSearchButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyLogFilter(string searchText)
        {
            if (this.FindName("LogsListView") is ListView logsListView && logsListView.Items != null)
            {
                var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(logsListView.ItemsSource);
                
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // Remove filter if search text is empty
                    collectionView.Filter = null;
                }
                else
                {
                    // Apply filter based on search text
                    collectionView.Filter = item =>
                    {
                        if (item is LogEntry logEntry)
                        {
                            return logEntry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   logEntry.Level.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   logEntry.Source.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        }
                        return false;
                    };
                }
            }
        }
    }
}
