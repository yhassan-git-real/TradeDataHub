using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using TradeDataHub.Core.Cancellation;
using TradeDataHub.Core.Controllers;
using TradeDataHub.Core.Database;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Core.Models;
using TradeDataHub.Core.Services;
using TradeDataHub.Core.Validation;
using TradeDataHub.Features.Common.ViewModels;
using TradeDataHub.Features.Export;
using TradeDataHub.Features.Export.Services;
using TradeDataHub.Features.Import;
using TradeDataHub.Features.Import.Services;
using TradeDataHub.Features.Monitoring.Models;
using TradeDataHub.Features.Monitoring.Services;
using MonitoringLogLevel = TradeDataHub.Features.Monitoring.Models.LogLevel;

namespace TradeDataHub
{
    public partial class MainWindow : Window
    {
        #region Fields

        // Core Services
        private readonly ExportExcelService _excelService;
        private readonly ImportExcelService _importService;
        private readonly ICancellationManager _cancellationManager;
        private readonly MonitoringService _monitoringService;

        // Validation Services
        private readonly ExportObjectValidationService _exportObjectValidationService;
        private readonly ImportObjectValidationService _importObjectValidationService;
        private readonly IParameterValidator _parameterValidator;
        private readonly DatabaseObjectValidator _databaseObjectValidator;

        // Controllers
        private readonly IExportController _exportController;
        private readonly IImportController _importController;
        private readonly IUIService _uiService;
        private readonly IMenuService _menuService;

        // View Models
        private readonly DbObjectSelectorViewModel _exportDbObjectViewModel;
        private readonly DbObjectSelectorViewModel _importDbObjectViewModel;

        // State Management
        private CancellationTokenSource? _currentCancellationSource;

        #endregion

        #region Data Transfer Object Methods

        private TradeDataHub.Core.Models.ExportInputs GetExportInputs()
        {
            return InputBinder.GetExportInputs(
                FromMonthPicker,
                ToMonthPicker,
                txt_Port,
                Txt_HS,
                Txt_Product,
                Txt_Exporter,
                Txt_IEC,
                txt_ForCount,
                Txt_ForName
            );
        }

        private TradeDataHub.Core.Models.ImportInputs GetImportInputs()
        {
            return InputBinder.GetImportInputs(
                FromMonthPicker,
                ToMonthPicker,
                txt_Port,
                Txt_HS,
                Txt_Product,
                Txt_Importer,
                Txt_IEC,
                txt_ForCount,
                Txt_ForName
            );
        }

        #endregion

        #region Validation Helper Methods

        private ExportParameterHelper.ValidationResult ValidateExportMonths(string fromMonth, string toMonth)
        {
            var exportInputs = new TradeDataHub.Core.Models.ExportInputs(fromMonth, toMonth, new(), new(), new(), new(), new(), new(), new());
            return _parameterValidator.ValidateExport(exportInputs);
        }

        private ImportParameterHelper.ValidationResult ValidateImportMonths(string fromMonth, string toMonth)
        {
            var importInputs = new TradeDataHub.Core.Models.ImportInputs(fromMonth, toMonth, new(), new(), new(), new(), new(), new(), new());
            return _parameterValidator.ValidateImport(importInputs);
        }

        #endregion

        #region Configuration Methods

        private SharedDatabaseSettings LoadSharedDatabaseSettings()
        {
            const string json = "Config/database.appsettings.json";
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<SharedDatabaseSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
            return root.DatabaseConfig;
        }

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            _excelService = new ExportExcelService();
            _importService = new ImportExcelService();
            _cancellationManager = new CancellationManager();
            _monitoringService = MonitoringService.Instance;
            
            // Initialize menu service
            _menuService = new MenuService();
            
            // Initialize validation services
            _exportObjectValidationService = new ExportObjectValidationService(_excelService.ExportSettings);
            _importObjectValidationService = new ImportObjectValidationService(_importService.ImportSettings);
            _parameterValidator = new TradeDataHub.Core.Validation.ParameterValidator();
            
            // Initialize controllers
            _exportController = new ExportController(
                _excelService,
                _parameterValidator,
                _monitoringService,
                _exportObjectValidationService,
                this.Dispatcher);
                
            _importController = new ImportController(
                _importService,
                _parameterValidator,
                _monitoringService,
                _importObjectValidationService,
                this.Dispatcher);
            
            // Initialize database object validator
            var dbSettings = LoadSharedDatabaseSettings();
            _databaseObjectValidator = new Core.Database.DatabaseObjectValidator(dbSettings.ConnectionString);
            
            // Initialize UI service
            _uiService = new UIService(_databaseObjectValidator, _monitoringService);
            
