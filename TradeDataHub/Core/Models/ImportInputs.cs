using System.Collections.Generic;

namespace TradeDataHub.Core.Models
{
    /// <summary>
    /// DTO for import input parameters
    /// </summary>
    public record ImportInputs(
        string FromMonth, 
        string ToMonth, 
        List<string> Ports, 
        List<string> HSCodes, 
        List<string> Products,
        List<string> Importers, 
        List<string> IECs, 
        List<string> ForeignCountries, 
        List<string> ForeignNames);
}
