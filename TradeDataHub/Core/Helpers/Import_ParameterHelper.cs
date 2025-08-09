using System.Collections.Generic;

namespace TradeDataHub.Core.Helpers
{
    public static class Import_ParameterHelper
    {
        public static class ImportParameters
        {
            public const string FROM_MONTH = "fromMonth";
            public const string TO_MONTH = "toMonth";
            public const string HS_CODE = "hsCode";
            public const string PRODUCT = "product";
            public const string IEC = "iec";
            public const string IMPORTER = "importer";
            public const string FOREIGN_COUNTRY = "foreignCountry";
            public const string FOREIGN_NAME = "foreignName";
            public const string PORT = "port";
        }

        public static class StoredProcedureParameters
        {
            public const string SP_FROM_MONTH = "@fromMonth";
            public const string SP_TO_MONTH = "@ToMonth";
            public const string SP_HS_CODE = "@hs";
            public const string SP_PRODUCT = "@prod";
            public const string SP_IEC = "@Iec";
            public const string SP_IMPORTER = "@ImpCmp"; // matches stored procedure expectation
            public const string SP_FOREIGN_COUNTRY = "@forcount";
            public const string SP_FOREIGN_NAME = "@forname";
            public const string SP_PORT = "@port";
        }

        public static Dictionary<string,string> CreateImportParameterSet(string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string foreignCountry, string foreignName, string port)
        {
            return new Dictionary<string, string>
            {
                { ImportParameters.FROM_MONTH, Export_ParameterHelper.NormalizeParameter(fromMonth) },
                { ImportParameters.TO_MONTH, Export_ParameterHelper.NormalizeParameter(toMonth) },
                { ImportParameters.HS_CODE, Export_ParameterHelper.NormalizeParameter(hsCode) },
                { ImportParameters.PRODUCT, Export_ParameterHelper.NormalizeParameter(product) },
                { ImportParameters.IEC, Export_ParameterHelper.NormalizeParameter(iec) },
                { ImportParameters.IMPORTER, Export_ParameterHelper.NormalizeParameter(importer) },
                { ImportParameters.FOREIGN_COUNTRY, Export_ParameterHelper.NormalizeParameter(foreignCountry) },
                { ImportParameters.FOREIGN_NAME, Export_ParameterHelper.NormalizeParameter(foreignName) },
                { ImportParameters.PORT, Export_ParameterHelper.NormalizeParameter(port) },
            };
        }
    }
}
