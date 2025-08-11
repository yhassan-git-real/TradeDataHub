using System;
using System.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using TradeDataHub.Core.Database;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace TradeDataHub.Core.Services
{
    public class DatabaseConnectionInfo
    {
        public string ServerName { get; set; } = "Unknown";
        public string DatabaseName { get; set; } = "Unknown";
        public string UserAccount { get; set; } = "Unknown";
        public string ConnectionStatus { get; set; } = "Disconnected";
        public string StatusColor { get; set; } = "#dc3545"; // Red by default
        public DateTime LastChecked { get; set; } = DateTime.Now;
    }

    public class DatabaseConnectionService : INotifyPropertyChanged
    {
        private static DatabaseConnectionService? _instance;
        private readonly Timer _connectionCheckTimer;
        private DatabaseConnectionInfo _connectionInfo;
        private readonly SharedDatabaseSettings _dbSettings;

        public static DatabaseConnectionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DatabaseConnectionService();
                }
                return _instance;
            }
        }

        private DatabaseConnectionService()
        {
            _connectionInfo = new DatabaseConnectionInfo();
            _dbSettings = LoadDatabaseSettings();
            
            // Parse connection string initially
            ParseConnectionString();
            
            // Start timer to check connection every 30 seconds
            _connectionCheckTimer = new Timer(CheckConnectionStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        public DatabaseConnectionInfo ConnectionInfo
        {
            get => _connectionInfo;
            private set
            {
                _connectionInfo = value;
                OnPropertyChanged();
            }
        }

        private SharedDatabaseSettings LoadDatabaseSettings()
        {
            try
            {
                const string json = "Config/database.appsettings.json";
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(json, false);
                var cfg = builder.Build();
                var root = cfg.Get<SharedDatabaseSettingsRoot>() 
                    ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
                return root.DatabaseConfig;
            }
            catch (Exception)
            {
                // Return default settings if config fails to load
                return new SharedDatabaseSettings
                {
                    ConnectionString = "Server=localhost;Database=Unknown;",
                    LogDirectory = "Logs"
                };
            }
        }

        private void ParseConnectionString()
        {
            try
            {
                var connectionStringBuilder = new SqlConnectionStringBuilder(_dbSettings.ConnectionString);
                
                var newInfo = new DatabaseConnectionInfo
                {
                    ServerName = connectionStringBuilder.DataSource ?? "Unknown",
                    DatabaseName = connectionStringBuilder.InitialCatalog ?? "Unknown",
                    UserAccount = !string.IsNullOrEmpty(connectionStringBuilder.UserID) 
                        ? connectionStringBuilder.UserID 
                        : connectionStringBuilder.IntegratedSecurity ? "Windows Auth" : "Unknown",
                    ConnectionStatus = "Checking...",
                    StatusColor = "#ffc107", // Yellow for checking
                    LastChecked = DateTime.Now
                };

                ConnectionInfo = newInfo;
            }
            catch (Exception)
            {
                ConnectionInfo = new DatabaseConnectionInfo
                {
                    ServerName = "Configuration Error",
                    DatabaseName = "Configuration Error",
                    UserAccount = "Configuration Error",
                    ConnectionStatus = "Config Error",
                    StatusColor = "#dc3545",
                    LastChecked = DateTime.Now
                };
            }
        }

        private async void CheckConnectionStatus(object? state)
        {
            await CheckConnectionStatusAsync();
        }

        public async Task CheckConnectionStatusAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbSettings.ConnectionString);
                await connection.OpenAsync();
                
                // Update status to connected
                var updatedInfo = new DatabaseConnectionInfo
                {
                    ServerName = ConnectionInfo.ServerName,
                    DatabaseName = ConnectionInfo.DatabaseName,
                    UserAccount = ConnectionInfo.UserAccount,
                    ConnectionStatus = "Connected",
                    StatusColor = "#28a745", // Green for connected
                    LastChecked = DateTime.Now
                };

                ConnectionInfo = updatedInfo;
            }
            catch (Exception)
            {
                // Update status to disconnected
                var updatedInfo = new DatabaseConnectionInfo
                {
                    ServerName = ConnectionInfo.ServerName,
                    DatabaseName = ConnectionInfo.DatabaseName,
                    UserAccount = ConnectionInfo.UserAccount,
                    ConnectionStatus = "Disconnected",
                    StatusColor = "#dc3545", // Red for disconnected
                    LastChecked = DateTime.Now
                };

                ConnectionInfo = updatedInfo;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _connectionCheckTimer?.Dispose();
        }
    }
}
