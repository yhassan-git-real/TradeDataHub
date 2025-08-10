using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeDataHub.Core.Services;

namespace TradeDataHub
{
    public partial class MainWindow : Window
    {
        #region Menu Event Handlers

        // File Menu Handlers
        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleNewCommand();
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleOpenCommand();
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleSaveCommand();
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleSaveAsCommand();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleExitCommand(this);
        }

        // Edit Menu Handlers
        private void MenuUndo_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleUndoCommand();
        }

        private void MenuRedo_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleRedoCommand();
        }

        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleCutCommand();
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleCopyCommand();
        }

        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandlePasteCommand();
        }

        private void MenuSelectAll_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleSelectAllCommand();
        }

        private void MenuPreferences_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandlePreferencesCommand();
        }

        // View Menu Handlers
        private void ToggleSwitch_Click(object sender, MouseButtonEventArgs e)
        {
            // Guard against calls during XAML initialization before services are ready
            if (_services?.MenuService == null) return;
            
            _services.MenuService.HandleToggleSwitchClick();
        }

        private void MenuBasicView_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleBasicViewCommand();
        }

        private void MenuAdvancedView_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleAdvancedViewCommand();
        }

        private void MenuMonitoringPanel_Click(object sender, RoutedEventArgs e)
        {
            // Guard against calls during XAML initialization before services are ready
            if (_services?.MenuService == null) return;
            
            _services.MenuService.HandleMonitoringPanelCommand(sender);
        }

        private void MenuActivityLog_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleActivityLogCommand();
        }

        private void MenuRefresh_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleRefreshCommand();
        }

        private void MenuFullScreen_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleFullScreenCommand(this);
        }

        // Help Menu Handlers
        private void MenuUserManual_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleUserManualCommand();
        }

        private void MenuQuickStart_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleQuickStartCommand();
        }

        private void MenuKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleKeyboardShortcutsCommand();
        }

        private void MenuOnlineHelp_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleOnlineHelpCommand();
        }

        private void MenuCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleCheckUpdatesCommand();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            _services.MenuService.HandleAboutCommand();
        }

        #endregion
    }
}
