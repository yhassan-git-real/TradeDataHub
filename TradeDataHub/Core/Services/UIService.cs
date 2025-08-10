using System.Windows;
using System.Windows.Controls;
using TradeDataHub.Features.Common.ViewModels;
using TradeDataHub.Core.Models;
using TradeDataHub.Core.Database;
using TradeDataHub.Features.Monitoring.Services;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for managing UI operations and state
    /// </summary>
    public class UIService : IUIService
    {
        private readonly DatabaseObjectValidator _databaseObjectValidator;
        private readonly MonitoringService _monitoringService;
        
        // UI Controls - set via Initialize method
        private TextBlock? _lblExporter;
        private TextBox? _txtExporter;
        private TextBlock? _lblImporter;
        private TextBox? _txtImporter;
        private ComboBox? _viewComboBox;
        private ComboBox? _storedProcedureComboBox;

        public UIService(
            DatabaseObjectValidator databaseObjectValidator,
            MonitoringService monitoringService)
        {
            _databaseObjectValidator = databaseObjectValidator;
            _monitoringService = monitoringService;
        }
        
        public void Initialize(
            TextBlock lblExporter, TextBox txtExporter,
            TextBlock lblImporter, TextBox txtImporter,
            ComboBox viewComboBox, ComboBox storedProcedureComboBox)
        {
            _lblExporter = lblExporter;
            _txtExporter = txtExporter;
            _lblImporter = lblImporter;
            _txtImporter = txtImporter;
            _viewComboBox = viewComboBox;
            _storedProcedureComboBox = storedProcedureComboBox;
        }

        public void ApplyModeUI(bool isExportMode, 
            DbObjectSelectorViewModel? exportViewModel, 
            DbObjectSelectorViewModel? importViewModel)
        {
            if (_lblExporter == null || _txtExporter == null || 
                _lblImporter == null || _txtImporter == null ||
                _viewComboBox == null || _storedProcedureComboBox == null)
            {
                return; // Controls not initialized
            }
            
            if (isExportMode)
            {
                _lblExporter.Visibility = Visibility.Visible;
                _txtExporter.Visibility = Visibility.Visible;
                _lblImporter.Visibility = Visibility.Collapsed;
                _txtImporter.Visibility = Visibility.Collapsed;
                
                // Set export database objects with null checks
                if (exportViewModel != null)
                {
                    _viewComboBox.ItemsSource = exportViewModel.Views;
                    _viewComboBox.SelectedItem = exportViewModel.SelectedView;
                    _storedProcedureComboBox.ItemsSource = exportViewModel.StoredProcedures;
                    _storedProcedureComboBox.SelectedItem = exportViewModel.SelectedStoredProcedure;
                    
                    // Validate selected database objects
                    ValidateSelectedDatabaseObjects(exportViewModel.SelectedView?.Name, 
                                                   exportViewModel.SelectedStoredProcedure?.Name);
                }
            }
            else
            {
                _lblExporter.Visibility = Visibility.Collapsed;
                _txtExporter.Visibility = Visibility.Collapsed;
                _lblImporter.Visibility = Visibility.Visible;
                _txtImporter.Visibility = Visibility.Visible;
                
                // Set import database objects with null checks
                if (importViewModel != null)
                {
                    _viewComboBox.ItemsSource = importViewModel.Views;
                    _viewComboBox.SelectedItem = importViewModel.SelectedView;
                    _storedProcedureComboBox.ItemsSource = importViewModel.StoredProcedures;
                    _storedProcedureComboBox.SelectedItem = importViewModel.SelectedStoredProcedure;
                    
                    // Validate selected database objects
                    ValidateSelectedDatabaseObjects(importViewModel.SelectedView?.Name, 
                                                   importViewModel.SelectedStoredProcedure?.Name);
                }
            }
        }
        
        public void HandleViewSelectionChanged(DbObjectOption selectedView, bool isExportMode,
            DbObjectSelectorViewModel? exportViewModel,
            DbObjectSelectorViewModel? importViewModel)
        {
            if (isExportMode && exportViewModel != null)
            {
                exportViewModel.SelectedView = selectedView;
                
                // Validate if the view exists in the database
                if (!_databaseObjectValidator.ViewExists(selectedView.Name))
                {
                    MessageBox.Show($"The selected view '{selectedView.Name}' does not exist in the database.", 
                        "Database Object Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _monitoringService.SetWarning($"View '{selectedView.Name}' not found in database");
                }
            }
            else if (!isExportMode && importViewModel != null)
            {
                importViewModel.SelectedView = selectedView;
                
                // Validate if the view exists in the database
                if (!_databaseObjectValidator.ViewExists(selectedView.Name))
                {
                    MessageBox.Show($"The selected view '{selectedView.Name}' does not exist in the database.", 
                        "Database Object Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _monitoringService.SetWarning($"View '{selectedView.Name}' not found in database");
                }
            }
        }
        
        public void HandleStoredProcedureSelectionChanged(DbObjectOption selectedStoredProcedure, bool isExportMode,
            DbObjectSelectorViewModel? exportViewModel,
            DbObjectSelectorViewModel? importViewModel)
        {
            if (isExportMode && exportViewModel != null)
            {
                exportViewModel.SelectedStoredProcedure = selectedStoredProcedure;
                
                // Validate if the stored procedure exists in the database
                if (!_databaseObjectValidator.StoredProcedureExists(selectedStoredProcedure.Name))
                {
                    MessageBox.Show($"The selected stored procedure '{selectedStoredProcedure.Name}' does not exist in the database.", 
                        "Database Object Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _monitoringService.SetWarning($"Stored procedure '{selectedStoredProcedure.Name}' not found in database");
                }
            }
            else if (!isExportMode && importViewModel != null)
            {
                importViewModel.SelectedStoredProcedure = selectedStoredProcedure;
                
                // Validate if the stored procedure exists in the database
                if (!_databaseObjectValidator.StoredProcedureExists(selectedStoredProcedure.Name))
                {
                    MessageBox.Show($"The selected stored procedure '{selectedStoredProcedure.Name}' does not exist in the database.", 
                        "Database Object Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _monitoringService.SetWarning($"Stored procedure '{selectedStoredProcedure.Name}' not found in database");
                }
            }
        }

        public void ValidateSelectedDatabaseObjects(string? viewName, string? storedProcedureName)
        {
            if (string.IsNullOrEmpty(viewName) || string.IsNullOrEmpty(storedProcedureName))
                return;
                
            var (viewExists, spExists) = _databaseObjectValidator.ValidateDatabaseObjects(viewName, storedProcedureName);
            
            if (!viewExists)
            {
                MessageBox.Show($"The selected view '{viewName}' does not exist in the database.", 
                    "Database Object Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                _monitoringService.SetWarning($"View '{viewName}' not found in database");
            }
            
            if (!spExists)
            {
                MessageBox.Show($"The selected stored procedure '{storedProcedureName}' does not exist in the database.", 
                    "Database Object Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                _monitoringService.SetWarning($"Stored procedure '{storedProcedureName}' not found in database");
            }
        }
    }
}
