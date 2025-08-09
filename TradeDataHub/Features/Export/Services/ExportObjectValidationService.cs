using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeDataHub.Core.Models;
using TradeDataHub.Features.Export;

namespace TradeDataHub.Features.Export.Services
{
    /// <summary>
    /// Service for validating export database objects
    /// </summary>
    public class ExportObjectValidationService
    {
        private readonly ExportSettings _exportSettings;

        public ExportObjectValidationService(ExportSettings exportSettings)
        {
            _exportSettings = exportSettings ?? throw new ArgumentNullException(nameof(exportSettings));
        }

        /// <summary>
        /// Validates that the specified view and stored procedure exist in the configuration
        /// </summary>
        /// <param name="viewName">The name of the view to validate</param>
        /// <param name="storedProcedureName">The name of the stored procedure to validate</param>
        /// <returns>True if both objects exist, false otherwise</returns>
        public bool ValidateObjects(string? viewName, string? storedProcedureName)
        {
            // If either parameter is null, validation fails
            if (string.IsNullOrEmpty(viewName) || string.IsNullOrEmpty(storedProcedureName))
            {
                return false;
            }
            
            // If ExportObjects is not configured, use the default Operation settings
            if (_exportSettings.ExportObjects == null)
            {
                return viewName == _exportSettings.Operation.ViewName &&
                       storedProcedureName == _exportSettings.Operation.StoredProcedureName;
            }

            // Check if the view exists in the configuration
            bool viewExists = _exportSettings.ExportObjects.Views.Any(v => v.Name == viewName);

            // Check if the stored procedure exists in the configuration
            bool spExists = _exportSettings.ExportObjects.StoredProcedures.Any(sp => sp.Name == storedProcedureName);

            return viewExists && spExists;
        }

        /// <summary>
        /// Gets the order by column for the specified view
        /// </summary>
        /// <param name="viewName">The name of the view</param>
        /// <returns>The order by column or null if not found</returns>
        public string GetOrderByColumn(string? viewName)
        {
            // If ExportObjects is not configured, use the default Operation settings
            if (_exportSettings.ExportObjects == null || !_exportSettings.ExportObjects.Views.Any())
            {
                return _exportSettings.Operation.OrderByColumn;
            }

            // Find the view in the configuration
            var view = _exportSettings.ExportObjects.Views.FirstOrDefault(v => v.Name == viewName);

            // Return the order by column or the default if not found
            return view?.OrderByColumn ?? _exportSettings.Operation.OrderByColumn;
        }

        /// <summary>
        /// Gets all available views from the configuration
        /// </summary>
        /// <returns>A list of database object options</returns>
        public List<DbObjectOption> GetAvailableViews()
        {
            // If ExportObjects is not configured, return a list with the default view
            if (_exportSettings.ExportObjects == null || !_exportSettings.ExportObjects.Views.Any())
            {
                return new List<DbObjectOption>
                {
                    new DbObjectOption(
                        _exportSettings.Operation.ViewName,
                        "Default Export View",
                        _exportSettings.Operation.OrderByColumn)
                };
            }

            return _exportSettings.ExportObjects.Views.ToList();
        }

        /// <summary>
        /// Gets all available stored procedures from the configuration
        /// </summary>
        /// <returns>A list of database object options</returns>
        public List<DbObjectOption> GetAvailableStoredProcedures()
        {
            // If ExportObjects is not configured, return a list with the default stored procedure
            if (_exportSettings.ExportObjects == null || !_exportSettings.ExportObjects.StoredProcedures.Any())
            {
                return new List<DbObjectOption>
                {
                    new DbObjectOption(
                        _exportSettings.Operation.StoredProcedureName,
                        "Default Export Process")
                };
            }

            return _exportSettings.ExportObjects.StoredProcedures.ToList();
        }

        /// <summary>
        /// Gets the default view name from the configuration
        /// </summary>
        /// <returns>The default view name</returns>
        public string GetDefaultViewName()
        {
            return _exportSettings.ExportObjects?.DefaultViewName ?? _exportSettings.Operation.ViewName;
        }

        /// <summary>
        /// Gets the default stored procedure name from the configuration
        /// </summary>
        /// <returns>The default stored procedure name</returns>
        public string GetDefaultStoredProcedureName()
        {
            return _exportSettings.ExportObjects?.DefaultStoredProcedureName ?? _exportSettings.Operation.StoredProcedureName;
        }
    }
}