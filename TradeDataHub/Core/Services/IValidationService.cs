using TradeDataHub.Core.Models;
using TradeDataHub.Core.Validation;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Interface for validation service operations
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Validates export inputs and database objects
        /// </summary>
        ValidationResult ValidateExportOperation(ExportInputs exportInputs, string selectedView, string selectedStoredProcedure);
        
        /// <summary>
        /// Validates import inputs and database objects
        /// </summary>
        ValidationResult ValidateImportOperation(ImportInputs importInputs, string selectedView, string selectedStoredProcedure);
    }

    /// <summary>
    /// Result of validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Title { get; set; } = "Validation Error";
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}
