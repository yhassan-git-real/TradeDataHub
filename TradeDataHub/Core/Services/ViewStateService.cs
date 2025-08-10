using System;
using System.Windows;
using System.Windows.Media;
using TradeDataHub.Features.Monitoring.Services;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for managing view states and UI mode switching
    /// </summary>
    public interface IViewStateService
    {
        void SwitchToBasicMode();
        void SwitchToAdvancedMode();
        void ToggleViewMode();
        void SetMonitoringPanelVisibility(bool isVisible);
        void Initialize(Window window);
    }

    public class ViewStateService : IViewStateService
    {
        private readonly MonitoringService _monitoringService;
        private Window? _mainWindow;
        
        // UI Controls - accessed via FindName
        private System.Windows.Controls.Grid? _advancedParametersGrid;
        private System.Windows.Controls.Grid? _monitoringPanel;
        private System.Windows.Controls.MenuItem? _menuBasicView;
        private System.Windows.Controls.MenuItem? _menuAdvancedView;
        private System.Windows.Controls.MenuItem? _menuMonitoringPanel;
        private System.Windows.Shapes.Rectangle? _activeIndicator;
        private System.Windows.Controls.TextBlock? _basicText;
        private System.Windows.Shapes.Path? _basicIcon;
        private System.Windows.Controls.TextBlock? _allText;
        private System.Windows.Shapes.Path? _allIcon;

        public ViewStateService(MonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        public void Initialize(Window window)
        {
            _mainWindow = window;
            
            // Get UI control references
            _advancedParametersGrid = window.FindName("AdvancedParametersGrid") as System.Windows.Controls.Grid;
            _monitoringPanel = window.FindName("MonitoringPanel") as System.Windows.Controls.Grid;
            _menuBasicView = window.FindName("MenuBasicView") as System.Windows.Controls.MenuItem;
            _menuAdvancedView = window.FindName("MenuAdvancedView") as System.Windows.Controls.MenuItem;
            _menuMonitoringPanel = window.FindName("MenuMonitoringPanel") as System.Windows.Controls.MenuItem;
            _activeIndicator = window.FindName("ActiveIndicator") as System.Windows.Shapes.Rectangle;
            _basicText = window.FindName("BasicText") as System.Windows.Controls.TextBlock;
            _basicIcon = window.FindName("BasicIcon") as System.Windows.Shapes.Path;
            _allText = window.FindName("AllText") as System.Windows.Controls.TextBlock;
            _allIcon = window.FindName("AllIcon") as System.Windows.Shapes.Path;
        }

        public void SwitchToBasicMode()
        {
            try
            {
                // Hide additional parameters
                if (_advancedParametersGrid != null)
                    _advancedParametersGrid.Visibility = Visibility.Collapsed;
                
                // Move active indicator to left (Basic mode)
                if (_activeIndicator != null)
                    _activeIndicator.SetValue(System.Windows.Controls.Grid.ColumnProperty, 0);
                
                // Update colors for Basic mode
                if (_basicText != null) _basicText.Foreground = new SolidColorBrush(Colors.White);
                if (_basicIcon != null) _basicIcon.Fill = new SolidColorBrush(Colors.White);
                if (_allText != null) _allText.Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)); // #6C757D
                if (_allIcon != null) _allIcon.Fill = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                
                // Update menu checkboxes
                if (_menuBasicView != null) _menuBasicView.IsChecked = true;
                if (_menuAdvancedView != null) _menuAdvancedView.IsChecked = false;

                _monitoringService.AddLog(Features.Monitoring.Models.LogLevel.Info, "Switched to Basic view mode", "ViewState");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching to basic mode: {ex.Message}", "View Mode Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SwitchToAdvancedMode()
        {
            try
            {
                // Show all parameters
                if (_advancedParametersGrid != null)
                    _advancedParametersGrid.Visibility = Visibility.Visible;
                
                // Move active indicator to right (All mode)
                if (_activeIndicator != null)
                    _activeIndicator.SetValue(System.Windows.Controls.Grid.ColumnProperty, 1);
                
                // Update colors for All mode
                if (_basicText != null) _basicText.Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)); // #6C757D
                if (_basicIcon != null) _basicIcon.Fill = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                if (_allText != null) _allText.Foreground = new SolidColorBrush(Colors.White);
                if (_allIcon != null) _allIcon.Fill = new SolidColorBrush(Colors.White);
                
                // Update menu checkboxes
                if (_menuBasicView != null) _menuBasicView.IsChecked = false;
                if (_menuAdvancedView != null) _menuAdvancedView.IsChecked = true;

                _monitoringService.AddLog(Features.Monitoring.Models.LogLevel.Info, "Switched to Advanced view mode", "ViewState");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching to advanced mode: {ex.Message}", "View Mode Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ToggleViewMode()
        {
            // Check current state based on AdvancedParametersGrid visibility
            bool isBasicMode = _advancedParametersGrid?.Visibility == Visibility.Collapsed;
            
            if (isBasicMode)
            {
                SwitchToAdvancedMode();
            }
            else
            {
                SwitchToBasicMode();
            }
        }

        public void SetMonitoringPanelVisibility(bool isVisible)
        {
            try
            {
                if (_monitoringPanel != null)
                {
                    _monitoringPanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                }

                if (_menuMonitoringPanel != null)
                {
                    _menuMonitoringPanel.IsChecked = isVisible;
                }

                _monitoringService.AddLog(Features.Monitoring.Models.LogLevel.Info, $"Monitoring panel {(isVisible ? "shown" : "hidden")}", "ViewState");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting monitoring panel visibility: {ex.Message}", "View State Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
