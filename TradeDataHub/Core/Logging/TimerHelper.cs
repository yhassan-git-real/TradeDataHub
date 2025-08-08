using System;
using System.Diagnostics;

namespace TradeDataHub.Core.Logging
{
    /// <summary>
    /// Timer helper class that provides disposable timing functionality for module loggers
    /// </summary>
    public class TimerHelper : IDisposable
    {
        private readonly string _operationName;
        private readonly ModuleLogger _logger;
        private readonly Stopwatch _stopwatch;
        private readonly string _processId;
        private bool _disposed = false;

        internal TimerHelper(string operationName, string processId, ModuleLogger logger)
        {
            _operationName = operationName;
            _logger = logger;
            _processId = processId ?? "";
            _stopwatch = Stopwatch.StartNew();
            _logger.LogStep($"{_operationName}", "Started", _processId);
        }

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public void Dispose()
        {
            if (_disposed) return;
            
            _stopwatch.Stop();
            _logger.LogStep($"{_operationName}", $"Completed in {_stopwatch.Elapsed:mm\\:ss\\.fff}", _processId);
            _disposed = true;
        }
    }
}
