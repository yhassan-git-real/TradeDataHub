using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TradeDataHub.Core.Helpers
{
    /// <summary>
    /// Base class providing common file naming functionality for both Export and Import operations.
    /// </summary>
    public static class BaseFileNameHelper
    {
        private static readonly string[] MonthAbbreviations = 
        {
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
            "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
        };

        /// <summary>
        /// Sanitizes a file name by replacing invalid characters with underscores.
        /// Uses .NET's comprehensive invalid character detection.
        /// </summary>
        /// <param name="fileName">The file name to sanitize</param>
        /// <returns>Sanitized file name</returns>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return fileName;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            
            return fileName;
        }

        /// <summary>
        /// Sanitizes a parameter string for use in file names.
        /// Trims, replaces spaces with underscores, and removes illegal characters.
        /// </summary>
        /// <param name="parameter">The parameter to sanitize</param>
        /// <returns>Sanitized parameter string</returns>
        public static string SanitizeParameter(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                return parameter;

            string sanitized = parameter.Trim();
            sanitized = sanitized.Replace(' ', '_');
            
            // Remove any remaining illegal characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), "");
            }
            
            return sanitized;
        }

        /// <summary>
        /// Converts YYYYMM format to MMMYY format (e.g., "202501" -> "JAN25").
        /// </summary>
        /// <param name="yyyymm">Date string in YYYYMM format</param>
        /// <param name="defaultValue">Default value to return for invalid input</param>
        /// <returns>Month abbreviation in MMMYY format</returns>
        public static string ConvertToMonthAbbreviation(string yyyymm, string defaultValue = "UNK00")
        {
            if (string.IsNullOrWhiteSpace(yyyymm) || yyyymm.Length != 6 || !int.TryParse(yyyymm, out _))
            {
                return defaultValue;
            }

            if (!int.TryParse(yyyymm.Substring(0, 4), out int year) || 
                !int.TryParse(yyyymm.Substring(4, 2), out int month))
            {
                return defaultValue;
            }

            if (month < 1 || month > 12)
            {
                return defaultValue == "UNK00" ? "UNK00" : "MMM";
            }

            string monthAbbr = MonthAbbreviations[month - 1];
            string yearSuffix = (year % 100).ToString("D2");
            
            return $"{monthAbbr}{yearSuffix}";
        }

        /// <summary>
        /// Builds a month range segment for file names (e.g., "JAN25" or "JAN25-MAR25").
        /// </summary>
        /// <param name="fromMonth">Start month in YYYYMM format</param>
        /// <param name="toMonth">End month in YYYYMM format</param>
        /// <param name="defaultValue">Default value for invalid months</param>
        /// <returns>Month range string</returns>
        public static string BuildMonthRangeSegment(string fromMonth, string toMonth, string defaultValue = "UNK00")
        {
            string mon1 = ConvertToMonthAbbreviation(fromMonth, defaultValue);
            string mon2 = ConvertToMonthAbbreviation(toMonth, defaultValue);
            
            return (mon1 == mon2) ? mon1 : $"{mon1}-{mon2}";
        }

        /// <summary>
        /// Filters and sanitizes parameters, removing null, empty, whitespace, and wildcard values.
        /// </summary>
        /// <param name="parameters">Array of parameters to filter</param>
        /// <param name="wildcard">Wildcard string to filter out</param>
        /// <returns>Array of sanitized, non-empty parameters</returns>
        public static string[] FilterAndSanitizeParameters(string[] parameters, string wildcard = "%")
        {
            return parameters
                .Where(p => !string.IsNullOrWhiteSpace(p) && p != wildcard)
                .Select(SanitizeParameter)
                .ToArray();
        }

        /// <summary>
        /// Ensures a file name is unique by adding a timestamp if the file already exists.
        /// </summary>
        /// <param name="basePath">Directory path where the file will be created</param>
        /// <param name="fileName">Proposed file name</param>
        /// <returns>Unique file name</returns>
        public static string EnsureUniqueFileName(string basePath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(fileName))
                return fileName;

            string fullPath = Path.Combine(basePath, fileName);
            
            if (!File.Exists(fullPath))
            {
                return fileName;
            }

            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            return $"{nameWithoutExt}_{timestamp}{extension}";
        }

        /// <summary>
        /// Builds the core file name from filtered parameters.
        /// </summary>
        /// <param name="parameters">Parameters to include in the file name</param>
        /// <param name="wildcard">Wildcard string to filter out</param>
        /// <param name="defaultCore">Default core name if no parameters are valid</param>
        /// <returns>Core file name string</returns>
        public static string BuildCoreFileName(string[] parameters, string wildcard = "%", string defaultCore = "ALL")
        {
            string[] filteredParts = FilterAndSanitizeParameters(parameters, wildcard);
            
            string core = string.Join("_", filteredParts);
            if (string.IsNullOrWhiteSpace(core))
            {
                core = defaultCore;
            }

            // Clean up any double underscores
            core = core.Replace("__", "_");
            
            return core;
        }
    }
}
