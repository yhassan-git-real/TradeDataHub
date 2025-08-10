using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TradeDataHub.Core.Models;
using TradeDataHub.Core.Validation;
using TradeDataHub.Features.Import;
using TradeDataHub.Features.Import.Services;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;
using TradeDataHub.Core.Logging;
using MonitoringLogLevel = TradeDataHub.Features.Monitoring.Models.LogLevel;

namespace TradeDataHub.Core.Controllers
{
    /// <summary>
    /// Controller for import operations
    /// </summary>
    public class ImportController : IImportController
    {
        private readonly ImportExcelService _importExcelService;
        private readonly IParameterValidator _parameterValidator;
        private readonly MonitoringService _monitoringService;
        private readonly ImportObjectValidationService _importObjectValidationService;
        private readonly Dispatcher _dispatcher;

        public ImportController(
            ImportExcelService importExcelService,
            IParameterValidator parameterValidator,
            MonitoringService monitoringService,
            ImportObjectValidationService importObjectValidationService,
            Dispatcher dispatcher)
        {
            _importExcelService = importExcelService;
            _parameterValidator = parameterValidator;
            _monitoringService = monitoringService;
            _importObjectValidationService = importObjectValidationService;
            _dispatcher = dispatcher;
        }

        public async Task RunAsync(ImportInputs importInputs, CancellationToken cancellationToken, string selectedView, string selectedStoredProcedure)
        {
            var fromMonth = importInputs.FromMonth;
            var toMonth = importInputs.ToMonth;

            // Validate selected database objects
            if (!_importObjectValidationService.ValidateObjects(selectedView, selectedStoredProcedure))
            {
                MessageBox.Show("The selected View or Stored Procedure is not valid. Please select valid database objects.", 
                    "Invalid Database Objects", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Early guard for empty months (faster feedback before full validation call)
            if (string.IsNullOrWhiteSpace(fromMonth) || string.IsNullOrWhiteSpace(toMonth))
            {
                MessageBox.Show("From Month and To Month are required.", "Missing Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Centralized validation (months + format)
            var validation = _parameterValidator.ValidateImport(importInputs);
            
            if (!validation.IsValid)
            {
                string errorMessage = "Parameter Validation Failed:\n" + string.Join("\n", validation.Errors);
                MessageBox.Show(errorMessage, "Invalid Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse lists (Txt_Exporter textbox is repurposed as Importer list when Import mode selected)
            var ports = importInputs.Ports;
            var hsCodes = importInputs.HSCodes;
            var products = importInputs.Products;
            var importers = importInputs.Importers; // importer names
            var foreignCountries = importInputs.ForeignCountries;
            var foreignNames = importInputs.ForeignNames;
            var iecs = importInputs.IECs;

            int filesGenerated = 0;
            int combinationsProcessed = 0;
            int combinationsSkipped = 0;
            int skippedNoData = 0;
            int skippedRowLimit = 0;
            int cancelledCombinations = 0;

            await Task.Run(() =>
            {
                foreach (var port in ports)
                {
                    foreach (var hsCode in hsCodes)
                    {
                        foreach (var product in products)
                        {
                            foreach (var importer in importers)
                            {
                                foreach (var iec in iecs)
                                {
                                    foreach (var country in foreignCountries)
                                    {
                                        foreach (var name in foreignNames)
                                        {
                                            // Check for cancellation at the start of each combination
                                            if (cancellationToken.IsCancellationRequested)
                                            {
                                                cancellationToken.ThrowIfCancellationRequested();
                                            }

                                            combinationsProcessed++;
                                            var comboNumber = combinationsProcessed; // capture for closure
                                            _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Running, $"Processing (Import) combination {comboNumber}...", "Import"));

                                            try
                                            {
                                                var result = _importExcelService.CreateReport(
                                                    fromMonth, 
                                                    toMonth, 
                                                    hsCode, 
                                                    product, 
                                                    iec, 
                                                    importer, 
                                                    country, 
                                                    name, 
                                                    port, 
                                                    cancellationToken,
                                                    selectedView,
                                                    selectedStoredProcedure);
                                                    
                                                if (result.Success)
                                                {
                                                    filesGenerated++;
                                                }
                                                else if (result.IsCancelled)
                                                {
                                                    cancelledCombinations++;
                                                    cancellationToken.ThrowIfCancellationRequested();
                                                }
                                                else
                                                {
                                                    combinationsSkipped++;
                                                    if (result.SkipReason == "NoData")
                                                    {
                                                        skippedNoData++;
                                                        SkippedDatasetLogger.LogSkippedDataset(comboNumber, 0, fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, "NoData");
                                                    }
                                                    else if (result.SkipReason == "ExcelRowLimit")
                                                    {
                                                        skippedRowLimit++;
                                                        SkippedDatasetLogger.LogSkippedDataset(comboNumber, result.RowCount, fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, "RowLimit");
                                                    }
                                                }
                                            }
                                            catch (OperationCanceledException)
                                            {
                                                cancelledCombinations++;
                                                throw; // Re-throw to exit the loops
                                            }
                                            catch (Exception ex)
                                            {
                                                var errorMsg = $"Import error combination {comboNumber}: {ex.Message}";
                                                _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Error, errorMsg, "Import"));
                                                _monitoringService.AddLog(MonitoringLogLevel.Error, errorMsg, "Import");
                                                System.Diagnostics.Debug.WriteLine($"ERROR: {errorMsg}");
                                                System.Diagnostics.Debug.WriteLine($"Filters - HSCode:{hsCode}, Product:{product}, IEC:{iec}, Importer:{importer}, Country:{country}, Name:{name}, Port:{port}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }, cancellationToken);

            // Check if operation was cancelled
            if (cancellationToken.IsCancellationRequested)
            {
                _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Cancelled, $"Import cancelled - {filesGenerated} files generated before cancellation"));
                return;
            }

            SkippedDatasetLogger.LogProcessingSummary(combinationsProcessed, filesGenerated, combinationsSkipped);

            var summaryMessage = "Import Processing Complete\n\n";
            summaryMessage += $"Files Generated: {filesGenerated:N0} Excel files created successfully\n";
            summaryMessage += $"Total Processed: {combinationsProcessed:N0} parameter combinations checked\n\n";

            if (combinationsSkipped > 0)
            {
                summaryMessage += $"Skipped Combinations: {combinationsSkipped:N0} total\n";
                if (skippedNoData > 0)
                    summaryMessage += $"  • No Data Found: {skippedNoData:N0} combinations had zero matching records\n";
                if (skippedRowLimit > 0)
                    summaryMessage += $"  • Excel Row Limit: {skippedRowLimit:N0} combinations exceeded 1,048,575 row limit\n";
                summaryMessage += $"\nNote: Skipped datasets are logged in: Logs\\SkippedDatasets_{DateTime.Now:yyyyMMdd}.log\n";
            }

            if (filesGenerated > 0)
            {
                summaryMessage += $"\nSuccess Rate: {(double)filesGenerated / combinationsProcessed * 100:F1}% of combinations produced files";
            }

            var messageType = MessageBoxImage.Information;
            var messageTitle = "Import Batch Complete";
            if (filesGenerated == 0)
            {
                messageType = MessageBoxImage.Warning;
                messageTitle = "No Files Generated";
                summaryMessage += "\n\nNext Steps: Review your filter criteria - all combinations resulted in no data or exceeded limits.";
            }
            else if (skippedRowLimit > 0)
            {
                messageType = MessageBoxImage.Warning;
                messageTitle = "Processing Complete - Some Data Limits Exceeded";
                summaryMessage += "\n\nSuggestion: Consider adding more specific filters to reduce row counts for large datasets.";
            }

            var importStatusMessage = filesGenerated == 0
                ? (skippedNoData > 0 && skippedRowLimit == 0 ? "Import complete: All combinations had no data" :
                   skippedRowLimit > 0 && skippedNoData == 0 ? "Import complete: All combinations exceeded row limits" :
                   (skippedNoData > 0 && skippedRowLimit > 0 ? $"Import complete: 0 files - {skippedNoData} no data, {skippedRowLimit} over limits" : "Import complete: No files"))
                : (combinationsSkipped == 0 ? $"Import complete: {filesGenerated} files" : $"Import complete: {filesGenerated} files, {combinationsSkipped} skipped");

            _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Completed, importStatusMessage));

            MessageBox.Show(summaryMessage, messageTitle, MessageBoxButton.OK, messageType);
        }
    }
}
