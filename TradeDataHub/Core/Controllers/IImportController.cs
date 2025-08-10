using System.Threading;
using System.Threading.Tasks;
using TradeDataHub.Core.Models;

namespace TradeDataHub.Core.Controllers
{
    /// <summary>
    /// Interface for import controller operations
    /// </summary>
    public interface IImportController
    {
        Task RunAsync(ImportInputs importInputs, CancellationToken cancellationToken, string selectedView, string selectedStoredProcedure);
    }
}
