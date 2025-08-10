using System.Threading;
using System.Threading.Tasks;
using TradeDataHub.Core.Models;

namespace TradeDataHub.Core.Controllers
{
    /// <summary>
    /// Interface for export controller operations
    /// </summary>
    public interface IExportController
    {
        /// <summary>
        /// Runs the export process asynchronously
        /// </summary>
        /// <param name="exportInputs">The export input parameters</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <param name="selectedView">The selected database view name</param>
        /// <param name="selectedStoredProcedure">The selected stored procedure name</param>
        /// <returns>Task representing the async operation</returns>
        Task RunAsync(ExportInputs exportInputs, CancellationToken cancellationToken, string selectedView, string selectedStoredProcedure);
    }
}
