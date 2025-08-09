using System;

namespace TradeDataHub.Features.Monitoring.Models
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug,
        Success
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string Details { get; set; }

        public string LevelColor
        {
            get
            {
                return Level switch
                {
                    LogLevel.Info => "#17A2B8",    // Info blue
                    LogLevel.Warning => "#FFC107", // Warning yellow
                    LogLevel.Error => "#DC3545",   // Error red
                    LogLevel.Debug => "#6C757D",   // Debug gray
                    LogLevel.Success => "#28A745", // Success green
                    _ => "#6C757D"
                };
            }
        }

        public string LevelText
        {
            get
            {
                return Level switch
                {
                    LogLevel.Info => "INFO",
                    LogLevel.Warning => "WARN",
                    LogLevel.Error => "ERROR",
                    LogLevel.Debug => "DEBUG",
                    LogLevel.Success => "SUCCESS",
                    _ => "UNKNOWN"
                };
            }
        }

        public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");

        public LogEntry()
        {
            Timestamp = DateTime.Now;
            Level = LogLevel.Info;
            Message = string.Empty;
            Source = string.Empty;
            Details = string.Empty;
        }

        public LogEntry(LogLevel level, string message, string source = "", string details = "")
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
            Source = source;
            Details = details;
        }
    }
}
