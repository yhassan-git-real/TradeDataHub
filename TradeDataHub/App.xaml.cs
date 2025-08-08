using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Windows;
using TradeDataHub.Config;
using OfficeOpenXml; // EPPlus license context

namespace TradeDataHub
{
    public partial class App : Application
    {
        public static AppSettings Settings { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // EPPlus license context (non-commercial as per plan)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true);

                IConfiguration configuration = builder.Build();

                Settings = configuration.GetSection("AppSettings").Get<AppSettings>() ?? 
                          throw new InvalidOperationException("AppSettings could not be loaded from appsettings.json. Please check the file.");
            }
            catch (Exception ex)
            {
                // On configuration error, it's often best to fail fast.
                MessageBox.Show($"A critical error occurred while loading configuration: {ex.Message}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // This will terminate the application if the configuration is missing or invalid.
                Application.Current.Shutdown();
            }
        }
    }
}