            // Initialize view models for database object selection
            _exportDbObjectViewModel = new DbObjectSelectorViewModel(
                _exportObjectValidationService.GetAvailableViews(),
                _exportObjectValidationService.GetAvailableStoredProcedures(),
                _exportObjectValidationService.GetDefaultViewName(),
                _exportObjectValidationService.GetDefaultStoredProcedureName());
                
            _importDbObjectViewModel = new DbObjectSelectorViewModel(
                _importObjectValidationService.GetAvailableViews(),
                _importObjectValidationService.GetAvailableStoredProcedures(),
                _importObjectValidationService.GetDefaultViewName(),
                _importObjectValidationService.GetDefaultStoredProcedureName());
            
            // Initialize to Basic mode (hide additional parameters)
            // AdvancedParametersGrid.Visibility = Visibility.Collapsed;
            
            // Initialize UI service with controls - moved to Loaded event
            // _uiService.Initialize(Lbl_Exporter, Txt_Exporter, Lbl_Importer, Txt_Importer, ViewComboBox, StoredProcedureComboBox);
            
            // Set initial status
            _monitoringService.UpdateStatus(StatusType.Idle, "Application ready");
            
            // Apply UI mode after everything is initialized
            // Use Loaded event to ensure all XAML controls are fully loaded
            this.Loaded += (sender, e) => {
                // Initialize UI controls after they're loaded
                AdvancedParametersGrid.Visibility = Visibility.Collapsed;
                _uiService.Initialize(Lbl_Exporter, Txt_Exporter, Lbl_Importer, Txt_Importer, ViewComboBox, StoredProcedureComboBox);
                
                // Initialize menu service
                _menuService.Initialize(this);
                
                // Wire up the RadioButton event handlers after initialization
                rbExport.Checked += ProcessType_CheckedChanged;
                rbImport.Checked += ProcessType_CheckedChanged;
                
                // Apply the initial UI state
                ApplyModeUI();
            };
            
            // Add keyboard event handler
            this.KeyDown += MainWindow_KeyDown;
        }

        private void ApplyModeUI()
        {
            // Guard against calls during initialization
            if (_uiService == null || _exportDbObjectViewModel == null || _importDbObjectViewModel == null)
                return;
                
            bool isExportMode = rbExport.IsChecked == true;
            _uiService.ApplyModeUI(isExportMode, _exportDbObjectViewModel, _importDbObjectViewModel);
        }
        
        private void ValidateSelectedDatabaseObjects(string viewName, string storedProcedureName)
        {
            // Guard against calls during initialization
            if (_uiService == null)
                return;
                
            _uiService.ValidateSelectedDatabaseObjects(viewName, storedProcedureName);
        }
        
