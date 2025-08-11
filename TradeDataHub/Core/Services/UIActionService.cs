using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TradeDataHub.Core.Controllers;
using TradeDataHub.Core.Models;
using TradeDataHub.Features.Common.ViewModels;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;
using MonitoringLogLevel = TradeDataHub.Features.Monitoring.Models.LogLevel;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for handling main UI actions (Generate, Cancel, Reset)
    /// </summary>
    public interface IUIActionService
    {
        Task HandleGenerateAsync(CancellationToken cancellationToken);
        void HandleCancel();
        void HandleReset();
        void Initialize(Window window);
        void SetServiceContainer(ServiceContainer serviceContainer);
    }

    public class UIActionService : IUIActionService
    {
        private readonly IExportController _exportController;
        private readonly IImportController _importController;
        private readonly MonitoringService _monitoringService;
        private Window? _mainWindow;
        private CancellationTokenSource? _currentCancellationSource;

        // Service container reference to access view models
        private ServiceContainer? _serviceContainer;

        // UI Controls - accessed via FindName
        private System.Windows.Controls.RadioButton? _rbExport;
        private System.Windows.Controls.RadioButton? _rbImport;
        private System.Windows.Controls.Button? _generateButton;
        private System.Windows.Controls.Button? _cancelButton;
        
        // Input controls for data binding
        private Controls.YearMonthPicker? _fromMonthPicker;
        private Controls.YearMonthPicker? _toMonthPicker;
        private System.Windows.Controls.TextBox? _txtHS;
        private System.Windows.Controls.TextBox? _txtPort;
        private System.Windows.Controls.TextBox? _txtProduct;
        private System.Windows.Controls.TextBox? _txtExporter;
        private System.Windows.Controls.TextBox? _txtImporter;
        private System.Windows.Controls.TextBox? _txtForCount;
        private System.Windows.Controls.TextBox? _txtForName;
        private System.Windows.Controls.TextBox? _txtIEC;

        public UIActionService(
            IExportController exportController,
            IImportController importController,
            MonitoringService monitoringService)
        {
            _exportController = exportController;
            _importController = importController;
            _monitoringService = monitoringService;
        }

        public void SetServiceContainer(ServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public void Initialize(Window window)
        {
            _mainWindow = window;
            
            // Get UI control references
            _rbExport = window.FindName("rbExport") as System.Windows.Controls.RadioButton;
            _rbImport = window.FindName("rbImport") as System.Windows.Controls.RadioButton;
            _generateButton = window.FindName("GenerateButton") as System.Windows.Controls.Button;
            _cancelButton = window.FindName("CancelButton") as System.Windows.Controls.Button;
            
            // Get input controls
            _fromMonthPicker = window.FindName("FromMonthPicker") as Controls.YearMonthPicker;
            _toMonthPicker = window.FindName("ToMonthPicker") as Controls.YearMonthPicker;
            _txtHS = window.FindName("Txt_HS") as System.Windows.Controls.TextBox;
            _txtPort = window.FindName("txt_Port") as System.Windows.Controls.TextBox;
            _txtProduct = window.FindName("Txt_Product") as System.Windows.Controls.TextBox;
            _txtExporter = window.FindName("Txt_Exporter") as System.Windows.Controls.TextBox;
            _txtImporter = window.FindName("Txt_Importer") as System.Windows.Controls.TextBox;
            _txtForCount = window.FindName("txt_ForCount") as System.Windows.Controls.TextBox;
            _txtForName = window.FindName("Txt_ForName") as System.Windows.Controls.TextBox;
            _txtIEC = window.FindName("Txt_IEC") as System.Windows.Controls.TextBox;
        }

        public async Task HandleGenerateAsync(CancellationToken cancellationToken)
        {
            try
            {
                _currentCancellationSource?.Dispose();
                _currentCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _monitoringService.UpdateStatus(StatusType.Running, "Processing...");
                if (_generateButton != null) _generateButton.IsEnabled = false;

                // Check which process type is selected
                if (_rbImport?.IsChecked == true)
                {
                    // Import is selected - use ImportController
                    var importInputs = GetImportInputs();
                    var selectedView = _serviceContainer?.ImportDbObjectViewModel?.SelectedView?.Name ?? "";
                    var selectedSP = _serviceContainer?.ImportDbObjectViewModel?.SelectedStoredProcedure?.Name ?? "";
                    await _importController.RunAsync(importInputs, _currentCancellationSource.Token, selectedView, selectedSP);
                }
                else if (_rbExport?.IsChecked == true)
                {
                    // Export is selected - use ExportController
                    var exportInputs = GetExportInputs();
                    var selectedView = _serviceContainer?.ExportDbObjectViewModel?.SelectedView?.Name ?? "";
                    var selectedSP = _serviceContainer?.ExportDbObjectViewModel?.SelectedStoredProcedure?.Name ?? "";
                    await _exportController.RunAsync(exportInputs, _currentCancellationSource.Token, selectedView, selectedSP);
                }
                else
                {
                    // Neither Import nor Export is selected
                    MessageBox.Show("Please select either Import or Export mode.", "Mode Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                _mainWindow?.Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Cancelled, "Operation cancelled by user"));
                MessageBox.Show("Operation was cancelled by user.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _mainWindow?.Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Error, "An error occurred"));
                _monitoringService.AddLog(MonitoringLogLevel.Error, $"Unexpected error: {ex.Message}", "UIActionService");
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _mainWindow?.Dispatcher.Invoke(() => {
                    if (_generateButton != null) _generateButton.IsEnabled = true;
                    if (_monitoringService.CurrentStatus.CurrentStatus == StatusType.Running)
                    {
                        _monitoringService.UpdateStatus(StatusType.Idle, "Ready");
                    }
                });

                // Clean up cancellation source
                _currentCancellationSource?.Dispose();
                _currentCancellationSource = null;
            }
        }

        public void HandleCancel()
        {
            try
            {
                if (_currentCancellationSource != null && !_currentCancellationSource.IsCancellationRequested)
                {
                    _currentCancellationSource.Cancel();
                    _monitoringService.UpdateStatus(StatusType.Running, "Cancelling operation...");
                    if (_cancelButton != null) _cancelButton.IsEnabled = false;
                }
                else
                {
                    // No operation is currently running
                    MessageBox.Show("No operation is currently running to cancel.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during cancellation: {ex.Message}", "Cancellation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void HandleReset()
        {
            try
            {
                // Clear YearMonth pickers
                _fromMonthPicker?.Clear();
                _toMonthPicker?.Clear();
                
                // Clear all text boxes
                if (_txtHS != null) _txtHS.Text = "";
                if (_txtPort != null) _txtPort.Text = "";
                if (_txtProduct != null) _txtProduct.Text = "";
                if (_txtExporter != null) _txtExporter.Text = "";
                if (_txtImporter != null) _txtImporter.Text = "";
                if (_txtForCount != null) _txtForCount.Text = "";
                if (_txtForName != null) _txtForName.Text = "";
                if (_txtIEC != null) _txtIEC.Text = "";

                // Reset to Export mode
                if (_rbExport != null) _rbExport.IsChecked = true;
                if (_rbImport != null) _rbImport.IsChecked = false;
                
                // Update status
                _monitoringService.UpdateStatus(StatusType.Idle, "All input fields have been cleared. Ready.");

                MessageBox.Show("All input fields have been cleared.", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during reset: {ex.Message}", "Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ExportInputs GetExportInputs()
        {
            return new ExportInputs(
                _fromMonthPicker?.SelectedYearMonth ?? "",
                _toMonthPicker?.SelectedYearMonth ?? "",
                SplitCommaSeparated(_txtPort?.Text),
                SplitCommaSeparated(_txtHS?.Text),
                SplitCommaSeparated(_txtProduct?.Text),
                SplitCommaSeparated(_txtExporter?.Text),
                SplitCommaSeparated(_txtIEC?.Text),
                SplitCommaSeparated(_txtForCount?.Text),
                SplitCommaSeparated(_txtForName?.Text)
            );
        }

        private ImportInputs GetImportInputs()
        {
            return new ImportInputs(
                _fromMonthPicker?.SelectedYearMonth ?? "",
                _toMonthPicker?.SelectedYearMonth ?? "",
                SplitCommaSeparated(_txtPort?.Text),
                SplitCommaSeparated(_txtHS?.Text),
                SplitCommaSeparated(_txtProduct?.Text),
                SplitCommaSeparated(_txtImporter?.Text),
                SplitCommaSeparated(_txtIEC?.Text),
                SplitCommaSeparated(_txtForCount?.Text),
                SplitCommaSeparated(_txtForName?.Text)
            );
        }
        
        private System.Collections.Generic.List<string> SplitCommaSeparated(string? input)
        {
            // If input is null, empty or just whitespace, return a list with a single "%" wildcard
            // This matches the VB behavior: If Txt_HS.Text = "" Then Txt_HS.Text = "%"
            if (string.IsNullOrWhiteSpace(input))
                return new() { "%" };
                
            // If there are no commas, return as a single item list
            if (!input.Contains(','))
                return new() { input.Trim() };
                
            // Split by comma, trim each value, and ensure no empty values
            var result = input.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
                
            // If after processing we have no items (e.g., only had empty values), return a "%" wildcard
            if (result.Count == 0)
                result.Add("%");
                
            return result;
        }
    }
}
