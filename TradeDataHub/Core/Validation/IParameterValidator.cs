using TradeDataHub.Core.Helpers;
using TradeDataHub.Core.Models;

namespace TradeDataHub.Core.Validation
{
    /// <summary>
    /// Interface for parameter validation operations
    /// </summary>
    public interface IParameterValidator
    {
        /// <summary>
        /// Validates export input parameters
        /// </summary>
        /// <param name="exportInputs">The export input parameters to validate</param>
        /// <returns>Validation result with success/failure and error messages</returns>
        ExportParameterHelper.ValidationResult ValidateExport(ExportInputs exportInputs);

        /// <summary>
        /// Validates import input parameters
        /// </summary>
        /// <param name="importInputs">The import input parameters to validate</param>
        /// <returns>Validation result with success/failure and error messages</returns>
        ImportParameterHelper.ValidationResult ValidateImport(ImportInputs importInputs);
    }
}
