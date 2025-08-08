using System.Linq;
using System.Text.RegularExpressions;

namespace TradeDataHub.Core.Helpers
{
    public static class Import_FileNameHelper
    {
        private static readonly Regex IllegalChars = new Regex("[\\\\/:*?\"<>|]", RegexOptions.Compiled);

        public static string GenerateImportFileName(string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string foreignCountry, string foreignName, string port, string fileSuffix)
        {
            string monthSegment = BuildMonthSegment(fromMonth, toMonth);

            string[] parts = new [] { hsCode, product, iec, importer, foreignCountry, foreignName, port }
                .Where(p => !string.IsNullOrWhiteSpace(p) && p != ParameterHelper.WILDCARD)
                .Select(Sanitize)
                .ToArray();

            string core = string.Join("_", parts);
            if (string.IsNullOrWhiteSpace(core)) core = "ALL";

            string fileName = core + "_" + monthSegment + fileSuffix + ".xlsx";
            fileName = fileName.Replace("__", "_");
            return fileName;
        }

        private static string BuildMonthSegment(string fromMonth, string toMonth)
        {
            string seg1 = ConvertToMonYY(fromMonth);
            string seg2 = ConvertToMonYY(toMonth);
            return seg1 == seg2 ? seg1 : seg1 + "-" + seg2;
        }

        private static string ConvertToMonYY(string? yyyymm)
        {
            if (string.IsNullOrWhiteSpace(yyyymm) || yyyymm.Length != 6) return "UNK00";
            int year = int.Parse(yyyymm.Substring(0,4));
            int month = int.Parse(yyyymm.Substring(4,2));
            string[] mons = {"JAN","FEB","MAR","APR","MAY","JUN","JUL","AUG","SEP","OCT","NOV","DEC"};
            string mon = month >=1 && month <=12 ? mons[month-1] : "UNK";
            return mon + (year % 100).ToString("D2");
        }

        private static string Sanitize(string raw)
        {
            string s = raw.Trim();
            s = s.Replace(' ', '_');
            s = IllegalChars.Replace(s, "");
            return s;
        }
    }
}
