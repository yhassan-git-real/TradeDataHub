using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TradeDataHub.Core.Models;
using TradeDataHub.Core.Validation;
using TradeDataHub.Features.Export;
using TradeDataHub.Features.Export.Services;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;
using MonitoringLogLevel = TradeDataHub.Features.Monitoring.Models.LogLevel;
using TradeDataHub.Core.Logging;

namespace TradeDataHub.Core.Controllers
{
    /// <summary>
    /// Controller for export operations
    /// </summary>
    public class ExportController : IExportController
    {
        private readonly ExportExcelService _excelService;
        private readonly IParameterValidator _parameterValidator;
        private readonly MonitoringService _monitoringService;
        private readonly ExportObjectValidationService _exportObjectValidationService;
        private readonly Dispatcher _dispatcher;

        public ExportController(
            ExportExcelService excelService, 
            IParameterValidator parameterValidator, 
            MonitoringService monitoringService,
            ExportObjectValidationService exportObjectValidationService,
            Dispatcher dispatcher)
        {
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _parameterValidator = parameterValidator ?? throw new ArgumentNullException(nameof(parameterValidator));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
            _exportObjectValidationService = exportObjectValidationService ?? throw new ArgumentNullException(nameof(exportObjectValidationService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public async Task RunAsync(ExportInputs exportInputs, CancellationToken cancellationToken, string selectedView, string selectedStoredProcedure)
        {
            // Validate inputs
            if (exportInputs == null)
            {
                MessageBox.Show("Export inputs cannot be null.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate selected database objects
            if (!_exportObjectValidationService.ValidateObjects(selectedView, selectedStoredProcedure))
            {
                MessageBox.Show("The selected View or Stored Procedure is not valid. Please select valid database objects.", 
                    "Invalid Database Objects", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Centralized validation (months + format)
            var validation = _parameterValidator.ValidateExport(exportInputs);
            
            if (!validation.IsValid)
            {
                string errorMessage = "Parameter Validation Failed:\n" + string.Join("\n", validation.Errors);
                MessageBox.Show(errorMessage, "Invalid Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ports = exportInputs.Ports;
            var hsCodes = exportInputs.HSCodes;
            var products = exportInputs.Products;
            var exporters = exportInputs.Exporters;
            var foreignCountries = exportInputs.ForeignCountries;
            var foreignNames = exportInputs.ForeignNames;
            var iecs = exportInputs.IECs;

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
                            foreach (var exporter in exporters)
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
                                            _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Running, $"Processing combination {combinationsProcessed}...", "Export"));

                                            try
                                            {
                                                // Step 1: Execute SP and get data reader with row count (single SP execution)
                                                var result = _excelService.CreateReport(
                                                    combinationsProcessed, 
                                                    exportInputs.FromMonth, 
                                                    exportInputs.ToMonth, 
                                                    hsCode, 
                                                    product, 
                                                    iec, 
                                                    exporter, 
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
                                                    
                                                    // Track skip reasons for better completion message
                                                    if (result.SkipReason == SkipReason.NoData)
                                                    {
                                                        skippedNoData++;
                                                    }
                                                    else if (result.SkipReason == SkipReason.ExcelRowLimit)
                                                    {
                                                        skippedRowLimit++;
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
                                                // Log individual combination errors but continue processing
                                                var errorMsg = $"Error processing combination {combinationsProcessed}: {ex.Message}";
                                                _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Error, errorMsg, "Export"));
                                                _monitoringService.AddLog(MonitoringLogLevel.Error, errorMsg, "Export");
                                                System.Diagnostics.Debug.WriteLine($"ERROR: {errorMsg}");
                                                System.Diagnostics.Debug.WriteLine($"Filters - HSCode:{hsCode}, Product:{product}, IEC:{iec}, Exporter:{exporter}, Country:{country}, Name:{name}, Port:{port}");
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
                _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Cancelled, $"Export cancelled - {filesGenerated} files generated before cancellation"));
                return;
            }

            // Log processing summary
            SkippedDatasetLogger.LogProcessingSummary(combinationsProcessed, filesGenerated, combinationsSkipped);

            // Create enhanced completion summary with clear skip reasons
            var summaryMessage = "Export Processing Complete\n\n";
            
            // Success metrics
            summaryMessage += $"Files Generated: {filesGenerated:N0} Excel files created successfully\n";
            summaryMessage += $"Total Processed: {combinationsProcessed:N0} parameter combinations checked\n\n";
            
            // Skip details with clear explanations
            if (combinationsSkipped > 0)
            {
                summaryMessage += $"Skipped Combinations: {combinationsSkipped:N0} total\n";
                
                if (skippedNoData > 0)
                {
                    summaryMessage += $"  • No Data Found: {skippedNoData:N0} combinations had zero matching records\n";
                }
                
                if (skippedRowLimit > 0)
                {
                    summaryMessage += $"  • Excel Row Limit: {skippedRowLimit:N0} combinations exceeded 1,048,575 row limit\n";
                }
                
                summaryMessage += $"\nNote: Skipped datasets are logged in: Logs\\SkippedDatasets_{DateTime.Now:yyyyMMdd}.log\n";
            }
            
            // Performance summary
            if (filesGenerated > 0)
            {
                summaryMessage += $"\nSuccess Rate: {(double)filesGenerated / combinationsProcessed * 100:F1}% of combinations produced files";
            }

            // Determine message type and icon based on results
            var messageType = MessageBoxImage.Information;
            var messageTitle = "Export Batch Processing Complete";
            
            if (filesGenerated == 0)
            {
                messageType = MessageBoxImage.Warning;
                messageTitle = "No Files Generated";
                summaryMessage += "\n\nNext Steps: Review your filter criteria - all combinations resulted in no data or exceeded limits.";
            }
            else if (skippedRowLimit > 0)
            {
                messageType = MessageBoxImage.Warning;
                messageTitle = "Export Processing Complete - Some Data Limits Exceeded";
                summaryMessage += "\n\nSuggestion: Consider adding more specific filters to reduce row counts for large datasets.";
            }

            _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Completed, GetStatusSummary(filesGenerated, skippedNoData, skippedRowLimit)));
            MessageBox.Show(summaryMessage, messageTitle, MessageBoxButton.OK, messageType);
        }

        private string GetStatusSummary(int filesGenerated, int skippedNoData, int skippedRowLimit)
        {
            if (filesGenerated == 0)
            {
                if (skippedNoData > 0 && skippedRowLimit == 0)
                    return "Complete: No files generated - all combinations had no data";
                else if (skippedRowLimit > 0 && skippedNoData == 0)
                    return "Complete: No files generated - all combinations exceeded row limits";
                else if (skippedNoData > 0 && skippedRowLimit > 0)
                    return $"Complete: No files generated - {skippedNoData} no data, {skippedRowLimit} over limits";
                else
                    return "Complete: No files generated";
            }
            else
            {
                var totalSkipped = skippedNoData + skippedRowLimit;
                if (totalSkipped == 0)
                    return $"Complete: {filesGenerated} files generated successfully";
                else
                    return $"Complete: {filesGenerated} files, {totalSkipped} skipped ({skippedNoData} no data, {skippedRowLimit} over limits)";
            }
        }
    }
}
