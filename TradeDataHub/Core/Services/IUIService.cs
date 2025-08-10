using System.Windows.Controls;
using TradeDataHub.Features.Common.ViewModels;
using TradeDataHub.Core.Models;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Interface for UI management services
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// Apply UI mode based on export/import selection
        /// </summary>
        void ApplyModeUI(bool isExportMode, 
            DbObjectSelectorViewModel? exportViewModel, 
            DbObjectSelectorViewModel? importViewModel);
            
        /// <summary>
        /// Handle view selection change
        /// </summary>
        void HandleViewSelectionChanged(DbObjectOption selectedView, bool isExportMode,
            DbObjectSelectorViewModel? exportViewModel,
            DbObjectSelectorViewModel? importViewModel);
            
        /// <summary>
        /// Handle stored procedure selection change
        /// </summary>
        void HandleStoredProcedureSelectionChanged(DbObjectOption selectedStoredProcedure, bool isExportMode,
            DbObjectSelectorViewModel? exportViewModel,
            DbObjectSelectorViewModel? importViewModel);
            
        /// <summary>
        /// Validate selected database objects and show warnings if needed
        /// </summary>
        void ValidateSelectedDatabaseObjects(string? viewName, string? storedProcedureName);
        
        /// <summary>
        /// Initialize UI controls for the service
        /// </summary>
        void Initialize(
            TextBlock lblExporter, TextBox txtExporter,
            TextBlock lblImporter, TextBox txtImporter,
            ComboBox viewComboBox, ComboBox storedProcedureComboBox);
    }
}
