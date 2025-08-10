using System;
using System.Windows;
using System.Windows.Input;

namespace TradeDataHub
{
    public partial class MainWindow : Window
    {
        #region Keyboard Event Handlers

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Guard against calls during initialization before services are ready
            if (_services?.KeyboardShortcutService == null) return;
            
            // Let the keyboard shortcut service handle the key event
            _services.KeyboardShortcutService.HandleKeyDown(e);
        }

        #endregion
    }
}
