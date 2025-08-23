using System;
using System.Linq;

namespace TradeDataHub.Core.Helpers
{
    /// <summary>
    /// File name helper for Export operations, utilizing shared functionality from BaseFileNameHelper.
    /// Maintains backward compatibility with existing export functionality.
    /// </summary>
    public static class Export_FileNameHelper
    {
        /// <summary>
        /// Sanitizes a file name by replacing invalid characters with underscores.
        /// </summary>
        /// <param name="fileName">The file name to sanitize</param>
        /// <returns>Sanitized file name</returns>
        public static string SanitizeFileName(string fileName)
        {
            return BaseFileNameHelper.SanitizeFileName(fileName);
        }

        /// <summary>
        /// Converts YYYYMM format to MMMYY format for export files.
        /// Maintains legacy behavior of returning "MMM" for invalid input.
        /// </summary>
        /// <param name="yyyymm">Date string in YYYYMM format</param>
        /// <returns>Month abbreviation in MMMYY format</returns>
        public static string GetMonthAbbreviation(string yyyymm)
        {
            return BaseFileNameHelper.ConvertToMonthAbbreviation(yyyymm, "MMM");
        }

        /// <summary>
        /// Ensures a file name is unique by adding a timestamp if the file already exists.
        /// </summary>
        /// <param name="basePath">Directory path where the file will be created</param>
        /// <param name="fileName">Proposed file name</param>
        /// <returns>Unique file name</returns>
        public static string EnsureUniqueFileName(string basePath, string fileName)
        {
            return BaseFileNameHelper.EnsureUniqueFileName(basePath, fileName);
        }

        /// <summary>
        /// Generates a standardized export file name based on the provided parameters.
        /// </summary>
        /// <param name="fromMonth">Start month in YYYYMM format</param>
        /// <param name="toMonth">End month in YYYYMM format</param>
        /// <param name="hsCode">HS Code filter</param>
        /// <param name="product">Product filter</param>
        /// <param name="iec">IEC filter</param>
        /// <param name="exporter">Exporter filter</param>
        /// <param name="country">Country filter</param>
        /// <param name="name">Name filter</param>
        /// <param name="port">Port filter</param>
        /// <returns>Generated export file name</returns>
        public static string GenerateExportFileName(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            // Build month range segment using base helper with export-specific default
            string monthRange = BaseFileNameHelper.BuildMonthRangeSegment(fromMonth, toMonth, "MMM");

            // Build core file name from parameters
            string[] parameters = { hsCode, product, iec, exporter, country, name, port };
            string core = BaseFileNameHelper.BuildCoreFileName(parameters, "%", "ALL");

            return $"{core}_{monthRange}EXP.xlsx";
        }
    }
}
