using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Data;
using System.Threading.Tasks;
using TradeDataHub.Core;
using TradeDataHub.Features.Export; // Ensure ExcelService is in scope
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Features.Import;
using System.Threading;
using TradeDataHub.Core.Cancellation;
using System.Windows.Input;
using System.Windows.Media;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;
using MonitoringLogLevel = TradeDataHub.Features.Monitoring.Models.LogLevel;
using System.IO;

namespace TradeDataHub
{
    public partial class MainWindow : Window
    {
    private readonly ExportExcelService _excelService;
    private readonly ImportExcelService _importService;
    private readonly ICancellationManager _cancellationManager;
    private readonly MonitoringService _monitoringService;
    private CancellationTokenSource? _currentCancellationSource;

        public MainWindow()
        {
            InitializeComponent();
            _excelService = new ExportExcelService();
            _importService = new ImportExcelService();
            _cancellationManager = new CancellationManager();
            _monitoringService = MonitoringService.Instance;
            
            // Initialize to Basic mode (hide advanced parameters)
            AdvancedParametersGrid.Visibility = Visibility.Collapsed;
            
            // Set initial status
            _monitoringService.UpdateStatus(StatusType.Idle, "Application ready");
            
            ApplyModeUI();
            rbExport.Checked += (_,__) => { ApplyModeUI(); };
            rbImport.Checked += (_,__) => { ApplyModeUI(); };
            
            // Add keyboard event handler
            this.KeyDown += MainWindow_KeyDown;
        }

        private void ApplyModeUI()
        {
            if (rbExport.IsChecked == true)
            {
                Lbl_Exporter.Visibility = Visibility.Visible;
                Txt_Exporter.Visibility = Visibility.Visible;
                Lbl_Importer.Visibility = Visibility.Collapsed;
                Txt_Importer.Visibility = Visibility.Collapsed;
            }
            else
            {
                Lbl_Exporter.Visibility = Visibility.Collapsed;
                Txt_Exporter.Visibility = Visibility.Collapsed;
                Lbl_Importer.Visibility = Visibility.Visible;
                Txt_Importer.Visibility = Visibility.Visible;
            }
        }

