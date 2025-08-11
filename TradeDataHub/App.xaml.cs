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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // EPPlus license context (non-commercial as per plan)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}", "Startup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
