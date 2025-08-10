using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using TradeDataHub.Core.Database;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Core.Services;

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
            InitializeUIControls();
            InitializeServices();
            InitializeEventHandlers();
            ApplyInitialUIState();
        }

        /// <summary>
        /// Initialize UI controls and set their initial visibility states
        /// </summary>
        private void InitializeUIControls()
        {
            // Initialize UI controls after they're loaded
            var advancedParametersGrid = this.FindName("AdvancedParametersGrid") as System.Windows.Controls.Grid;
            if (advancedParametersGrid != null)
            {
                advancedParametersGrid.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Initialize services with UI control references
        /// </summary>
        private void InitializeServices()
        {
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
        }

        /// <summary>
        /// Wire up event handlers for UI controls
        /// </summary>
        private void InitializeEventHandlers()
        {
            // Wire up the RadioButton event handlers after initialization
            var rbExport = this.FindName("rbExport") as System.Windows.Controls.RadioButton;
            var rbImport = this.FindName("rbImport") as System.Windows.Controls.RadioButton;
            if (rbExport != null) rbExport.Checked += ProcessType_CheckedChanged;
            if (rbImport != null) rbImport.Checked += ProcessType_CheckedChanged;
        }

        /// <summary>
        /// Apply initial UI state based on current mode
        /// </summary>
        private void ApplyInitialUIState()
        {
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
        






        #region Public Methods for Services

        /// <summary>
        /// Public method to handle reset functionality for menu service
        /// </summary>
        public void HandleResetFields()
        {
            ResetButton_Click(this, new RoutedEventArgs());
        }

        #endregion

        #region Menu Event Handlers - Moved to MainWindow.MenuHandlers.cs

        #endregion

        #region Keyboard Event Handlers - Moved to MainWindow.KeyboardHandlers.cs

        #endregion

        #endregion
    }
}
