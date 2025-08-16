using System.Collections.Generic;

namespace TradeDataHub.Config
{
    public class ExcelFormatSettings
    {
        public required string FontName { get; set; }
        public required int FontSize { get; set; }
        public required string HeaderBackgroundColor { get; set; }
        public required string BorderStyle { get; set; }
        public required int AutoFitSampleRows { get; set; }
        public required string DateFormat { get; set; }
        public required List<int> DateColumns { get; set; }
        public required List<int> TextColumns { get; set; }
        public required bool WrapText { get; set; }
        public required bool AutoFitColumns { get; set; }
    }
}
