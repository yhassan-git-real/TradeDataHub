using System;
using System.Threading;
using System.Windows.Threading;
using TradeDataHub.Core.Logging;
using TradeDataHub.Features.Export;
using TradeDataHub.Features.Import;
using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Monitoring.Models;
using MonitoringLogLevel = TradeDataHub.Features.Monitoring.Models.LogLevel;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for processing and tracking operation results
    /// </summary>
    public class ResultProcessorService : IResultProcessorService
    {
        private readonly Dispatcher _dispatcher;

        public ResultProcessorService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public ProcessingCounters InitializeCounters()
        {
            return new ProcessingCounters();
        }

        public void ProcessExportResult(ProcessingCounters counters, ExcelResult result, int combinationNumber, MonitoringService monitoringService)
        {
            if (result.Success)
            {
                counters.FilesGenerated++;
            }
            else if (result.IsCancelled)
            {
                counters.CancelledCombinations++;
            }
            else
            {
                counters.CombinationsSkipped++;
                
                // Track skip reasons for better completion message
                if (result.SkipReason == SkipReason.NoData)
                {
                    counters.SkippedNoData++;
                }
                else if (result.SkipReason == SkipReason.ExcelRowLimit)
                {
                    counters.SkippedRowLimit++;
                }
            }
        }

        public void ProcessImportResult(ProcessingCounters counters, ImportExcelResult result, int combinationNumber, MonitoringService monitoringService)
        {
            if (result.Success)
            {
                counters.FilesGenerated++;
            }
            else if (result.IsCancelled)
            {
                counters.CancelledCombinations++;
            }
            else
            {
                counters.CombinationsSkipped++;
                
                // Track skip reasons for better completion message
                if (result.SkipReason == "NoData")
                {
                    counters.SkippedNoData++;
                }
                else if (result.SkipReason == "ExcelRowLimit")
                {
                    counters.SkippedRowLimit++;
                }
            }
        }

        public string GenerateCompletionSummary(ProcessingCounters counters, string operationType)
        {
            var summaryMessage = $"{operationType} Processing Complete\n\n";
            
            // Success metrics
            summaryMessage += $"Files Generated: {counters.FilesGenerated:N0} Excel files created successfully\n";
            summaryMessage += $"Total Processed: {counters.CombinationsProcessed:N0} parameter combinations checked\n\n";
            
            // Skip details with clear explanations
            if (counters.CombinationsSkipped > 0)
            {
                summaryMessage += $"Skipped Combinations: {counters.CombinationsSkipped:N0} total\n";
                
                if (counters.SkippedNoData > 0)
                {
                    summaryMessage += $"  • No Data Found: {counters.SkippedNoData:N0} combinations had zero matching records\n";
                }
                
                if (counters.SkippedRowLimit > 0)
                {
                    summaryMessage += $"  • Excel Row Limit: {counters.SkippedRowLimit:N0} combinations exceeded Excel's 1,048,576 row limit\n";
                }
                
                summaryMessage += "\n";
            }
            
            // Final completion message
            if (counters.FilesGenerated == 0)
            {
                summaryMessage += "No files were generated. All combinations were either skipped or contained no data.";
            }
            else if (counters.CombinationsSkipped == 0)
            {
                summaryMessage += $"Operation completed successfully! All {counters.CombinationsProcessed:N0} combinations generated files.";
            }
            else
            {
                var successRate = (double)counters.FilesGenerated / counters.CombinationsProcessed * 100;
                summaryMessage += $"Operation completed with {successRate:F1}% success rate.";
            }
            
            return summaryMessage;
        }

        public void UpdateProcessingStatus(int combinationNumber, MonitoringService monitoringService, string operationType)
        {
            _dispatcher.Invoke(() => 
                monitoringService.UpdateStatus(StatusType.Running, $"Processing {operationType} combination {combinationNumber}...", operationType));
        }

        public void HandleProcessingError(Exception ex, int combinationNumber, MonitoringService monitoringService, string operationType, string filterDetails = "")
        {
            var errorMsg = $"Error processing combination {combinationNumber}: {ex.Message}";
            _dispatcher.Invoke(() => monitoringService.UpdateStatus(StatusType.Error, errorMsg, operationType));
            monitoringService.AddLog(MonitoringLogLevel.Error, errorMsg, operationType);
            
            System.Diagnostics.Debug.WriteLine($"ERROR: {errorMsg}");
            if (!string.IsNullOrEmpty(filterDetails))
            {
                System.Diagnostics.Debug.WriteLine($"Filters - {filterDetails}");
            }
        }

        public void HandleCancellation(ProcessingCounters counters, MonitoringService monitoringService, string operationType)
        {
            _dispatcher.Invoke(() => 
                monitoringService.UpdateStatus(StatusType.Cancelled, $"{operationType} cancelled - {counters.FilesGenerated} files generated before cancellation"));
        }

        public void LogProcessingSummary(ProcessingCounters counters)
        {
            SkippedDatasetLogger.LogProcessingSummary(counters.CombinationsProcessed, counters.FilesGenerated, counters.CombinationsSkipped);
        }
    }
}
