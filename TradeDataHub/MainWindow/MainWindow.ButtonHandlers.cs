using System;
using System.Windows;
using MonitoringLogLevel = TradeDataHub.Features.Monitoring.Models.LogLevel;

namespace TradeDataHub
{
    public partial class MainWindow : Window
    {
        #region Button Event Handlers

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _services.UIActionService.HandleGenerateAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _services.MonitoringService.AddLog(MonitoringLogLevel.Error, $"Unexpected error in Generate button: {ex.Message}", "GenerateButton");
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _services.UIActionService.HandleCancel();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _services.UIActionService.HandleReset();
        }

        #endregion
    }
}
