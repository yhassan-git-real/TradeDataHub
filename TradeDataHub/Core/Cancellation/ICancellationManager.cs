using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradeDataHub.Core.Cancellation
{
    /// <summary>
    /// Interface for managing cancellation operations across the application
    /// </summary>
    public interface ICancellationManager
    {
        /// <summary>
        /// Gets the current cancellation token
        /// </summary>
        CancellationToken Token { get; }

        /// <summary>
        /// Gets whether cancellation is currently requested
        /// </summary>
        bool IsCancellationRequested { get; }

        /// <summary>
        /// Starts a new cancellation context for an operation
        /// </summary>
        void StartOperation();

        /// <summary>
        /// Cancels the current operation
        /// </summary>
        void CancelOperation();

        /// <summary>
        /// Completes the current operation and cleans up resources
        /// </summary>
        void CompleteOperation();

        /// <summary>
        /// Throws OperationCanceledException if cancellation is requested
        /// </summary>
        void ThrowIfCancellationRequested();

        /// <summary>
        /// Event fired when cancellation is requested
        /// </summary>
        event EventHandler<CancellationEventArgs> CancellationRequested;
    }

    /// <summary>
    /// Event arguments for cancellation events
    /// </summary>
    public class CancellationEventArgs : EventArgs
    {
        public string Reason { get; }
        public DateTime RequestedAt { get; }

        public CancellationEventArgs(string reason)
        {
            Reason = reason;
            RequestedAt = DateTime.Now;
        }
    }
}
