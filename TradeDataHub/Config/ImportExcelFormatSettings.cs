namespace TradeDataHub.Features.Import
{
    public class ImportExcelFormatSettings
    {
        public string FontName { get; set; } = "Calibri";
        public int FontSize { get; set; } = 10;
        public string HeaderBackgroundColor { get; set; } = "#D9E1F2";
        public string BorderStyle { get; set; } = "thin"; // thin or none
        public string DateFormat { get; set; } = "dd-MMM-yy";
        public int[] DateColumns { get; set; } = new int[0];
        public int[] TextColumns { get; set; } = new int[0];
        public bool WrapText { get; set; } = false;
        public bool AutoFitColumns { get; set; } = true;
        public int AutoFitSampleRows { get; set; } = 1000;
    }
}
