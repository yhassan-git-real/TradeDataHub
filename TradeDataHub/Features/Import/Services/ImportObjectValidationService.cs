using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeDataHub.Core.Models;
using TradeDataHub.Features.Import;

namespace TradeDataHub.Features.Import.Services
{
    /// <summary>
    /// Service for validating import database objects
    /// </summary>
    public class ImportObjectValidationService
    {
        private readonly ImportSettings _importSettings;

        public ImportObjectValidationService(ImportSettings importSettings)
        {
            _importSettings = importSettings ?? throw new ArgumentNullException(nameof(importSettings));
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
            
            // If ImportObjects is not configured, use the default Database settings
            if (_importSettings.ImportObjects == null)
            {
                return viewName == _importSettings.Database.ViewName &&
                       storedProcedureName == _importSettings.Database.StoredProcedureName;
            }

            // Check if the view exists in the configuration
            bool viewExists = _importSettings.ImportObjects.Views.Any(v => v.Name == viewName);

            // Check if the stored procedure exists in the configuration
            bool spExists = _importSettings.ImportObjects.StoredProcedures.Any(sp => sp.Name == storedProcedureName);

            return viewExists && spExists;
        }

        /// <summary>
        /// Gets the order by column for the specified view
        /// </summary>
        /// <param name="viewName">The name of the view</param>
        /// <returns>The order by column or null if not found</returns>
        public string GetOrderByColumn(string? viewName)
        {
            // If ImportObjects is not configured, use the default Database settings
            if (_importSettings.ImportObjects == null || !_importSettings.ImportObjects.Views.Any())
            {
                return _importSettings.Database.OrderByColumn;
            }

            // Find the view in the configuration
            var view = _importSettings.ImportObjects.Views.FirstOrDefault(v => v.Name == viewName);

            // Return the order by column or the default if not found
            return view?.OrderByColumn ?? _importSettings.Database.OrderByColumn;
        }

        /// <summary>
        /// Gets all available views from the configuration
        /// </summary>
        /// <returns>A list of database object options</returns>
        public List<DbObjectOption> GetAvailableViews()
        {
            // If ImportObjects is not configured, return a list with the default view
            if (_importSettings.ImportObjects == null || !_importSettings.ImportObjects.Views.Any())
            {
                return new List<DbObjectOption>
                {
                    new DbObjectOption(
                        _importSettings.Database.ViewName,
                        "Default Import View",
                        _importSettings.Database.OrderByColumn)
                };
            }

            return _importSettings.ImportObjects.Views.ToList();
        }

        /// <summary>
        /// Gets all available stored procedures from the configuration
        /// </summary>
        /// <returns>A list of database object options</returns>
        public List<DbObjectOption> GetAvailableStoredProcedures()
        {
            // If ImportObjects is not configured, return a list with the default stored procedure
            if (_importSettings.ImportObjects == null || !_importSettings.ImportObjects.StoredProcedures.Any())
            {
                return new List<DbObjectOption>
                {
                    new DbObjectOption(
                        _importSettings.Database.StoredProcedureName,
                        "Default Import Process")
                };
            }

            return _importSettings.ImportObjects.StoredProcedures.ToList();
        }

        /// <summary>
        /// Gets the default view name from the configuration
        /// </summary>
        /// <returns>The default view name</returns>
        public string GetDefaultViewName()
        {
            return _importSettings.ImportObjects?.DefaultViewName ?? _importSettings.Database.ViewName;
        }

        /// <summary>
        /// Gets the default stored procedure name from the configuration
        /// </summary>
        /// <returns>The default stored procedure name</returns>
        public string GetDefaultStoredProcedureName()
        {
            return _importSettings.ImportObjects?.DefaultStoredProcedureName ?? _importSettings.Database.StoredProcedureName;
        }
    }
}