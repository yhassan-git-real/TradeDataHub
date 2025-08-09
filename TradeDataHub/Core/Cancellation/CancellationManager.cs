using System;
using System.Threading;
using System.Threading.Tasks;
using TradeDataHub.Core.Logging;

namespace TradeDataHub.Core.Cancellation
{
    /// <summary>
    /// Thread-safe cancellation manager for coordinating operation cancellation
    /// </summary>
    public class CancellationManager : ICancellationManager, IDisposable
    {
        private readonly object _lock = new object();
        private CancellationTokenSource? _currentTokenSource;
        private bool _isOperationActive;
        private bool _disposed;
        private readonly ModuleLogger _logger;

        public CancellationManager()
        {
            _logger = ModuleLoggerFactory.GetCancellationLogger();
        }

        /// <summary>
        /// Gets the current cancellation token, or CancellationToken.None if no operation is active
        /// </summary>
        public CancellationToken Token
        {
            get
            {
                lock (_lock)
                {
                    return _currentTokenSource?.Token ?? CancellationToken.None;
                }
            }
        }

        /// <summary>
        /// Gets whether cancellation is currently requested
        /// </summary>
        public bool IsCancellationRequested
        {
            get
            {
                lock (_lock)
                {
                    return _currentTokenSource?.IsCancellationRequested ?? false;
                }
            }
        }

        /// <summary>
        /// Event fired when cancellation is requested
        /// </summary>
        public event EventHandler<CancellationEventArgs>? CancellationRequested;

        /// <summary>
        /// Starts a new cancellation context for an operation
        /// </summary>
        public void StartOperation()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                if (_isOperationActive)
                {
                    throw new InvalidOperationException("Cannot start new operation while another is active. Call CompleteOperation() first.");
                }

                _currentTokenSource?.Dispose();
                _currentTokenSource = new CancellationTokenSource();
                _isOperationActive = true;

                var processId = _logger.GenerateProcessId();
                _logger.LogProcessStart("Cancellation Manager", "Operation started with cancellation support", processId);
            }
        }

        /// <summary>
        /// Cancels the current operation
        /// </summary>
        public void CancelOperation()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                if (!_isOperationActive || _currentTokenSource == null)
                {
                    _logger.LogWarning("Cancel requested but no active operation found");
                    return;
                }

                if (_currentTokenSource.IsCancellationRequested)
                {
                    _logger.LogWarning("Cancel requested but operation already cancelled");
                    return;
                }

                var processId = _logger.GenerateProcessId();
                _logger.LogProcessStart("Cancellation Request", "User requested operation cancellation", processId);

                try
                {
                    _currentTokenSource.Cancel();
                    
                    // Fire cancellation event
                    var args = new CancellationEventArgs("User requested cancellation");
                    CancellationRequested?.Invoke(this, args);

                    _logger.LogProcessComplete("Cancellation Request", TimeSpan.Zero, "Cancellation token activated", processId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error during cancellation request: {ex.Message}", ex, processId);
                    throw;
                }
            }
        }

        /// <summary>
        /// Completes the current operation and cleans up resources
        /// </summary>
        public void CompleteOperation()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                if (!_isOperationActive)
                {
                    return;
                }

                var processId = _logger.GenerateProcessId();
                
                try
                {
                    _currentTokenSource?.Dispose();
                    _currentTokenSource = null;
                    _isOperationActive = false;

                    _logger.LogProcessComplete("Cancellation Manager", TimeSpan.Zero, "Operation completed and resources cleaned up", processId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error during operation completion: {ex.Message}", ex, processId);
                    throw;
                }
            }
        }

        /// <summary>
        /// Throws OperationCanceledException if cancellation is requested
        /// </summary>
        public void ThrowIfCancellationRequested()
        {
            lock (_lock)
            {
                _currentTokenSource?.Token.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Disposes the cancellation manager and cleans up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lock)
                {
                    _currentTokenSource?.Dispose();
                    _currentTokenSource = null;
                    _isOperationActive = false;
                    _disposed = true;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CancellationManager));
            }
        }
    }
}
