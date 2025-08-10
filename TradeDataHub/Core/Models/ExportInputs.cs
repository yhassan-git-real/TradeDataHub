using System.Collections.Generic;

namespace TradeDataHub.Core.Models
{
    /// <summary>
    /// DTO for export input parameters
    /// </summary>
    public record ExportInputs(
        string FromMonth, 
        string ToMonth, 
        List<string> Ports, 
        List<string> HSCodes, 
        List<string> Products,
        List<string> Exporters, 
        List<string> IECs, 
        List<string> ForeignCountries, 
        List<string> ForeignNames);
}
