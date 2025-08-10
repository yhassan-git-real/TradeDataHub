using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;
using TradeDataHub.Controls;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for menu operations
    /// </summary>
    public class MenuService : IMenuService
    {
        private readonly MonitoringService _monitoringService;
        private Window? _mainWindow;
        
        // UI Controls that need to be accessed for menu operations
        private YearMonthPicker? _fromMonthPicker;
        private YearMonthPicker? _toMonthPicker;
        private TextBox? _txtHS;
        private TextBox? _txtPort;
        private TextBox? _txtProduct;
        private TextBox? _txtExporter;
        private TextBox? _txtImporter;
        private TextBox? _txtForCount;
        private TextBox? _txtForName;
        private TextBox? _txtIEC;
        private RadioButton? _rbExport;
        private RadioButton? _rbImport;
        private MenuItem? _menuAdvancedView;
        private Grid? _advancedParametersGrid;
        private MenuItem? _menuBasicView;
        private MenuItem? _menuMonitoringPanel;
        private Grid? _monitoringPanel;
        private Border? _activeIndicator;
        private TextBlock? _basicText;
        private System.Windows.Shapes.Path? _basicIcon;
        private TextBlock? _allText;
        private System.Windows.Shapes.Path? _allIcon;

        public MenuService()
        {
            _monitoringService = MonitoringService.Instance;
        }

        public void Initialize(Window window)
        {
            _mainWindow = window;
            
            // Get references to UI controls using FindName
            _fromMonthPicker = window.FindName("FromMonthPicker") as YearMonthPicker;
            _toMonthPicker = window.FindName("ToMonthPicker") as YearMonthPicker;
            _txtHS = window.FindName("Txt_HS") as TextBox;
            _txtPort = window.FindName("txt_Port") as TextBox;
            _txtProduct = window.FindName("Txt_Product") as TextBox;
            _txtExporter = window.FindName("Txt_Exporter") as TextBox;
            _txtImporter = window.FindName("Txt_Importer") as TextBox;
            _txtForCount = window.FindName("txt_ForCount") as TextBox;
            _txtForName = window.FindName("Txt_ForName") as TextBox;
            _txtIEC = window.FindName("Txt_IEC") as TextBox;
            _rbExport = window.FindName("rbExport") as RadioButton;
            _rbImport = window.FindName("rbImport") as RadioButton;
            _menuAdvancedView = window.FindName("MenuAdvancedView") as MenuItem;
            _menuBasicView = window.FindName("MenuBasicView") as MenuItem;
            _menuMonitoringPanel = window.FindName("MenuMonitoringPanel") as MenuItem;
            _monitoringPanel = window.FindName("MonitoringPanel") as Grid;
            _advancedParametersGrid = window.FindName("AdvancedParametersGrid") as Grid;
            _activeIndicator = window.FindName("ActiveIndicator") as Border;
            _basicText = window.FindName("BasicText") as TextBlock;
            _basicIcon = window.FindName("BasicIcon") as System.Windows.Shapes.Path;
            _allText = window.FindName("AllText") as TextBlock;
            _allIcon = window.FindName("AllIcon") as System.Windows.Shapes.Path;
        }

        #region File Menu Operations

        public void HandleNewCommand()
        {
            // Reset all fields to create a "new" session - delegate to reset functionality
            if (_mainWindow is MainWindow mainWindow)
            {
                mainWindow.HandleResetFields();
            }
        }

        public void HandleOpenCommand()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Open Configuration File",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config")
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

        public void HandleSaveCommand()
        {
            try
            {
                // Get current parameters
                var config = new
                {
                    FromMonth = _fromMonthPicker?.SelectedYearMonth ?? "",
                    ToMonth = _toMonthPicker?.SelectedYearMonth ?? "",
                    HSCodes = _txtHS?.Text ?? "",
                    PortCodes = _txtPort?.Text ?? "",
                    Products = _txtProduct?.Text ?? "",
                    Exporters = _txtExporter?.Text ?? "",
                    Importers = _txtImporter?.Text ?? "",
                    ForeignCountries = _txtForCount?.Text ?? "",
                    ForeignCompanies = _txtForName?.Text ?? "",
                    IECCodes = _txtIEC?.Text ?? "",
                    Mode = _rbExport?.IsChecked == true ? "Export" : "Import",
                    View = _menuAdvancedView?.IsChecked == true ? "Advanced" : "Basic"
                };

                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "user_settings.json");
                
                File.WriteAllText(configPath, json);
                MessageBox.Show($"Current settings saved to: {configPath}", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void HandleSaveAsCommand()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Configuration As",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config"),
                    FileName = $"TradeDataHub_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var config = new
                    {
                        FromMonth = _fromMonthPicker?.SelectedYearMonth ?? "",
                        ToMonth = _toMonthPicker?.SelectedYearMonth ?? "",
                        HSCodes = _txtHS?.Text ?? "",
                        PortCodes = _txtPort?.Text ?? "",
                        Products = _txtProduct?.Text ?? "",
                        Exporters = _txtExporter?.Text ?? "",
                        Importers = _txtImporter?.Text ?? "",
                        ForeignCountries = _txtForCount?.Text ?? "",
                        ForeignCompanies = _txtForName?.Text ?? "",
                        IECCodes = _txtIEC?.Text ?? "",
                        Mode = _rbExport?.IsChecked == true ? "Export" : "Import",
                        View = _menuAdvancedView?.IsChecked == true ? "Advanced" : "Basic"
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show($"Configuration saved to: {saveFileDialog.FileName}", "Configuration Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void HandleExitCommand(Window window)
        {
            // Check if there's an ongoing operation (this would need to be passed from MainWindow)
            var result = MessageBox.Show("Are you sure you want to exit?", 
                                       "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                window.Close();
            }
        }

        #endregion

        #region Edit Menu Operations

        public void HandleUndoCommand()
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

        public void HandleRedoCommand()
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

        public void HandleCutCommand()
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

        public void HandleCopyCommand()
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

        public void HandlePasteCommand()
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

        public void HandleSelectAllCommand()
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

        public void HandlePreferencesCommand()
        {
            var prefMessage = "Preferences:\n\n" +
                            "• File locations:\n" +
                            "  - Config: " + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config") + "\n" +
                            "  - Logs: " + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs") + "\n" +
                            "  - Export: " + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EXPORT_Excel") + "\n" +
                            "  - Import: " + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IMPORT_Excel") + "\n\n" +
                            "• Database connection settings are in Config/database.appsettings.json\n" +
                            "• Excel formatting settings are in Config/excelFormatting.json\n\n" +
                            "Advanced preferences dialog will be available in future updates.";
            
            MessageBox.Show(prefMessage, "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region View Menu Operations

        public void HandleBasicViewCommand()
        {
            SwitchToBasicMode();
        }

        public void HandleAdvancedViewCommand()
        {
            SwitchToAllMode();
        }

        public void HandleToggleSwitchClick()
        {
            // Check current state based on AdvancedParametersGrid visibility
            if (_advancedParametersGrid == null) return;
            
            bool isBasicMode = _advancedParametersGrid.Visibility == Visibility.Collapsed;
            
            if (isBasicMode)
            {
                SwitchToAllMode();
            }
            else
            {
                SwitchToBasicMode();
            }
        }

        public void HandleMonitoringPanelCommand(object sender)
        {
            if (_monitoringPanel == null)
            {
                // Defer until layout pass if not ready
                _mainWindow?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyMonitoringPanelVisibility(sender);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            ApplyMonitoringPanelVisibility(sender);
        }

        private void ApplyMonitoringPanelVisibility(object sender)
        {
            if (_monitoringPanel == null || _menuMonitoringPanel == null) return;

            bool isChecked = false;

            // Prefer the sender if it's a ToggleButton / MenuItem
            switch (sender)
            {
                case System.Windows.Controls.Primitives.ToggleButton tb:
                    isChecked = tb.IsChecked == true;
                    break;
                case MenuItem mi:
                    isChecked = mi.IsChecked;
                    break;
                default:
                    isChecked = _menuMonitoringPanel.IsChecked; // fallback
                    break;
            }

            _monitoringPanel.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;

            // Keep the MenuItem and ToggleButton in sync
            if (_menuMonitoringPanel.IsChecked != isChecked)
                _menuMonitoringPanel.IsChecked = isChecked;
        }

        public void HandleActivityLogCommand()
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

        public void HandleRefreshCommand()
        {
            // Refresh monitoring status
            _monitoringService.UpdateStatus(StatusType.Idle, "Application refreshed");
            MessageBox.Show("Application view refreshed.", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void HandleFullScreenCommand(Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                window.ResizeMode = ResizeMode.CanResize;
            }
            else
            {
                window.WindowState = WindowState.Maximized;
                window.WindowStyle = WindowStyle.None;
                window.ResizeMode = ResizeMode.NoResize;
            }
        }

        #endregion

        #region Help Menu Operations

        public void HandleUserManualCommand()
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

        public void HandleQuickStartCommand()
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

        public void HandleKeyboardShortcutsCommand()
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

        public void HandleOnlineHelpCommand()
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

        public void HandleCheckUpdatesCommand()
        {
            try
            {
                var updateInfo = "Trade Data Hub Update Check\n\n" +
                               $"Current Version: 1.0.0\n" +
                               $"Build Date: {File.GetCreationTime(System.Reflection.Assembly.GetExecutingAssembly().Location):yyyy-MM-dd}\n" +
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

        public void HandleAboutCommand()
        {
            try
            {
                var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var buildDate = File.GetCreationTime(System.Reflection.Assembly.GetExecutingAssembly().Location);
                
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

        #endregion

        #region Helper Methods

        private void CopyApplicationSettingsToClipboard()
        {
            try
            {
                var settings = "Trade Data Hub - Current Settings\n" +
                              "================================\n\n" +
                              $"From Month: {_fromMonthPicker?.SelectedYearMonth ?? ""}\n" +
                              $"To Month: {_toMonthPicker?.SelectedYearMonth ?? ""}\n" +
                              $"HS Codes: {_txtHS?.Text ?? ""}\n" +
                              $"Port Codes: {_txtPort?.Text ?? ""}\n" +
                              $"Products: {_txtProduct?.Text ?? ""}\n" +
                              $"Exporters: {_txtExporter?.Text ?? ""}\n" +
                              $"Importers: {_txtImporter?.Text ?? ""}\n" +
                              $"Foreign Countries: {_txtForCount?.Text ?? ""}\n" +
                              $"Foreign Companies: {_txtForName?.Text ?? ""}\n" +
                              $"IEC Codes: {_txtIEC?.Text ?? ""}\n" +
                              $"Mode: {(_rbExport?.IsChecked == true ? "Export" : "Import")}\n" +
                              $"View: {(_menuAdvancedView?.IsChecked == true ? "Advanced" : "Basic")}\n" +
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

        private void SwitchToAllMode()
        {
            // Show all parameters
            if (_advancedParametersGrid != null)
                _advancedParametersGrid.Visibility = Visibility.Visible;
            
            // Move active indicator to right (All mode)
            if (_activeIndicator != null)
                _activeIndicator.SetValue(Grid.ColumnProperty, 1);
            
            // Update colors for All mode
            if (_basicText != null)
                _basicText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125)); // #6C757D
            if (_basicIcon != null)
                _basicIcon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125));
            if (_allText != null)
                _allText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            if (_allIcon != null)
                _allIcon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            
            // Update menu checkboxes
            if (_menuBasicView != null) _menuBasicView.IsChecked = false;
            if (_menuAdvancedView != null) _menuAdvancedView.IsChecked = true;
        }

        private void SwitchToBasicMode()
        {
            // Hide additional parameters
            if (_advancedParametersGrid != null)
                _advancedParametersGrid.Visibility = Visibility.Collapsed;
            
            // Move active indicator to left (Basic mode)
            if (_activeIndicator != null)
                _activeIndicator.SetValue(Grid.ColumnProperty, 0);
            
            // Update colors for Basic mode
            if (_basicText != null)
                _basicText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            if (_basicIcon != null)
                _basicIcon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            if (_allText != null)
                _allText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125)); // #6C757D
            if (_allIcon != null)
                _allIcon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125));
            
            // Update menu checkboxes
            if (_menuBasicView != null) _menuBasicView.IsChecked = true;
            if (_menuAdvancedView != null) _menuAdvancedView.IsChecked = false;
        }

        #endregion
    }
}
