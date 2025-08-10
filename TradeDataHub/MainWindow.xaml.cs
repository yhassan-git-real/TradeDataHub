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

        private readonly ServiceContainer _services;

        #endregion

        #region Validation Helper Methods

        private ExportParameterHelper.ValidationResult ValidateExportMonths(string fromMonth, string toMonth)
        {
            var exportInputs = new TradeDataHub.Core.Models.ExportInputs(fromMonth, toMonth, new(), new(), new(), new(), new(), new(), new());
            return _services.ParameterValidator.ValidateExport(exportInputs);
        }

        private ImportParameterHelper.ValidationResult ValidateImportMonths(string fromMonth, string toMonth)
        {
            var importInputs = new TradeDataHub.Core.Models.ImportInputs(fromMonth, toMonth, new(), new(), new(), new(), new(), new(), new());
            return _services.ParameterValidator.ValidateImport(importInputs);
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
            
            // Initialize all services through container
            _services = new ServiceContainer();
            _services.InitializeServices(this);
            
            // Apply UI mode after everything is initialized
            // Use Loaded event to ensure all XAML controls are fully loaded
            this.Loaded += OnWindowLoaded;
            
            // Add keyboard event handler
            this.KeyDown += MainWindow_KeyDown;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize UI controls after they're loaded
            var advancedParametersGrid = this.FindName("AdvancedParametersGrid") as System.Windows.Controls.Grid;
            if (advancedParametersGrid != null)
            {
                advancedParametersGrid.Visibility = Visibility.Collapsed;
            }

            // Get UI control references using FindName
            var lblExporter = this.FindName("Lbl_Exporter") as TextBlock;
            var txtExporter = this.FindName("Txt_Exporter") as TextBox;
            var lblImporter = this.FindName("Lbl_Importer") as TextBlock;
            var txtImporter = this.FindName("Txt_Importer") as TextBox;
            var viewComboBox = this.FindName("ViewComboBox") as ComboBox;
            var storedProcedureComboBox = this.FindName("StoredProcedureComboBox") as ComboBox;
            
            // Initialize UIService only if all required controls are found
            if (lblExporter != null && txtExporter != null && lblImporter != null && 
                txtImporter != null && viewComboBox != null && storedProcedureComboBox != null)
            {
                _services.UIService.Initialize(lblExporter, txtExporter, lblImporter, txtImporter, viewComboBox, storedProcedureComboBox);
            }
            
            // Initialize UIActionService with window reference for control access
            _services.UIActionService.Initialize(this);
            
            // Wire up the RadioButton event handlers after initialization
            var rbExport = this.FindName("rbExport") as System.Windows.Controls.RadioButton;
            var rbImport = this.FindName("rbImport") as System.Windows.Controls.RadioButton;
            if (rbExport != null) rbExport.Checked += ProcessType_CheckedChanged;
            if (rbImport != null) rbImport.Checked += ProcessType_CheckedChanged;
            
            // Apply the initial UI state
            ApplyModeUI();
        }

        private void ApplyModeUI()
        {
            // Guard against calls during initialization
            if (_services?.UIService == null || _services?.ExportDbObjectViewModel == null || _services?.ImportDbObjectViewModel == null)
                return;
                
            var rbExport = this.FindName("rbExport") as System.Windows.Controls.RadioButton;
            bool isExportMode = rbExport?.IsChecked == true;
            _services.UIService.ApplyModeUI(isExportMode, _services.ExportDbObjectViewModel, _services.ImportDbObjectViewModel);
        }
        
        private void ValidateSelectedDatabaseObjects(string viewName, string storedProcedureName)
        {
            // Guard against calls during initialization
            if (_services?.UIService == null)
                return;
                
            _services.UIService.ValidateSelectedDatabaseObjects(viewName, storedProcedureName);
        }
        
        private void ProcessType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ApplyModeUI();
        }
        
        private void ViewComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is DbObjectOption selectedView)
            {
                var rbExport = this.FindName("rbExport") as System.Windows.Controls.RadioButton;
                bool isExportMode = rbExport?.IsChecked == true;
                _services.UIService.HandleViewSelectionChanged(selectedView, isExportMode, _services.ExportDbObjectViewModel, _services.ImportDbObjectViewModel);
            }
        }
        
        private void StoredProcedureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is DbObjectOption selectedSP)
            {
                var rbExport = this.FindName("rbExport") as System.Windows.Controls.RadioButton;
                bool isExportMode = rbExport?.IsChecked == true;
                _services.UIService.HandleStoredProcedureSelectionChanged(selectedSP, isExportMode, _services.ExportDbObjectViewModel, _services.ImportDbObjectViewModel);
            }
        }



        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _services.UIActionService.HandleGenerateAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _services.MonitoringService.AddLog(MonitoringLogLevel.Error, $"Unexpected error in Generate button: {ex.Message}", "GenerateButton");
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _services.UIActionService.HandleCancel();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _services.UIActionService.HandleReset();
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
            _services.MenuService.HandleNewCommand();
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleOpenCommand();
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleSaveCommand();
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleSaveAsCommand();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleExitCommand(this);
        }

        // Edit Menu Handlers
        private void MenuUndo_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleUndoCommand();
        }

        private void MenuRedo_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleRedoCommand();
        }

        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleCutCommand();
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleCopyCommand();
        }

        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandlePasteCommand();
        }

        private void MenuSelectAll_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleSelectAllCommand();
        }

        private void MenuPreferences_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandlePreferencesCommand();
        }

        // View Menu Handlers
        private void ToggleSwitch_Click(object sender, MouseButtonEventArgs e)
        {
            // Guard against calls during XAML initialization before services are ready
            if (_services?.MenuService == null) return;
            
            _services.MenuService.HandleToggleSwitchClick();
        }

        private void MenuBasicView_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleBasicViewCommand();
        }

        private void MenuAdvancedView_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleAdvancedViewCommand();
        }

        private void MenuMonitoringPanel_Click(object sender, RoutedEventArgs e)
        {
            // Guard against calls during XAML initialization before services are ready
            if (_services?.MenuService == null) return;
            
            _services.MenuService.HandleMonitoringPanelCommand(sender);
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
            _services.MonitoringService.UpdateStatus(StatusType.Idle, "Application refreshed");
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
