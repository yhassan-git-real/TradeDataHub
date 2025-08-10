using System.Collections.Generic;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Core.Models;

namespace TradeDataHub.Core.Validation
{
    /// <summary>
    /// Service for validating parameters
    /// </summary>
    public class ParameterValidator : IParameterValidator
    {
        /// <summary>
        /// Validates export input parameters
        /// </summary>
        /// <param name="exportInputs">The export input parameters to validate</param>
        /// <returns>Validation result with success/failure and error messages</returns>
        public ExportParameterHelper.ValidationResult ValidateExport(ExportInputs exportInputs)
        {
            // Null checks
            if (exportInputs == null)
            {
                return new ExportParameterHelper.ValidationResult 
                { 
                    IsValid = false, 
                    Errors = new List<string> { "Export inputs cannot be null" } 
                };
            }

            // Basic month validation
            if (string.IsNullOrWhiteSpace(exportInputs.FromMonth) || string.IsNullOrWhiteSpace(exportInputs.ToMonth))
            {
                return new ExportParameterHelper.ValidationResult 
                { 
                    IsValid = false, 
                    Errors = new List<string> { "From Month and To Month are required." } 
                };
            }

            // Use existing ExportParameterHelper for comprehensive validation
            var w = ExportParameterHelper.WILDCARD;
            return ExportParameterHelper.ValidateExportParameters(
                exportInputs.FromMonth, 
                exportInputs.ToMonth, 
                w, w, w, w, w, w, w
            );
        }

        /// <summary>
        /// Validates import input parameters
        /// </summary>
        /// <param name="importInputs">The import input parameters to validate</param>
        /// <returns>Validation result with success/failure and error messages</returns>
        public ImportParameterHelper.ValidationResult ValidateImport(ImportInputs importInputs)
        {
            // Null checks
            if (importInputs == null)
            {
                return new ImportParameterHelper.ValidationResult 
                { 
                    IsValid = false, 
                    Errors = new List<string> { "Import inputs cannot be null" } 
                };
            }

            // Basic month validation
            if (string.IsNullOrWhiteSpace(importInputs.FromMonth) || string.IsNullOrWhiteSpace(importInputs.ToMonth))
            {
                return new ImportParameterHelper.ValidationResult 
                { 
                    IsValid = false, 
                    Errors = new List<string> { "From Month and To Month are required." } 
                };
            }

            // Use existing ImportParameterHelper for comprehensive validation
            var w = ImportParameterHelper.WILDCARD;
            return ImportParameterHelper.ValidateImportParameters(
                importInputs.FromMonth, 
                importInputs.ToMonth, 
                w, w, w, w, w, w, w
            );
        }
    }
}
