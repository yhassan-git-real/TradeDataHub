using System;
using System.Data;
using System.IO;
using System.Threading;
using OfficeOpenXml;
using Microsoft.Data.SqlClient;
using TradeDataHub.Config;
using TradeDataHub.Core.Logging;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// High-performance Excel processor that streams data in configurable chunks to optimize memory usage
    /// while maintaining processing speed for large datasets
    /// </summary>
    public class StreamingExcelProcessor
    {
        private readonly PerformanceSettings _performanceSettings;
        private readonly ModuleLogger _logger;
        private readonly string? _processId;

        public StreamingExcelProcessor(ModuleLogger logger, string? processId = null)
        {
            _performanceSettings = ConfigurationCacheService.GetPerformanceSettings();
            _logger = logger;
            _processId = processId;
        }

        /// <summary>
        /// Loads data from SqlDataReader into Excel worksheet using optimized processing
        /// Balances memory efficiency with database performance
        /// </summary>
        /// <param name="reader">The SqlDataReader containing data</param>
        /// <param name="worksheet">The Excel worksheet to populate</param>
        /// <param name="recordCount">Total number of records to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if processing completed successfully</returns>
        public bool LoadDataFromReaderOptimized(SqlDataReader reader, ExcelWorksheet worksheet, long recordCount, CancellationToken cancellationToken = default)
        {
            try
            {
                int fieldCount = reader.FieldCount;
                
                // Use larger chunk size for better database performance while maintaining memory control
                int chunkSize = recordCount < 100000 ? (int)recordCount : 
                               Math.Max(10000, Math.Min(50000, (int)(recordCount / 20)));
                
                int currentRow = 2; // Start after header row
                int processedRows = 0;
                int chunksProcessed = 0;

                var startMessage = StringPool.Instance.CreateChunkedStartMessage(recordCount, chunkSize);
                _logger?.LogStep("StreamingProcessor", startMessage, _processId);

                // Always use direct loading for optimal performance - eliminates chunked processing overhead
                _logger?.LogStep("StreamingProcessor", $"Using direct loading for optimal performance ({recordCount:N0} records)", _processId);
                
                // Load data directly using EPPlus optimized method - this is fastest
                worksheet.Cells[2, 1].LoadFromDataReader(reader, false);
                processedRows = (int)recordCount;
                chunksProcessed = 1;
                
                // Skip chunked processing entirely
                if (false)
                {
                    // Process larger datasets in optimized chunks
                    while (processedRows < recordCount && !cancellationToken.IsCancellationRequested)
                    {
                        var chunk = ReadDataChunk(reader, fieldCount, chunkSize, cancellationToken);
                        if (chunk.Count == 0) break;

                        // Write chunk to worksheet efficiently
                        WriteChunkToWorksheet(worksheet, chunk, currentRow, fieldCount);
                        
                        processedRows += chunk.Count;
                        currentRow += chunk.Count;
                        chunksProcessed++;

                        // Progress reporting every 2 chunks for large datasets
                        if (chunksProcessed % 2 == 0 || processedRows >= recordCount)
                        {
                            double progress = (double)processedRows / recordCount * 100;
                            var progressMessage = StringPool.Instance.CreateProgressMessage(processedRows, (int)recordCount, progress);
                            _logger?.LogStep("StreamingProcessor", progressMessage, _processId);
                        }

                        // Less aggressive GC for better performance
                        if (_performanceSettings.ExcelProcessing.EnableProgressiveGC && 
                            processedRows % (_performanceSettings.ExcelProcessing.GCCollectionInterval * 2) == 0 &&
                            processedRows > 0)
                        {
                            GC.Collect(0, GCCollectionMode.Optimized);
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                var completeMessage = StringPool.Instance.CreateChunkedCompleteMessage(processedRows, chunksProcessed);
                _logger?.LogStep("StreamingProcessor", completeMessage, _processId);
                return true;
            }
            catch (OperationCanceledException)
            {
                var cancelledMessage = StringPool.Instance.GetPooled("Processing cancelled by user");
                _logger?.LogStep("StreamingProcessor", cancelledMessage, _processId);
                throw;
            }
            catch (Exception ex)
            {
                var failedMessage = StringPool.Instance.GetPooledFormat("StreamingProcessor failed: {0}", ex.Message);
                _logger?.LogError(failedMessage, ex, _processId);
                throw;
            }
        }

        /// <summary>
        /// Reads a chunk of data from SqlDataReader with optimized performance
        /// </summary>
        private System.Collections.Generic.List<object[]> ReadDataChunk(SqlDataReader reader, int fieldCount, int chunkSize, CancellationToken cancellationToken)
        {
            var chunk = new System.Collections.Generic.List<object[]>(chunkSize);
            
            int rowsRead = 0;
            // Pre-allocate object array to reduce allocations
            var rowDataBuffer = new object[fieldCount];
            
            while (rowsRead < chunkSize && reader.Read() && !cancellationToken.IsCancellationRequested)
            {
                // Create new array for each row (required since we're storing references)
                var rowData = new object[fieldCount];
                
                // Use GetValues for better performance than individual column access
                reader.GetValues(rowData);
                chunk.Add(rowData);
                rowsRead++;
            }

            return chunk;
        }

        /// <summary>
        /// Efficiently writes a chunk of data to Excel worksheet
        /// </summary>
        private void WriteChunkToWorksheet(ExcelWorksheet worksheet, System.Collections.Generic.List<object[]> chunk, int startRow, int fieldCount)
        {
            if (chunk.Count == 0) return;

            // Convert chunk to 2D array for efficient EPPlus loading
            var dataArray = new object[chunk.Count, fieldCount];
            for (int i = 0; i < chunk.Count; i++)
            {
                for (int j = 0; j < fieldCount; j++)
                {
                    dataArray[i, j] = chunk[i][j];
                }
            }

            // Load chunk data into worksheet efficiently
            var range = worksheet.Cells[startRow, 1, startRow + chunk.Count - 1, fieldCount];
            range.Value = dataArray;
        }

        /// <summary>
        /// Optimized method to add headers to worksheet
        /// </summary>
        public void AddHeaders(SqlDataReader reader, ExcelWorksheet worksheet)
        {
            int fieldCount = reader.FieldCount;
            var headerArray = new string[1, fieldCount];
            
            for (int col = 0; col < fieldCount; col++)
            {
                headerArray[0, col] = reader.GetName(col);
            }

            // Set headers in one operation
            worksheet.Cells[1, 1, 1, fieldCount].Value = headerArray;
        }

        /// <summary>
        /// Gets memory usage information for monitoring
        /// </summary>
        public (long WorkingSet, long PrivateMemory, double MemoryPressure) GetMemoryInfo()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            long workingSet = process.WorkingSet64;
            long privateMemory = process.PrivateMemorySize64;
            
            // Calculate memory pressure as percentage of configured max
            long maxMemoryBytes = _performanceSettings.ExcelProcessing.MaxMemoryUsageMB * 1024L * 1024L;
            double memoryPressure = (double)privateMemory / maxMemoryBytes;
            
            return (workingSet, privateMemory, memoryPressure);
        }

        /// <summary>
        /// Checks if memory pressure exceeds threshold and suggests action
        /// </summary>
        public bool IsMemoryPressureHigh()
        {
            var (_, _, memoryPressure) = GetMemoryInfo();
            return memoryPressure > _performanceSettings.ExcelProcessing.MemoryPressureThreshold;
        }
    }
}
