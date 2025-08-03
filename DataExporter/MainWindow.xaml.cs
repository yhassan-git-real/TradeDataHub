using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Data;
using System.Threading.Tasks;

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
                var fromMonth = Txt_Frommonth.Text;
                var toMonth = Txtmonthto.Text;

                if (string.IsNullOrWhiteSpace(fromMonth) || string.IsNullOrWhiteSpace(toMonth))
                {
                    MessageBox.Show("Please enter a 'From' and 'To' month.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var hsCodes = GetFilterList(Txt_HS.Text);
                var ports = GetFilterList(txt_Port.Text);
                var products = GetFilterList(Txt_Product.Text);
                var exporters = GetFilterList(Txt_Exporter.Text);
                var foreignCountries = GetFilterList(txt_ForCount.Text);
                var foreignNames = GetFilterList(Txt_ForName.Text);
                var iecs = GetFilterList(Txt_IEC.Text);
                var reportType = ((ComboBoxItem)Combo2.SelectedItem).Content.ToString();

                int filesGenerated = 0;
                int combinationsProcessed = 0;

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

                                                // Step 3 & 4: Get Data and Create Report
                                                DataTable data = _dataAccess.GetData(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);

                                                if (data != null && data.Rows.Count > 0)
                                                {
                                                    _excelService.CreateReport(data, fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
                                                    filesGenerated++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                StatusText.Text = $"Processing complete. {filesGenerated} files generated.";
                MessageBox.Show($"Batch processing finished.\n\n{filesGenerated} report(s) were generated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "An error occurred.";
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GenerateButton.IsEnabled = true;
                StatusText.Text = "Ready";
            }
        }

        private List<string> GetFilterList(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new List<string> { "%" };
            }
            return rawText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }
}
