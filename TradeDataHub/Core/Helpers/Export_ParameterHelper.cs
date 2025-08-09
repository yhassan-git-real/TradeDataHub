using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeDataHub.Core.Helpers
{
    public static class Export_ParameterHelper
    {
        // Parameter Constants
        public const string WILDCARD = "%";
        public const string DEFAULT_DATE_FORMAT = "yyyyMM";
        public const int MIN_DATE_VALUE = 190001;
        public const int MAX_DATE_VALUE = 299912;
        public const int MAX_EXCEL_ROWS = 1048575;

        // Export Operation Parameters
        public static class ExportParameters
        {
            public const string FROM_MONTH = "fromMonth";
            public const string TO_MONTH = "toMonth";
            public const string HS_CODE = "hsCode";
            public const string PRODUCT = "product";
            public const string IEC = "iec";
            public const string EXPORTER = "exporter";
            public const string FOREIGN_COUNTRY = "foreignCountry";
            public const string FOREIGN_NAME = "foreignName";
            public const string PORT = "port";

            public static readonly string[] ALL_PARAMETERS = {
                FROM_MONTH, TO_MONTH, HS_CODE, PRODUCT, IEC, EXPORTER, FOREIGN_COUNTRY, FOREIGN_NAME, PORT
            };
        }

        // Stored Procedure Parameter Names
        public static class StoredProcedureParameters
        {
            public const string SP_FROM_MONTH = "@fromMonth";
            public const string SP_TO_MONTH = "@ToMonth";
            public const string SP_HS_CODE = "@hs";
            public const string SP_PRODUCT = "@prod";
            public const string SP_IEC = "@Iec";
            public const string SP_EXPORTER = "@ExpCmp";
            public const string SP_FOREIGN_COUNTRY = "@forcount";
            public const string SP_FOREIGN_NAME = "@forname";
            public const string SP_PORT = "@port";

            public static readonly string[] ALL_SP_PARAMETERS = {
                SP_FROM_MONTH, SP_TO_MONTH, SP_HS_CODE, SP_PRODUCT, SP_IEC, SP_EXPORTER, SP_FOREIGN_COUNTRY, SP_FOREIGN_NAME, SP_PORT
            };
        }


        // Validation Methods
        public static bool IsValidDateFormat(string dateString)
        {
            return dateString.Length == 6 && 
                   int.TryParse(dateString, out int date) &&
                   date >= MIN_DATE_VALUE && date <= MAX_DATE_VALUE;
        }

        public static bool IsValidDateRange(string fromMonth, string toMonth)
        {
            if (!IsValidDateFormat(fromMonth) || !IsValidDateFormat(toMonth))
                return false;

            int fromDate = int.Parse(fromMonth);
            int toDate = int.Parse(toMonth);
            
            return fromDate <= toDate;
        }

        public static bool IsWildcard(string parameter)
        {
            return string.IsNullOrWhiteSpace(parameter) || parameter.Trim() == WILDCARD;
        }

        // Parameter Processing Methods
    public static List<string> ParseFilterList(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new List<string> { WILDCARD };
            }

            return rawText
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

    public static string NormalizeParameter(string parameter)
        {
            return string.IsNullOrWhiteSpace(parameter) ? WILDCARD : parameter.Trim();
        }

    public static Dictionary<string, string> CreateExportParameterSet(
            string fromMonth, string toMonth, string hsCode, string product, 
            string iec, string exporter, string foreignCountry, string foreignName, string port)
        {
            return new Dictionary<string, string>
            {
                { ExportParameters.FROM_MONTH, NormalizeParameter(fromMonth) },
                { ExportParameters.TO_MONTH, NormalizeParameter(toMonth) },
                { ExportParameters.HS_CODE, NormalizeParameter(hsCode) },
                { ExportParameters.PRODUCT, NormalizeParameter(product) },
                { ExportParameters.IEC, NormalizeParameter(iec) },
                { ExportParameters.EXPORTER, NormalizeParameter(exporter) },
                { ExportParameters.FOREIGN_COUNTRY, NormalizeParameter(foreignCountry) },
                { ExportParameters.FOREIGN_NAME, NormalizeParameter(foreignName) },
                { ExportParameters.PORT, NormalizeParameter(port) }
            };
        }

        // Logging and Key Generation
    public static string GenerateParameterKey(params string[] parameters)
        {
            return string.Join("|", parameters.Select(p => p ?? WILDCARD));
        }

    public static string GenerateExportParameterKey(
            string fromMonth, string toMonth, string hsCode, string product, 
            string iec, string exporter, string foreignCountry, string foreignName, string port)
        {
            return GenerateParameterKey(fromMonth, toMonth, hsCode, product, iec, exporter, foreignCountry, foreignName, port);
        }

    public static string FormatParametersForDisplay(Dictionary<string, string> parameters)
        {
            return string.Join(", ", parameters.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

    public static string FormatStoredProcedureParameters(
            string fromMonth, string toMonth, string hsCode, string product, 
            string iec, string exporter, string foreignCountry, string foreignName, string port)
        {
            return $"{StoredProcedureParameters.SP_FROM_MONTH}: {fromMonth}, {StoredProcedureParameters.SP_TO_MONTH}: {toMonth}, {StoredProcedureParameters.SP_HS_CODE}: {hsCode}, {StoredProcedureParameters.SP_PRODUCT}: {product}, {StoredProcedureParameters.SP_IEC}: {iec}, {StoredProcedureParameters.SP_EXPORTER}: {exporter}, {StoredProcedureParameters.SP_FOREIGN_COUNTRY}: {foreignCountry}, {StoredProcedureParameters.SP_FOREIGN_NAME}: {foreignName}, {StoredProcedureParameters.SP_PORT}: {port}";
        }

        // Validation Results
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public Dictionary<string, string> NormalizedParameters { get; set; } = new Dictionary<string, string>();
        }

    public static ValidationResult ValidateExportParameters(
            string fromMonth, string toMonth, string hsCode, string product, 
            string iec, string exporter, string foreignCountry, string foreignName, string port)
        {
            var result = new ValidationResult();

            // Date validation
            if (!IsValidDateFormat(fromMonth))
                result.Errors.Add($"Invalid fromMonth format: {fromMonth}. Expected YYYYMM.");

            if (!IsValidDateFormat(toMonth))
                result.Errors.Add($"Invalid toMonth format: {toMonth}. Expected YYYYMM.");

            if (IsValidDateFormat(fromMonth) && IsValidDateFormat(toMonth) && !IsValidDateRange(fromMonth, toMonth))
                result.Errors.Add($"Invalid date range: fromMonth ({fromMonth}) must be <= toMonth ({toMonth}).");

            // Create normalized parameters regardless of validation status
            result.NormalizedParameters = CreateExportParameterSet(
                fromMonth, toMonth, hsCode, product, iec, exporter, foreignCountry, foreignName, port);

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

    }
}
