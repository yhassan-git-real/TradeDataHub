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
    // Central AppSettings removed (modular configs now used directly by services)

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // EPPlus license context (non-commercial as per plan)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // No central configuration bootstrap needed; each module loads its own JSON.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}", "Startup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