        private void ProcessType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ApplyModeUI();
        }
        
        private void ViewComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is DbObjectOption selectedView)
            {
                bool isExportMode = this.rbExport.IsChecked == true;
                _uiService.HandleViewSelectionChanged(selectedView, isExportMode, _exportDbObjectViewModel, _importDbObjectViewModel);
            }
        }
        
        private void StoredProcedureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is DbObjectOption selectedSP)
            {
                bool isExportMode = this.rbExport.IsChecked == true;
                _uiService.HandleStoredProcedureSelectionChanged(selectedSP, isExportMode, _exportDbObjectViewModel, _importDbObjectViewModel);
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
                    // Import is selected - use new ImportController
                    var importInputs = GetImportInputs();
                    var selectedView = _importDbObjectViewModel.SelectedView?.Name ?? "";
                    var selectedSP = _importDbObjectViewModel.SelectedStoredProcedure?.Name ?? "";
                    await _importController.RunAsync(importInputs, _currentCancellationSource.Token, selectedView, selectedSP);
                }
                else if (rbExport.IsChecked == true)
                {
                    // Export is selected - use new ExportController
                    var exportInputs = GetExportInputs();
                    var selectedView = _exportDbObjectViewModel.SelectedView?.Name ?? "";
                    var selectedSP = _exportDbObjectViewModel.SelectedStoredProcedure?.Name ?? "";
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
                // Clear YearMonth pickers
                FromMonthPicker.Clear();
                ToMonthPicker.Clear();
                
                // Clear all text boxes
                Txt_HS.Text = "";
                txt_Port.Text = "";
                Txt_Product.Text = "";
                Txt_Exporter.Text = "";
                Txt_Importer.Text = "";
                txt_ForCount.Text = "";
                Txt_ForName.Text = "";
                Txt_IEC.Text = "";

                // Reset to Export mode
                rbExport.IsChecked = true;
                rbImport.IsChecked = false;
                
                // Update status
                _monitoringService.UpdateStatus(StatusType.Idle, "All input fields have been cleared. Ready.");

                MessageBox.Show("All input fields have been cleared.", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during reset: {ex.Message}", "Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Public Methods for Services

        /// <summary>
        /// Public method to handle reset functionality for menu service
        /// </summary>
        public void HandleResetFields()
        {
            ResetButton_Click(this, new RoutedEventArgs());
        }

        #endregion

        #region Menu Event Handlers

        // File Menu Handlers
        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            _menuService.HandleNewCommand();
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            _menuService.HandleOpenCommand();
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            _menuService.HandleSaveCommand();
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            _menuService.HandleSaveAsCommand();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            _menuService.HandleExitCommand(this);
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
                              $"From Month: {FromMonthPicker?.SelectedYearMonth ?? ""}\n" +
                              $"To Month: {ToMonthPicker?.SelectedYearMonth ?? ""}\n" +
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
        private void ToggleSwitch_Click(object sender, MouseButtonEventArgs e)
        {
            // Check current state based on AdvancedParametersGrid visibility
            bool isBasicMode = AdvancedParametersGrid.Visibility == Visibility.Collapsed;
            
            if (isBasicMode)
            {
                SwitchToAllMode();
            }
            else
            {
                SwitchToBasicMode();
            }
        }

        private void SwitchToAllMode()
        {
            // Show all parameters
            AdvancedParametersGrid.Visibility = Visibility.Visible;
            
            // Move active indicator to right (All mode)
            ActiveIndicator.SetValue(Grid.ColumnProperty, 1);
            
            // Update colors for All mode
            BasicText.Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)); // #6C757D
            BasicIcon.Fill = new SolidColorBrush(Color.FromRgb(108, 117, 125));
            AllText.Foreground = new SolidColorBrush(Colors.White);
            AllIcon.Fill = new SolidColorBrush(Colors.White);
            
            // Update menu checkboxes
            if (MenuBasicView != null) MenuBasicView.IsChecked = false;
            if (MenuAdvancedView != null) MenuAdvancedView.IsChecked = true;
        }

        private void SwitchToBasicMode()
        {
            // Hide additional parameters
            AdvancedParametersGrid.Visibility = Visibility.Collapsed;
            
            // Move active indicator to left (Basic mode)
            ActiveIndicator.SetValue(Grid.ColumnProperty, 0);
            
            // Update colors for Basic mode
            BasicText.Foreground = new SolidColorBrush(Colors.White);
            BasicIcon.Fill = new SolidColorBrush(Colors.White);
            AllText.Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)); // #6C757D
            AllIcon.Fill = new SolidColorBrush(Color.FromRgb(108, 117, 125));
            
            // Update menu checkboxes
            if (MenuBasicView != null) MenuBasicView.IsChecked = true;
            if (MenuAdvancedView != null) MenuAdvancedView.IsChecked = false;
        }

        private void MenuBasicView_Click(object sender, RoutedEventArgs e)
        {
            SwitchToBasicMode();
        }

        private void MenuAdvancedView_Click(object sender, RoutedEventArgs e)
        {
            SwitchToAllMode();
        }

        private void MenuMonitoringPanel_Click(object sender, RoutedEventArgs e)
        {
            // This handler is shared by the MenuItem (checkable) and the header ToggleButton.
            // During InitializeComponent() the ToggleButton's Checked event can fire before
            // the MonitoringPanel element (row 5) has been constructed, leading to a null ref.
            if (MonitoringPanel == null)
            {
                // Defer until layout pass – store desired visibility and apply once loaded.
                // Use dispatcher to run after initialization if we can still determine state.
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (MonitoringPanel == null) return; // still not ready – bail safely
                    ApplyMonitoringPanelVisibility(sender);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            ApplyMonitoringPanelVisibility(sender);
        }

        private void ApplyMonitoringPanelVisibility(object sender)
        {
            bool isChecked = false;

            // Prefer the sender if it's a ToggleButton / MenuItem to avoid relying on other controls being initialized
            switch (sender)
            {
                case System.Windows.Controls.Primitives.ToggleButton tb:
                    isChecked = tb.IsChecked == true;
                    break;
                case MenuItem mi:
                    isChecked = mi.IsChecked;
                    break;
                default:
                    if (MenuMonitoringPanel != null)
                        isChecked = MenuMonitoringPanel.IsChecked; // fallback
                    break;
            }

            MonitoringPanel.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;

            // Keep the MenuItem and the ToggleButton (if both exist) in sync without causing recursion.
            if (MenuMonitoringPanel != null && MenuMonitoringPanel.IsChecked != isChecked)
                MenuMonitoringPanel.IsChecked = isChecked;
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

        #endregion
    }
}
