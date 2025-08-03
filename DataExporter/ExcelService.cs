using ClosedXML.Excel;
using System.Data;
using System.IO;

namespace TradeDataHub
{
    public class ExcelService
    {
        public void CreateReport(DataTable data, string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            // --- Dynamic File Naming Logic ---
            string mon1 = GetMonthAbbreviation(fromMonth);
            string mon2 = GetMonthAbbreviation(toMonth);
            string mon = (mon1 == mon2) ? mon1 : $"{mon1}-{mon2}";

            var fileNameBuilder = new System.Text.StringBuilder();
            if (hsCode != "%") fileNameBuilder.Append(hsCode);
            if (product != "%") fileNameBuilder.Append($"_{product.Replace(' ', '_')}");
            if (iec != "%") fileNameBuilder.Append($"_{iec}");
            if (exporter != "%") fileNameBuilder.Append($"_{exporter.Replace(' ', '_')}");
            if (country != "%") fileNameBuilder.Append($"_{country.Replace(' ', '_')}");
            if (name != "%") fileNameBuilder.Append($"_{name.Replace(' ', '_')}");
            if (port != "%") fileNameBuilder.Append($"_{port.Replace(' ', '_')}");

            if (fileNameBuilder.Length > 0 && fileNameBuilder[0] == '_')
            {
                fileNameBuilder.Remove(0, 1);
            }

            string fileName = $"{fileNameBuilder}_{mon}EXP.xlsx";

            // --- Excel Generation using ClosedXML ---

            // Use template path from config. If it doesn't exist, create a new workbook.
            var templatePath = App.Settings.Files.TemplatePath;
            XLWorkbook workbook;
            if (!string.IsNullOrEmpty(templatePath) && File.Exists(templatePath))
            {
                workbook = new XLWorkbook(templatePath);
            }
            else
            {
                workbook = new XLWorkbook();
                workbook.Worksheets.Add("Data"); // Add a default sheet if no template
            }

            var wks = workbook.Worksheets.First(); // Use the first sheet from template or new

            var table = wks.Cell("A1").InsertTable(data, "ExportData", true);

            if (data.Rows.Count > 0)
            {
                var range = table.DataRange;
                range.Style.Font.FontName = "Times New Roman";
                range.Style.Font.FontSize = 10;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                if (data.Columns.Count > 2)
                {
                    wks.Column(3).Style.NumberFormat.Format = "dd-mmm-yy";
                }

                wks.Columns().AdjustToContents();
            }

            // Save the file to the output directory from config
            string outputDir = App.Settings.Files.OutputDirectory;
            Directory.CreateDirectory(outputDir); // Ensure the directory exists
            string outputPath = Path.Combine(outputDir, fileName);

            workbook.SaveAs(outputPath);
        }

        private string GetMonthAbbreviation(string yyyymm)
        {
            if (yyyymm.Length != 6 || !int.TryParse(yyyymm, out _))
            {
                return "MMM"; // Default
            }

            int month = int.Parse(yyyymm.Substring(4, 2));
            string year = yyyymm.Substring(2, 2);
            string monthAbbr = new System.Globalization.DateTimeFormatInfo().GetAbbreviatedMonthName(month).ToUpper();

            return $"{monthAbbr}{year}";
        }
    }
}