        private void ToggleSwitch_Click(object sender, MouseButtonEventArgs e)
        {
            // Check current state based on AdvancedParametersGrid visibility
            bool isBasicMode = AdvancedParametersGrid.Visibility == Visibility.Collapsed;
            
            if (isBasicMode)
            {
                // Switch to Advanced mode
                AdvancedParametersGrid.Visibility = Visibility.Visible;
                
                // Move active indicator to right (Advanced)
                ActiveIndicator.SetValue(Grid.ColumnProperty, 1);
                
                // Update text colors
                BasicText.Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // #555555
                AdvancedText.Foreground = new SolidColorBrush(Colors.White);
                
                // Update menu checkboxes
                MenuBasicView.IsChecked = false;
                MenuAdvancedView.IsChecked = true;
            }
            else
            {
                // Switch to Basic mode
                AdvancedParametersGrid.Visibility = Visibility.Collapsed;
                
                // Move active indicator to left (Basic)
                ActiveIndicator.SetValue(Grid.ColumnProperty, 0);
                
                // Update text colors
                BasicText.Foreground = new SolidColorBrush(Colors.White);
                AdvancedText.Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // #555555
                
                // Update menu checkboxes
                MenuBasicView.IsChecked = true;
                MenuAdvancedView.IsChecked = false;
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean up any previous cancellation source
            _currentCancellationSource?.Dispose();
            _currentCancellationSource = new CancellationTokenSource();

            _monitoringService.UpdateStatus(StatusType.Running, "Processing...");
            GenerateButton.IsEnabled = false;

            try
            {
                // 🎯 Check which process type is selected
                if (rbImport.IsChecked == true)
                {
                    await RunImportProcess(_currentCancellationSource.Token);
                }

                if (rbExport.IsChecked == true)
                {
                    // Export is selected - run the existing export process
                    await RunExportProcess(_currentCancellationSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Cancelled, "Operation cancelled by user"));
                MessageBox.Show("Operation was cancelled by user.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Error, "An error occurred"));
                _monitoringService.AddLog(MonitoringLogLevel.Error, $"Unexpected error: {ex.Message}", "GenerateButton");
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Dispatcher.Invoke(() => {
                    GenerateButton.IsEnabled = true;
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

        private async Task RunExportProcess(CancellationToken cancellationToken)
        {
            var fromMonth = Txt_Frommonth.Text;
            var toMonth = Txtmonthto.Text;

            // Enhanced parameter validation using ParameterHelper
            var validation = ExportParameterHelper.ValidateExportParameters(
                fromMonth, toMonth, ExportParameterHelper.WILDCARD, ExportParameterHelper.WILDCARD, ExportParameterHelper.WILDCARD, ExportParameterHelper.WILDCARD, ExportParameterHelper.WILDCARD, ExportParameterHelper.WILDCARD, ExportParameterHelper.WILDCARD);
            
            if (!validation.IsValid)
            {
                string errorMessage = "Parameter Validation Failed:\n" + string.Join("\n", validation.Errors);
                MessageBox.Show(errorMessage, "Invalid Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hsCodes = ExportParameterHelper.ParseFilterList(Txt_HS.Text);
            var ports = ExportParameterHelper.ParseFilterList(txt_Port.Text);
            var products = ExportParameterHelper.ParseFilterList(Txt_Product.Text);
            var exporters = ExportParameterHelper.ParseFilterList(Txt_Exporter.Text);
            var foreignCountries = ExportParameterHelper.ParseFilterList(txt_ForCount.Text);
            var foreignNames = ExportParameterHelper.ParseFilterList(Txt_ForName.Text);
            var iecs = ExportParameterHelper.ParseFilterList(Txt_IEC.Text);

            int filesGenerated = 0;
            int combinationsProcessed = 0;
            int combinationsSkipped = 0;
            int skippedNoData = 0;
            int skippedRowLimit = 0;
            int cancelledCombinations = 0;

            await Task.Run(() =>
            {
                foreach (var port in ports)
                {
                    foreach (var hsCode in hsCodes)
                    {
                        foreach (var product in products)
                        {
                            foreach (var exporter in exporters)
                            {
                                foreach (var iec in iecs)
                                {
                                    foreach (var country in foreignCountries)
                                    {
                                        foreach (var name in foreignNames)
                                        {
                                            // Check for cancellation at the start of each combination
                                            if (cancellationToken.IsCancellationRequested)
                                            {
                                                cancellationToken.ThrowIfCancellationRequested();
                                            }

                                            combinationsProcessed++;
                                            Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Running, $"Processing combination {combinationsProcessed}...", "Export"));

                                            try
                                            {
                                                // Step 1: Execute SP and get data reader with row count (single SP execution)
                                                var result = _excelService.CreateReport(combinationsProcessed, fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, cancellationToken);
                                                
                                                if (result.Success)
                                                {
                                                    filesGenerated++;
                                                }
                                                else if (result.IsCancelled)
                                                {
                                                    cancelledCombinations++;
                                                    cancellationToken.ThrowIfCancellationRequested();
                                                }
                                                else
                                                {
                                                    combinationsSkipped++;
                                                    
                                                    // Track skip reasons for better completion message
                                                    if (result.SkipReason == SkipReason.NoData)
                                                    {
                                                        skippedNoData++;
                                                    }
                                                    else if (result.SkipReason == SkipReason.ExcelRowLimit)
                                                    {
                                                        skippedRowLimit++;
                                                    }
                                                }
                                            }
                                            catch (OperationCanceledException)
                                            {
                                                cancelledCombinations++;
                                                throw; // Re-throw to exit the loops
                                            }
                                            catch (Exception ex)
                                            {
                                                // Log individual combination errors but continue processing
                                                var errorMsg = $"Error processing combination {combinationsProcessed}: {ex.Message}";
                                                Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Error, errorMsg, "Export"));
                                                _monitoringService.AddLog(MonitoringLogLevel.Error, errorMsg, "Export");
                                                System.Diagnostics.Debug.WriteLine($"ERROR: {errorMsg}");
                                                System.Diagnostics.Debug.WriteLine($"Filters - HSCode:{hsCode}, Product:{product}, IEC:{iec}, Exporter:{exporter}, Country:{country}, Name:{name}, Port:{port}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }, cancellationToken);

            // Check if operation was cancelled
            if (cancellationToken.IsCancellationRequested)
            {
                Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Cancelled, $"Export cancelled - {filesGenerated} files generated before cancellation"));
                return;
            }

            // Log processing summary
            SkippedDatasetLogger.LogProcessingSummary(combinationsProcessed, filesGenerated, combinationsSkipped);

            // Create enhanced completion summary with clear skip reasons
            var summaryMessage = "Export Processing Complete\n\n";
            
            // Success metrics
            summaryMessage += $"Files Generated: {filesGenerated:N0} Excel files created successfully\n";
            summaryMessage += $"Total Processed: {combinationsProcessed:N0} parameter combinations checked\n\n";
            
            // Skip details with clear explanations
            if (combinationsSkipped > 0)
            {
                summaryMessage += $"Skipped Combinations: {combinationsSkipped:N0} total\n";
                
                if (skippedNoData > 0)
                {
                    summaryMessage += $"  • No Data Found: {skippedNoData:N0} combinations had zero matching records\n";
                }
                
                if (skippedRowLimit > 0)
                {
                    summaryMessage += $"  • Excel Row Limit: {skippedRowLimit:N0} combinations exceeded 1,048,575 row limit\n";
                }
                
                summaryMessage += $"\nNote: Skipped datasets are logged in: Logs\\SkippedDatasets_{DateTime.Now:yyyyMMdd}.log\n";
            }
            
            // Performance summary
            if (filesGenerated > 0)
            {
                summaryMessage += $"\nSuccess Rate: {(double)filesGenerated / combinationsProcessed * 100:F1}% of combinations produced files";
            }

            // Determine message type and icon based on results
            var messageType = MessageBoxImage.Information;
            var messageTitle = "Export Batch Processing Complete";
            
            if (filesGenerated == 0)
            {
                messageType = MessageBoxImage.Warning;
                messageTitle = "No Files Generated";
                summaryMessage += "\n\nNext Steps: Review your filter criteria - all combinations resulted in no data or exceeded limits.";
            }
            else if (skippedRowLimit > 0)
            {
                messageType = MessageBoxImage.Warning;
                messageTitle = "Export Processing Complete - Some Data Limits Exceeded";
                summaryMessage += "\n\nSuggestion: Consider adding more specific filters to reduce row counts for large datasets.";
            }

            Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Completed, GetStatusSummary(filesGenerated, skippedNoData, skippedRowLimit)));
            MessageBox.Show(summaryMessage, messageTitle, MessageBoxButton.OK, messageType);
        }

        private string GetStatusSummary(int filesGenerated, int skippedNoData, int skippedRowLimit)
        {
            if (filesGenerated == 0)
            {
                if (skippedNoData > 0 && skippedRowLimit == 0)
                    return "Complete: No files generated - all combinations had no data";
                else if (skippedRowLimit > 0 && skippedNoData == 0)
                    return "Complete: No files generated - all combinations exceeded row limits";
                else if (skippedNoData > 0 && skippedRowLimit > 0)
                    return $"Complete: No files generated - {skippedNoData} no data, {skippedRowLimit} over limits";
                else
                    return "Complete: No files generated";
            }
            else
            {
                var totalSkipped = skippedNoData + skippedRowLimit;
                if (totalSkipped == 0)
                    return $"Complete: {filesGenerated} files generated successfully";
                else
                    return $"Complete: {filesGenerated} files, {totalSkipped} skipped ({skippedNoData} no data, {skippedRowLimit} over limits)";
            }
        }

        private async Task RunImportProcess(CancellationToken cancellationToken)
        {
            var fromMonth = Txt_Frommonth.Text;
            var toMonth = Txtmonthto.Text;

            // Reuse export parameter helper for validation (months only for now)
            var validation = ImportParameterHelper.ValidateImportParameters(
                fromMonth, toMonth, ImportParameterHelper.WILDCARD, ImportParameterHelper.WILDCARD, ImportParameterHelper.WILDCARD, ImportParameterHelper.WILDCARD, ImportParameterHelper.WILDCARD, ImportParameterHelper.WILDCARD, ImportParameterHelper.WILDCARD);

            if (!validation.IsValid)
            {
                string errorMessage = "Parameter Validation Failed:\n" + string.Join("\n", validation.Errors);
                MessageBox.Show(errorMessage, "Invalid Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse lists (Txt_Exporter textbox is repurposed as Importer list when Import mode selected)
            var hsCodes = ImportParameterHelper.ParseFilterList(Txt_HS.Text);
            var ports = ImportParameterHelper.ParseFilterList(txt_Port.Text);
            var products = ImportParameterHelper.ParseFilterList(Txt_Product.Text);
            var importers = ImportParameterHelper.ParseFilterList(Txt_Importer.Text); // importer names
            var foreignCountries = ImportParameterHelper.ParseFilterList(txt_ForCount.Text);
            var foreignNames = ImportParameterHelper.ParseFilterList(Txt_ForName.Text);
            var iecs = ImportParameterHelper.ParseFilterList(Txt_IEC.Text);

            int filesGenerated = 0;
            int combinationsProcessed = 0;
            int combinationsSkipped = 0;
            int skippedNoData = 0;
            int skippedRowLimit = 0;
            int cancelledCombinations = 0;

            await Task.Run(() =>
            {
                foreach (var port in ports)
                {
                    foreach (var hsCode in hsCodes)
                    {
                        foreach (var product in products)
                        {
                            foreach (var importer in importers)
                            {
                                foreach (var iec in iecs)
                                {
                                    foreach (var country in foreignCountries)
                                    {
                                        foreach (var name in foreignNames)
                                        {
                                            // Check for cancellation at the start of each combination
                                            if (cancellationToken.IsCancellationRequested)
                                            {
                                                cancellationToken.ThrowIfCancellationRequested();
                                            }

                                            combinationsProcessed++;
                                            var comboNumber = combinationsProcessed; // capture for closure
                                            Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Running, $"Processing (Import) combination {comboNumber}...", "Import"));

                                            try
                                            {
                                                var result = _importService.CreateReport(fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, cancellationToken);
                                                if (result.Success)
                                                {
                                                    filesGenerated++;
                                                }
                                                else if (result.IsCancelled)
                                                {
                                                    cancelledCombinations++;
                                                    cancellationToken.ThrowIfCancellationRequested();
                                                }
                                                else
                                                {
                                                    combinationsSkipped++;
                                                    if (result.SkipReason == "NoData")
                                                    {
                                                        skippedNoData++;
                                                        SkippedDatasetLogger.LogSkippedDataset(comboNumber, 0, fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, "NoData");
                                                    }
                                                    else if (result.SkipReason == "ExcelRowLimit")
                                                    {
                                                        skippedRowLimit++;
                                                        SkippedDatasetLogger.LogSkippedDataset(comboNumber, result.RowCount, fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, "RowLimit");
                                                    }
                                                }
                                            }
                                            catch (OperationCanceledException)
                                            {
                                                cancelledCombinations++;
                                                throw; // Re-throw to exit the loops
                                            }
                                            catch (Exception ex)
                                            {
                                                var errorMsg = $"Import error combination {comboNumber}: {ex.Message}";
                                                Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Error, errorMsg, "Import"));
                                                _monitoringService.AddLog(MonitoringLogLevel.Error, errorMsg, "Import");
                                                System.Diagnostics.Debug.WriteLine($"ERROR: {errorMsg}");
                                                System.Diagnostics.Debug.WriteLine($"Filters - HSCode:{hsCode}, Product:{product}, IEC:{iec}, Importer:{importer}, Country:{country}, Name:{name}, Port:{port}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }, cancellationToken);

            // Check if operation was cancelled
            if (cancellationToken.IsCancellationRequested)
            {
                Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Cancelled, $"Import cancelled - {filesGenerated} files generated before cancellation"));
                return;
            }

            SkippedDatasetLogger.LogProcessingSummary(combinationsProcessed, filesGenerated, combinationsSkipped);

            var summaryMessage = "Import Processing Complete\n\n";
            summaryMessage += $"Files Generated: {filesGenerated:N0} Excel files created successfully\n";
            summaryMessage += $"Total Processed: {combinationsProcessed:N0} parameter combinations checked\n\n";

            if (combinationsSkipped > 0)
            {
                summaryMessage += $"Skipped Combinations: {combinationsSkipped:N0} total\n";
                if (skippedNoData > 0)
                    summaryMessage += $"  • No Data Found: {skippedNoData:N0} combinations had zero matching records\n";
                if (skippedRowLimit > 0)
                    summaryMessage += $"  • Excel Row Limit: {skippedRowLimit:N0} combinations exceeded 1,048,575 row limit\n";
                summaryMessage += $"\nNote: Skipped datasets are logged in: Logs\\SkippedDatasets_{DateTime.Now:yyyyMMdd}.log\n";
            }

            if (filesGenerated > 0)
            {
                summaryMessage += $"\nSuccess Rate: {(double)filesGenerated / combinationsProcessed * 100:F1}% of combinations produced files";
            }

            var messageType = MessageBoxImage.Information;
            var messageTitle = "Import Batch Complete";
            if (filesGenerated == 0)
            {
                messageType = MessageBoxImage.Warning;
                messageTitle = "No Files Generated";
                summaryMessage += "\n\nNext Steps: Review your filter criteria - all combinations resulted in no data or exceeded limits.";
            }
            else if (skippedRowLimit > 0)
            {
                messageType = MessageBoxImage.Warning;
                messageTitle = "Processing Complete - Some Data Limits Exceeded";
                summaryMessage += "\n\nSuggestion: Consider adding more specific filters to reduce row counts for large datasets.";
            }

            var importStatusMessage = filesGenerated == 0
                ? (skippedNoData > 0 && skippedRowLimit == 0 ? "Import complete: All combinations had no data" :
                   skippedRowLimit > 0 && skippedNoData == 0 ? "Import complete: All combinations exceeded row limits" :
                   (skippedNoData > 0 && skippedRowLimit > 0 ? $"Import complete: 0 files - {skippedNoData} no data, {skippedRowLimit} over limits" : "Import complete: No files"))
                : (combinationsSkipped == 0 ? $"Import complete: {filesGenerated} files" : $"Import complete: {filesGenerated} files, {combinationsSkipped} skipped");

            Dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Completed, importStatusMessage));

            MessageBox.Show(summaryMessage, messageTitle, MessageBoxButton.OK, messageType);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCancellationSource != null && !_currentCancellationSource.IsCancellationRequested)
                {
                    _currentCancellationSource.Cancel();
                    _monitoringService.UpdateStatus(StatusType.Running, "Cancelling operation...");
                    CancelButton.IsEnabled = false;
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

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear all text boxes
                Txt_Frommonth.Text = "";
                Txtmonthto.Text = "";
                Txt_HS.Text = "";
                txt_Port.Text = "";
                Txt_Product.Text = "";
                Txt_Exporter.Text = "";
                Txt_Importer.Text = "";
                txt_ForCount.Text = "";
                Txt_ForName.Text = "";
                Txt_IEC.Text = "";

                
                // Update status
                _monitoringService.UpdateStatus(StatusType.Idle, "All input fields have been cleared. Ready.");

                MessageBox.Show("All input fields have been cleared.", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during reset: {ex.Message}", "Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Menu Event Handlers

        // File Menu Handlers
        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            // Reset all fields to create a "new" session
            ResetButton_Click(sender, e);
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Open Configuration File",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config")
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    MessageBox.Show($"Configuration file selected: {openFileDialog.FileName}\n\nConfiguration loading will be implemented in future updates.", 
                                  "File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file dialog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get current parameters
                var config = new
                {
                    FromMonth = Txt_Frommonth.Text,
                    ToMonth = Txtmonthto.Text,
                    HSCodes = Txt_HS.Text,
                    PortCodes = txt_Port.Text,
                    Products = Txt_Product.Text,
                    Exporters = Txt_Exporter.Text,
                    Importers = Txt_Importer.Text,
                    ForeignCountries = txt_ForCount.Text,
                    ForeignCompanies = Txt_ForName.Text,
                    IECCodes = Txt_IEC.Text,
                    Mode = rbExport.IsChecked == true ? "Export" : "Import",
                    View = MenuAdvancedView.IsChecked ? "Advanced" : "Basic"
                };

                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "user_settings.json");
                
                System.IO.File.WriteAllText(configPath, json);
                MessageBox.Show($"Current settings saved to: {configPath}", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Configuration As",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config"),
                    FileName = $"TradeDataHub_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var config = new
                    {
                        FromMonth = Txt_Frommonth.Text,
                        ToMonth = Txtmonthto.Text,
                        HSCodes = Txt_HS.Text,
                        PortCodes = txt_Port.Text,
                        Products = Txt_Product.Text,
                        Exporters = Txt_Exporter.Text,
                        Importers = Txt_Importer.Text,
                        ForeignCountries = txt_ForCount.Text,
                        ForeignCompanies = Txt_ForName.Text,
                        IECCodes = Txt_IEC.Text,
                        Mode = rbExport.IsChecked == true ? "Export" : "Import",
                        View = MenuAdvancedView.IsChecked ? "Advanced" : "Basic"
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show($"Configuration saved to: {saveFileDialog.FileName}", "Configuration Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            // Check if there's an ongoing operation
            if (_currentCancellationSource != null && !_currentCancellationSource.IsCancellationRequested)
            {
                var result = MessageBox.Show("An operation is currently running. Do you want to cancel it and exit?", 
                                           "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _currentCancellationSource.Cancel();
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        // Edit Menu Handlers
        private void MenuUndo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to undo in the focused text box
                if (Keyboard.FocusedElement is TextBox textBox)
                {
                    textBox.Undo();
                }
                else
                {
                    // If no textbox is focused, show information about undoable actions
                    var undoInfo = "Undo operations available:\n\n" +
                                  "• Text changes in input fields (Ctrl+Z when field is focused)\n" +
                                  "• Use Reset button to restore all fields to empty state\n" +
                                  "• Cancel button to stop ongoing operations\n\n" +
                                  "Focus on a text field first, then use Ctrl+Z to undo text changes.";
                    
                    MessageBox.Show(undoInfo, "Undo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during undo operation: {ex.Message}", "Undo Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuRedo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to redo in the focused text box
                if (Keyboard.FocusedElement is TextBox textBox)
                {
                    textBox.Redo();
                }
                else
                {
                    var redoInfo = "Redo operations available:\n\n" +
                                  "• Text changes in input fields (Ctrl+Y when field is focused)\n" +
                                  "• Re-run the last successful report generation\n\n" +
                                  "Focus on a text field first, then use Ctrl+Y to redo text changes.";
                    
                    MessageBox.Show(redoInfo, "Redo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during redo operation: {ex.Message}", "Redo Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to cut from focused text box
                if (Keyboard.FocusedElement is TextBox textBox && !string.IsNullOrEmpty(textBox.SelectedText))
                {
                    textBox.Cut();
                    _monitoringService.UpdateStatus(StatusType.Idle, $"Text cut to clipboard");
                }
                else if (Keyboard.FocusedElement is TextBox)
                {
                    MessageBox.Show("No text is selected in the current field.", "Cut", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Please focus on a text field first, then select text to cut.", "Cut", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during cut operation: {ex.Message}", "Cut Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to copy from focused text box
                if (Keyboard.FocusedElement is TextBox textBox && !string.IsNullOrEmpty(textBox.SelectedText))
                {
                    textBox.Copy();
                    _monitoringService.UpdateStatus(StatusType.Idle, $"Text copied to clipboard: '{textBox.SelectedText.Substring(0, Math.Min(textBox.SelectedText.Length, 20))}{(textBox.SelectedText.Length > 20 ? "..." : "")}'");
                }
                else if (Keyboard.FocusedElement is TextBox textBox2 && !string.IsNullOrEmpty(textBox2.Text))
                {
                    // If no text is selected, copy all text from the field
                    textBox2.SelectAll();
                    textBox2.Copy();
                    textBox2.Select(0, 0); // Clear selection
                    _monitoringService.UpdateStatus(StatusType.Idle, $"All text copied from field to clipboard");
                }
                else if (Keyboard.FocusedElement is TextBox)
                {
                    MessageBox.Show("The current field is empty.", "Copy", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // If no textbox is focused, offer to copy application information
                    var result = MessageBox.Show("No text field is focused. Would you like to copy the current application settings to clipboard?", 
                                                "Copy", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        CopyApplicationSettingsToClipboard();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during copy operation: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to paste to focused text box
                if (Keyboard.FocusedElement is TextBox textBox)
                {
                    if (Clipboard.ContainsText())
                    {
                        textBox.Paste();
                        _monitoringService.UpdateStatus(StatusType.Idle, "Text pasted from clipboard");
                    }
                    else
                    {
                        MessageBox.Show("Clipboard does not contain text.", "Paste", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please focus on a text field first to paste text.", "Paste", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during paste operation: {ex.Message}", "Paste Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to select all in focused text box
                if (Keyboard.FocusedElement is TextBox textBox)
                {
                    textBox.SelectAll();
                    _monitoringService.UpdateStatus(StatusType.Idle, "All text selected in current field");
                }
                else
                {
                    var result = MessageBox.Show("No text field is focused. Would you like to select all text in all input fields for easier copying?", 
                                                "Select All", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        SelectAllFieldsContent();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during select all operation: {ex.Message}", "Select All Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuPreferences_Click(object sender, RoutedEventArgs e)
        {
            var prefMessage = "Preferences:\n\n" +
                            "• File locations:\n" +
                            "  - Config: " + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config") + "\n" +
                            "  - Logs: " + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs") + "\n" +
                            "  - Export: " + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EXPORT_Excel") + "\n" +
                            "  - Import: " + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IMPORT_Excel") + "\n\n" +
                            "• Database connection settings are in Config/database.appsettings.json\n" +
                            "• Excel formatting settings are in Config/excelFormatting.json\n\n" +
                            "Advanced preferences dialog will be available in future updates.";
            
            MessageBox.Show(prefMessage, "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Helper methods for Edit menu functionality
        private void CopyApplicationSettingsToClipboard()
        {
            try
            {
                var settings = "Trade Data Hub - Current Settings\n" +
                              "================================\n\n" +
                              $"From Month: {Txt_Frommonth?.Text ?? ""}\n" +
                              $"To Month: {Txtmonthto?.Text ?? ""}\n" +
                              $"HS Codes: {Txt_HS?.Text ?? ""}\n" +
                              $"Port Codes: {txt_Port?.Text ?? ""}\n" +
                              $"Products: {Txt_Product?.Text ?? ""}\n" +
                              $"Exporters: {Txt_Exporter?.Text ?? ""}\n" +
                              $"Importers: {Txt_Importer?.Text ?? ""}\n" +
                              $"Foreign Countries: {txt_ForCount?.Text ?? ""}\n" +
                              $"Foreign Companies: {Txt_ForName?.Text ?? ""}\n" +
                              $"IEC Codes: {Txt_IEC?.Text ?? ""}\n" +
                              $"Mode: {(rbExport?.IsChecked == true ? "Export" : "Import")}\n" +
                              $"View: {(MenuAdvancedView?.IsChecked == true ? "Advanced" : "Basic")}\n" +
                              $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                Clipboard.SetText(settings);
                _monitoringService.UpdateStatus(StatusType.Idle, "Application settings copied to clipboard");
                MessageBox.Show("Current application settings have been copied to clipboard.", "Settings Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying settings to clipboard: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectAllFieldsContent()
        {
            try
            {
                var message = "Select All Fields functionality will be fully implemented once UI binding is restored.\n\n" +
                             "For now, you can focus on individual text fields and use Ctrl+A to select all text in that field.";
                
                MessageBox.Show(message, "Select All Fields", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in select all operation: {ex.Message}", "Select All Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // View Menu Handlers
        private void MenuBasicView_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Basic view (existing functionality)
            AdvancedParametersGrid.Visibility = Visibility.Collapsed;
            MenuBasicView.IsChecked = true;
            MenuAdvancedView.IsChecked = false;
            
            // Update the toggle switch to match
            Grid.SetColumn(ActiveIndicator, 0);
            BasicText.Foreground = new SolidColorBrush(Colors.White);
            AdvancedText.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
        }

        private void MenuAdvancedView_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Advanced view (existing functionality)
            AdvancedParametersGrid.Visibility = Visibility.Visible;
            MenuBasicView.IsChecked = false;
            MenuAdvancedView.IsChecked = true;
            
            // Update the toggle switch to match
            Grid.SetColumn(ActiveIndicator, 1);
            BasicText.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
            AdvancedText.Foreground = new SolidColorBrush(Colors.White);
        }

        private void MenuMonitoringPanel_Click(object sender, RoutedEventArgs e)
        {
            if (MenuMonitoringPanel.IsChecked)
            {
                MonitoringPanel.Visibility = Visibility.Visible;
            }
            else
            {
                MonitoringPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void MenuActivityLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read the log directory from the configuration file
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "Config", "database.appsettings.json");
                
                string? logsPath = null;
                
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    var configData = System.Text.Json.JsonDocument.Parse(configText);
                    
                    if (configData.RootElement.TryGetProperty("DatabaseConfig", out var dbConfig) &&
                        dbConfig.TryGetProperty("LogDirectory", out var logDir))
                    {
                        logsPath = logDir.GetString();
                    }
                }
                
                if (string.IsNullOrEmpty(logsPath))
                {
                    // Fallback to default location if not configured
                    logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                }

                if (Directory.Exists(logsPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logsPath);
                }
                else
                {
                    MessageBox.Show($"Logs directory not found: {logsPath}", "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening logs directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Refresh monitoring status
            _monitoringService.UpdateStatus(StatusType.Idle, "Application refreshed");
            MessageBox.Show("Application view refreshed.", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuFullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.ResizeMode = ResizeMode.CanResize;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
            }
        }

        // Help Menu Handlers
        private void MenuUserManual_Click(object sender, RoutedEventArgs e)
        {
            var manualText = "Trade Data Hub - User Manual\n\n" +
                           "GETTING STARTED:\n" +
                           "1. Set the date range using From Month and To Month (YYYYMM format)\n" +
                           "2. Optionally specify HS Codes for filtering\n" +
                           "3. Choose Export or Import mode\n" +
                           "4. Click 'Generate Reports' to process data\n\n" +
                           "BASIC VIEW:\n" +
                           "- From/To Month: Date range for data (required)\n" +
                           "- HS Codes: Product classification codes (optional)\n\n" +
                           "ADVANCED VIEW:\n" +
                           "- Port Codes: Filter by specific ports\n" +
                           "- Products: Filter by product names\n" +
                           "- Exporters/Importers: Filter by company names\n" +
                           "- Foreign Countries: Filter by country codes\n" +
                           "- Foreign Companies: Filter by foreign company names\n" +
                           "- IEC Codes: Import Export Code filtering\n\n" +
                           "MONITORING:\n" +
                           "- System Monitor shows current application status\n" +
                           "- Activity Monitor displays processing logs\n" +
                           "- Use Cancel button to stop ongoing operations\n\n" +
                           "OUTPUT:\n" +
                           "- Excel files are saved to EXPORT_Excel or IMPORT_Excel folders\n" +
                           "- Log files are saved to Logs folder\n\n" +
                           "For more detailed documentation, please refer to the README.md file.";
            
            MessageBox.Show(manualText, "User Manual", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuQuickStart_Click(object sender, RoutedEventArgs e)
        {
            var quickStartText = "Quick Start Guide\n\n" +
                               "1. BASIC SETUP:\n" +
                               "   • Enter From Month: 202501 (for January 2025)\n" +
                               "   • Enter To Month: 202501 (same month for single month)\n" +
                               "   • Leave HS Codes empty for all products\n\n" +
                               "2. SELECT MODE:\n" +
                               "   • Choose 'Export' for export data analysis\n" +
                               "   • Choose 'Import' for import data analysis\n\n" +
                               "3. GENERATE REPORT:\n" +
                               "   • Click 'Generate Reports' button\n" +
                               "   • Monitor progress in the System Monitor\n" +
                               "   • Check Activity Monitor for detailed logs\n\n" +
                               "4. VIEW RESULTS:\n" +
                               "   • Excel files will be saved automatically\n" +
                               "   • Location: EXPORT_Excel or IMPORT_Excel folder\n" +
                               "   • Check logs in Logs folder for any issues\n\n" +
                               "TIP: Use Advanced View for more detailed filtering options!";
            
            MessageBox.Show(quickStartText, "Quick Start Guide", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            string shortcuts = "Keyboard Shortcuts:\n\n" +
                              "File Operations:\n" +
                              "Ctrl+N - New\n" +
                              "Ctrl+O - Open\n" +
                              "Ctrl+S - Save\n" +
                              "Ctrl+Shift+S - Save As\n" +
                              "Alt+F4 - Exit\n\n" +
                              "Edit Operations:\n" +
                              "Ctrl+Z - Undo\n" +
                              "Ctrl+Y - Redo\n" +
                              "Ctrl+X - Cut\n" +
                              "Ctrl+C - Copy\n" +
                              "Ctrl+V - Paste\n" +
                              "Ctrl+A - Select All\n\n" +
                              "View Operations:\n" +
                              "F5 - Refresh\n" +
                              "F11 - Full Screen\n\n" +
                              "Help:\n" +
                              "F1 - User Manual";
            
            MessageBox.Show(shortcuts, "Keyboard Shortcuts", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuOnlineHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpUrl = "https://github.com/yhassan-git-real/TradeDataHub.02/wiki"; // Assuming GitHub wiki
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = helpUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open online help. Please visit the project repository for documentation.\n\nError: {ex.Message}", 
                              "Online Help", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var updateInfo = "Trade Data Hub Update Check\n\n" +
                               $"Current Version: 1.0.0\n" +
                               $"Build Date: {System.IO.File.GetCreationTime(System.Reflection.Assembly.GetExecutingAssembly().Location):yyyy-MM-dd}\n" +
                               $"Installation Path: {AppDomain.CurrentDomain.BaseDirectory}\n\n" +
                               "To check for updates, please visit:\n" +
                               "https://github.com/yhassan-git-real/TradeDataHub.02/releases\n\n" +
                               "Automatic update checking will be available in future versions.";
                
                MessageBox.Show(updateInfo, "Check for Updates", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for updates: {ex.Message}", "Update Check Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var buildDate = System.IO.File.GetCreationTime(System.Reflection.Assembly.GetExecutingAssembly().Location);
                
                var aboutText = "Trade Data Hub\n" +
                              $"Version {assemblyVersion?.ToString() ?? "1.0.0"}\n" +
                              $"Build Date: {buildDate:yyyy-MM-dd HH:mm}\n\n" +
                              "A professional application for managing and analyzing international trade data.\n\n" +
                              "KEY FEATURES:\n" +
                              "• Export and Import trade data processing\n" +
                              "• Advanced filtering and search capabilities\n" +
                              "• Real-time monitoring and logging\n" +
                              "• Professional Excel report generation\n" +
                              "• Configurable data parameters\n" +
                              "• Comprehensive error handling\n\n" +
                              "SUPPORTED FORMATS:\n" +
                              "• Excel (.xlsx) input and output\n" +
                              "• JSON configuration files\n" +
                              "• SQL Server database connectivity\n\n" +
                              "SYSTEM REQUIREMENTS:\n" +
                              "• Windows 10/11\n" +
                              "• .NET 8.0 Runtime\n" +
                              "• SQL Server connection\n\n" +
                              "© 2025 Trade Data Hub Development Team\n" +
                              "All rights reserved.\n\n" +
                              "For support and documentation:\n" +
                              "https://github.com/yhassan-git-real/TradeDataHub.02";
                
                MessageBox.Show(aboutText, "About Trade Data Hub", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying about information: {ex.Message}", "About Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Keyboard event handler
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                MenuRefresh_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F11)
            {
                MenuFullScreen_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F1)
            {
                MenuUserManual_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.N:
                        MenuNew_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.O:
                        MenuOpen_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.S:
                        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                        {
                            MenuSaveAs_Click(this, new RoutedEventArgs());
                        }
                        else
                        {
                            MenuSave_Click(this, new RoutedEventArgs());
                        }
                        e.Handled = true;
                        break;
                    case Key.Z:
                        MenuUndo_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.Y:
                        MenuRedo_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.X:
                        MenuCut_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.C:
                        MenuCopy_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.V:
                        MenuPaste_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.A:
                        MenuSelectAll_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
        }

        #endregion
    }
}
