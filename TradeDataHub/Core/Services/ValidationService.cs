using System;
using System.Linq;
using TradeDataHub.Core.Models;
using TradeDataHub.Core.Validation;
using TradeDataHub.Features.Export.Services;
using TradeDataHub.Features.Import.Services;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for handling validation operations across the application
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly IParameterValidator _parameterValidator;
        private readonly ExportObjectValidationService _exportObjectValidationService;
        private readonly ImportObjectValidationService _importObjectValidationService;

        public ValidationService(
            IParameterValidator parameterValidator,
            ExportObjectValidationService exportObjectValidationService,
            ImportObjectValidationService importObjectValidationService)
        {
            _parameterValidator = parameterValidator ?? throw new ArgumentNullException(nameof(parameterValidator));
            _exportObjectValidationService = exportObjectValidationService ?? throw new ArgumentNullException(nameof(exportObjectValidationService));
            _importObjectValidationService = importObjectValidationService ?? throw new ArgumentNullException(nameof(importObjectValidationService));
        }

        public ValidationResult ValidateExportOperation(ExportInputs exportInputs, string selectedView, string selectedStoredProcedure)
        {
            if (exportInputs == null)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Export inputs cannot be null.",
                    Title = "Invalid Input",
                    Errors = new[] { "Export inputs cannot be null." }
                };
            }

            // Validate selected database objects
            if (!_exportObjectValidationService.ValidateObjects(selectedView, selectedStoredProcedure))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "The selected View or Stored Procedure is not valid. Please select valid database objects.",
                    Title = "Invalid Database Objects",
                    Errors = new[] { "Invalid database objects selected." }
                };
            }

            // Centralized validation (months + format)
            var parameterValidation = _parameterValidator.ValidateExport(exportInputs);
            
            if (!parameterValidation.IsValid)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Parameter Validation Failed:\n" + string.Join("\n", parameterValidation.Errors),
                    Title = "Invalid Parameters",
                    Errors = parameterValidation.Errors.ToArray()
                };
            }

            return new ValidationResult { IsValid = true };
        }

        public ValidationResult ValidateImportOperation(ImportInputs importInputs, string selectedView, string selectedStoredProcedure)
        {
            if (importInputs == null)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Import inputs cannot be null.",
                    Title = "Invalid Input",
                    Errors = new[] { "Import inputs cannot be null." }
                };
            }

            // Validate selected database objects
            if (!_importObjectValidationService.ValidateObjects(selectedView, selectedStoredProcedure))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "The selected View or Stored Procedure is not valid. Please select valid database objects.",
                    Title = "Invalid Database Objects",
                    Errors = new[] { "Invalid database objects selected." }
                };
            }

            // Centralized validation (months + format)
            var parameterValidation = _parameterValidator.ValidateImport(importInputs);
            
            if (!parameterValidation.IsValid)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Parameter Validation Failed:\n" + string.Join("\n", parameterValidation.Errors),
                    Title = "Invalid Parameters",
                    Errors = parameterValidation.Errors.ToArray()
                };
            }

            return new ValidationResult { IsValid = true };
        }
    }
}
