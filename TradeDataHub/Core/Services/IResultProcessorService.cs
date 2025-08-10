using TradeDataHub.Features.Monitoring.Services;
using TradeDataHub.Features.Export;
using TradeDataHub.Features.Import;
using System;
using System.Threading;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for processing and tracking operation results
    /// </summary>
    public interface IResultProcessorService
    {
        /// <summary>
        /// Initialize counters for a new processing session
        /// </summary>
        ProcessingCounters InitializeCounters();
        
        /// <summary>
        /// Process the result of an export operation
        /// </summary>
        void ProcessExportResult(ProcessingCounters counters, ExcelResult result, int combinationNumber, MonitoringService monitoringService);
        
        /// <summary>
        /// Process the result of an import operation
        /// </summary>
        void ProcessImportResult(ProcessingCounters counters, ImportExcelResult result, int combinationNumber, MonitoringService monitoringService);
        
        /// <summary>
        /// Generate a completion summary message
        /// </summary>
        string GenerateCompletionSummary(ProcessingCounters counters, string operationType);

        /// <summary>
        /// Update processing status for current combination
        /// </summary>
        void UpdateProcessingStatus(int combinationNumber, MonitoringService monitoringService, string operationType);

        /// <summary>
        /// Handle processing errors
        /// </summary>
        void HandleProcessingError(Exception ex, int combinationNumber, MonitoringService monitoringService, string operationType, string filterDetails = "");

        /// <summary>
        /// Handle operation cancellation
        /// </summary>
        void HandleCancellation(ProcessingCounters counters, MonitoringService monitoringService, string operationType);

        /// <summary>
        /// Log processing summary
        /// </summary>
        void LogProcessingSummary(ProcessingCounters counters);
    }

    /// <summary>
    /// Tracks processing counters for an operation
    /// </summary>
    public class ProcessingCounters
    {
        public int FilesGenerated { get; set; } = 0;
        public int CombinationsProcessed { get; set; } = 0;
        public int CombinationsSkipped { get; set; } = 0;
        public int SkippedNoData { get; set; } = 0;
        public int SkippedRowLimit { get; set; } = 0;
        public int CancelledCombinations { get; set; } = 0;
    }
}
