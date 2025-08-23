using System.Linq;
using System.Text.RegularExpressions;

namespace TradeDataHub.Core.Helpers
{
    /// <summary>
    /// File name helper for Import operations, utilizing shared functionality from BaseFileNameHelper.
    /// Maintains backward compatibility with existing import functionality.
    /// </summary>
    public static class Import_FileNameHelper
    {
        /// <summary>
        /// Generates a standardized import file name based on the provided parameters.
        /// </summary>
        /// <param name="fromMonth">Start month in YYYYMM format</param>
        /// <param name="toMonth">End month in YYYYMM format</param>
        /// <param name="hsCode">HS Code filter</param>
        /// <param name="product">Product filter</param>
        /// <param name="iec">IEC filter</param>
        /// <param name="importer">Importer filter</param>
        /// <param name="foreignCountry">Foreign country filter</param>
        /// <param name="foreignName">Foreign name filter</param>
        /// <param name="port">Port filter</param>
        /// <param name="fileSuffix">File suffix (e.g., "IMP")</param>
        /// <returns>Generated import file name</returns>
        public static string GenerateImportFileName(string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string foreignCountry, string foreignName, string port, string fileSuffix)
        {
            // Build month range segment using base helper with import-specific default
            string monthSegment = BaseFileNameHelper.BuildMonthRangeSegment(fromMonth, toMonth, "UNK00");

            // Build core file name from parameters using ImportParameterHelper.WILDCARD
            string[] parameters = { hsCode, product, iec, importer, foreignCountry, foreignName, port };
            string core = BaseFileNameHelper.BuildCoreFileName(parameters, ImportParameterHelper.WILDCARD, "ALL");

            return $"{core}_{monthSegment}{fileSuffix}.xlsx";
        }

        /// <summary>
        /// Legacy method: Builds month segment for backward compatibility.
        /// Now delegates to BaseFileNameHelper for consistency.
        /// </summary>
        /// <param name="fromMonth">Start month in YYYYMM format</param>
        /// <param name="toMonth">End month in YYYYMM format</param>
        /// <returns>Month range string</returns>
        private static string BuildMonthSegment(string fromMonth, string toMonth)
        {
            return BaseFileNameHelper.BuildMonthRangeSegment(fromMonth, toMonth, "UNK00");
        }

        /// <summary>
        /// Legacy method: Converts YYYYMM to MMMYY format for backward compatibility.
        /// Now delegates to BaseFileNameHelper for consistency.
        /// </summary>
        /// <param name="yyyymm">Date string in YYYYMM format</param>
        /// <returns>Month abbreviation in MMMYY format</returns>
        private static string ConvertToMonYY(string? yyyymm)
        {
            return BaseFileNameHelper.ConvertToMonthAbbreviation(yyyymm ?? "", "UNK00");
        }

        /// <summary>
        /// Legacy method: Sanitizes parameter strings for backward compatibility.
        /// Now delegates to BaseFileNameHelper for consistency.
        /// </summary>
        /// <param name="raw">Raw parameter string</param>
        /// <returns>Sanitized parameter string</returns>
        private static string Sanitize(string raw)
        {
            return BaseFileNameHelper.SanitizeParameter(raw);
        }
    }
}
