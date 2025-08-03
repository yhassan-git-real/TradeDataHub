using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Data;
using System.Threading.Tasks;
using TradeDataHub.Core;
using TradeDataHub.Features.Export;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;

namespace TradeDataHub
{
    public partial class MainWindow : Window
    {
        private readonly DataAccess _dataAccess;
        private readonly ExcelService _excelService;

        public MainWindow()
        {
            InitializeComponent();
            _dataAccess = new DataAccess();
            _excelService = new ExcelService();
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Processing...";
            GenerateButton.IsEnabled = false;

            try
            {
                // 🎯 Check which process type is selected
                if (rbImport.IsChecked == true)
                {
                    // Import is selected - do nothing for now (future placeholder)
                    MessageBox.Show("Import functionality is not yet implemented.\nThis feature is reserved for future development.", 
                                  "Import Not Available", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Text = "Import process skipped - not implemented";
                    return;
                }

                if (rbExport.IsChecked == true)
                {
                    // Export is selected - run the existing export process
                    await RunExportProcess();
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => StatusText.Text = "An error occurred.");
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Dispatcher.Invoke(() => {
                    GenerateButton.IsEnabled = true;
                    StatusText.Text = "Ready";
                });
            }
        }

        private async Task RunExportProcess()
        {
            var fromMonth = Txt_Frommonth.Text;
            var toMonth = Txtmonthto.Text;

            // Enhanced parameter validation using ParameterHelper
            var validation = ParameterHelper.ValidateExportParameters(
                fromMonth, toMonth, ParameterHelper.WILDCARD, ParameterHelper.WILDCARD, ParameterHelper.WILDCARD, ParameterHelper.WILDCARD, ParameterHelper.WILDCARD, ParameterHelper.WILDCARD, ParameterHelper.WILDCARD);
            
            if (!validation.IsValid)
            {
                string errorMessage = "Parameter Validation Failed:\n" + string.Join("\n", validation.Errors);
                MessageBox.Show(errorMessage, "Invalid Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hsCodes = ParameterHelper.ParseFilterList(Txt_HS.Text);
            var ports = ParameterHelper.ParseFilterList(txt_Port.Text);
            var products = ParameterHelper.ParseFilterList(Txt_Product.Text);
            var exporters = ParameterHelper.ParseFilterList(Txt_Exporter.Text);
            var foreignCountries = ParameterHelper.ParseFilterList(txt_ForCount.Text);
            var foreignNames = ParameterHelper.ParseFilterList(Txt_ForName.Text);
            var iecs = ParameterHelper.ParseFilterList(Txt_IEC.Text);

            int filesGenerated = 0;
            int combinationsProcessed = 0;
            int combinationsSkipped = 0;
            int skippedNoData = 0;
            int skippedRowLimit = 0;

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
                                            combinationsProcessed++;
                                            Dispatcher.Invoke(() => StatusText.Text = $"Processing combination {combinationsProcessed}...");

                                            try
                                            {
                                                // Step 1: Execute SP and get data reader with row count (single SP execution)
                                                var result = _excelService.CreateReport(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
                                                
                                                if (result.Success)
                                                {
                                                    filesGenerated++;
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
                                            catch (Exception ex)
                                            {
                                                // Log individual combination errors but continue processing
                                                var errorMsg = $"Error processing combination {combinationsProcessed}: {ex.Message}";
                                                Dispatcher.Invoke(() => StatusText.Text = errorMsg);
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
            });

            // Log processing summary
            SkippedDatasetLogger.LogProcessingSummary(combinationsProcessed, filesGenerated, combinationsSkipped);

            // Create enhanced completion summary with clear skip reasons
            var summaryMessage = "Processing Complete\n\n";
            
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
            var messageTitle = "Batch Processing Complete";
            
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

            Dispatcher.Invoke(() => StatusText.Text = GetStatusSummary(filesGenerated, skippedNoData, skippedRowLimit));
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
