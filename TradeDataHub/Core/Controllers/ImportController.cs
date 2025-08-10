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
using TradeDataHub.Core.Services;

namespace TradeDataHub.Core.Controllers
{
    /// <summary>
    /// Controller for import operations
    /// </summary>
    public class ImportController : IImportController
    {
        private readonly ImportExcelService _importExcelService;
        private readonly IValidationService _validationService;
        private readonly IResultProcessorService _resultProcessorService;
        private readonly MonitoringService _monitoringService;
        private readonly Dispatcher _dispatcher;

        public ImportController(
            ImportExcelService importExcelService,
            IValidationService validationService,
            IResultProcessorService resultProcessorService,
            MonitoringService monitoringService,
            Dispatcher dispatcher)
        {
            _importExcelService = importExcelService;
            _validationService = validationService;
            _resultProcessorService = resultProcessorService;
            _monitoringService = monitoringService;
            _dispatcher = dispatcher;
        }

        public async Task RunAsync(ImportInputs importInputs, CancellationToken cancellationToken, string selectedView, string selectedStoredProcedure)
        {
            var fromMonth = importInputs.FromMonth;
            var toMonth = importInputs.ToMonth;

            // Validate using ValidationService
            var validationResult = _validationService.ValidateImportOperation(importInputs, selectedView, selectedStoredProcedure);
            if (!validationResult.IsValid)
            {
                MessageBox.Show(validationResult.ErrorMessage, validationResult.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
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

            // Initialize processing counters using ResultProcessorService
            var counters = _resultProcessorService.InitializeCounters();

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

                                            counters.CombinationsProcessed++;
                                            var comboNumber = counters.CombinationsProcessed; // capture for closure
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
                                                    
                                                // Process the result using ResultProcessorService
                                                _resultProcessorService.ProcessImportResult(counters, result, counters.CombinationsProcessed, _monitoringService);
                                                
                                                if (result.IsCancelled)
                                                {
                                                    cancellationToken.ThrowIfCancellationRequested();
                                                }
                                            }
                                            catch (OperationCanceledException)
                                            {
                                                counters.CancelledCombinations++;
                                                throw; // Re-throw to exit the loops
                                            }
                                            catch (Exception ex)
                                            {
                                                // Handle error using ResultProcessorService
                                                var filterDetails = $"HSCode:{hsCode}, Product:{product}, IEC:{iec}, Importer:{importer}, Country:{country}, Name:{name}, Port:{port}";
                                                _resultProcessorService.HandleProcessingError(ex, counters.CombinationsProcessed, _monitoringService, "Import", filterDetails);
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
                _resultProcessorService.HandleCancellation(counters, _monitoringService, "Import");
                return;
            }

            // Log processing summary using ResultProcessorService
            _resultProcessorService.LogProcessingSummary(counters);

            // Generate completion summary using ResultProcessorService
            var summaryMessage = _resultProcessorService.GenerateCompletionSummary(counters, "Import");

            // Show completion message
            MessageBox.Show(summaryMessage, "Import Processing Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            // Update final status
            var importStatusMessage = counters.FilesGenerated == 0
                ? (counters.SkippedNoData > 0 && counters.SkippedRowLimit == 0 ? "Import complete: All combinations had no data" :
                   counters.SkippedRowLimit > 0 && counters.SkippedNoData == 0 ? "Import complete: All combinations exceeded row limits" :
                   (counters.SkippedNoData > 0 && counters.SkippedRowLimit > 0 ? $"Import complete: 0 files - {counters.SkippedNoData} no data, {counters.SkippedRowLimit} over limits" : "Import complete: No files"))
                : (counters.CombinationsSkipped == 0 ? $"Import complete: {counters.FilesGenerated} files" : $"Import complete: {counters.FilesGenerated} files, {counters.CombinationsSkipped} skipped");

            _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Completed, importStatusMessage));
        }
    }
}
