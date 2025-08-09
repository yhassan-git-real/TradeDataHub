using System;

namespace TradeDataHub.Core.Helpers
{
    public static class Export_FileNameHelper
    {
        public static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            
            return fileName;
        }

        public static string GetMonthAbbreviation(string yyyymm)
        {
            if (yyyymm.Length != 6 || !int.TryParse(yyyymm, out _))
            {
                return "MMM";
            }

            int month = int.Parse(yyyymm.Substring(4, 2));
            string year = yyyymm.Substring(2, 2);
            
            string monthAbbr = month switch
            {
                1 => "JAN", 2 => "FEB", 3 => "MAR", 4 => "APR",
                5 => "MAY", 6 => "JUN", 7 => "JUL", 8 => "AUG",
                9 => "SEP", 10 => "OCT", 11 => "NOV", 12 => "DEC",
                _ => "MMM"
            };

            return $"{monthAbbr}{year}";
        }

        public static string EnsureUniqueFileName(string basePath, string fileName)
        {
            string fullPath = System.IO.Path.Combine(basePath, fileName);
            
            if (!System.IO.File.Exists(fullPath))
            {
                return fileName;
            }

            string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string extension = System.IO.Path.GetExtension(fileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            return $"{nameWithoutExt}_{timestamp}{extension}";
        }

        public static string GenerateExportFileName(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            string mon1 = GetMonthAbbreviation(fromMonth);
            string mon2 = GetMonthAbbreviation(toMonth);
            string mon = (mon1 == mon2) ? mon1 : $"{mon1}-{mon2}";

            var fileName1 = "";
            if (hsCode != "%") fileName1 += hsCode;
            if (product != "%") fileName1 += "_" + product.Replace(' ', '_');
            if (iec != "%") fileName1 += "_" + iec;
            if (exporter != "%") fileName1 += "_" + exporter.Replace(' ', '_');
            if (country != "%") fileName1 += "_" + country.Replace(' ', '_');
            if (name != "%") fileName1 += "_" + name.Replace(' ', '_');
            if (port != "%") fileName1 += "_" + port.Replace(' ', '_');

            if (fileName1.StartsWith("_"))
            {
                fileName1 = fileName1.Substring(1);
            }

            return $"{fileName1}_{mon}EXP.xlsx";
        }
    }
}
