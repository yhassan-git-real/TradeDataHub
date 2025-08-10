using System;
using System.Windows;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Interface for menu service operations
    /// </summary>
    public interface IMenuService
    {
        // File Menu Operations
        void HandleNewCommand();
        void HandleOpenCommand();
        void HandleSaveCommand();
        void HandleSaveAsCommand();
        void HandleExitCommand(Window window);

        // Edit Menu Operations
        void HandleUndoCommand();
        void HandleRedoCommand();
        void HandleCutCommand();
        void HandleCopyCommand();
        void HandlePasteCommand();
        void HandleSelectAllCommand();
        void HandlePreferencesCommand();

        // View Menu Operations
        void HandleBasicViewCommand();
        void HandleAdvancedViewCommand();
        void HandleToggleSwitchClick();
        void HandleMonitoringPanelCommand(object sender);
        void HandleActivityLogCommand();
        void HandleRefreshCommand();
        void HandleFullScreenCommand(Window window);

        // Help Menu Operations
        void HandleUserManualCommand();
        void HandleQuickStartCommand();
        void HandleKeyboardShortcutsCommand();
        void HandleOnlineHelpCommand();
        void HandleCheckUpdatesCommand();
        void HandleAboutCommand();

        // Initialize with required dependencies
        void Initialize(Window window);
    }
}
