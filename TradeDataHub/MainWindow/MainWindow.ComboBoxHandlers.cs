using System;
using System.Windows;
using System.Windows.Controls;
using TradeDataHub.Core.Models;

namespace TradeDataHub
{
    public partial class MainWindow : Window
    {
        #region ComboBox Event Handlers

        private void ViewComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is DbObjectOption selectedView)
            {
                var rbExport = this.FindName("rbExport") as System.Windows.Controls.RadioButton;
                bool isExportMode = rbExport?.IsChecked == true;
                _services.UIService.HandleViewSelectionChanged(selectedView, isExportMode, _services.ExportDbObjectViewModel, _services.ImportDbObjectViewModel);
            }
        }
        
        private void StoredProcedureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is DbObjectOption selectedSP)
            {
                var rbExport = this.FindName("rbExport") as System.Windows.Controls.RadioButton;
                bool isExportMode = rbExport?.IsChecked == true;
                _services.UIService.HandleStoredProcedureSelectionChanged(selectedSP, isExportMode, _services.ExportDbObjectViewModel, _services.ImportDbObjectViewModel);
            }
        }

        #endregion
    }
}
