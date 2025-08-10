using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Interface for keyboard shortcut service operations
    /// </summary>
    public interface IKeyboardShortcutService
    {
        /// <summary>
        /// Handles a keyboard key down event and executes associated shortcut if found
        /// </summary>
        /// <param name="e">The KeyEventArgs from the key down event</param>
        /// <returns>True if a shortcut was handled, false otherwise</returns>
        bool HandleKeyDown(KeyEventArgs e);

        /// <summary>
        /// Registers a new keyboard shortcut
        /// </summary>
        /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift, etc.)</param>
        /// <param name="key">The key to bind</param>
        /// <param name="action">The action to execute when the shortcut is pressed</param>
        void RegisterShortcut(ModifierKeys modifiers, Key key, Action action);

        /// <summary>
        /// Unregisters an existing keyboard shortcut
        /// </summary>
        /// <param name="modifiers">The modifier keys</param>
        /// <param name="key">The key to unbind</param>
        void UnregisterShortcut(ModifierKeys modifiers, Key key);

        /// <summary>
        /// Checks if a keyboard shortcut is currently registered
        /// </summary>
        /// <param name="modifiers">The modifier keys</param>
        /// <param name="key">The key to check</param>
        /// <returns>True if the shortcut is registered, false otherwise</returns>
        bool IsShortcutRegistered(ModifierKeys modifiers, Key key);

        /// <summary>
        /// Gets all currently registered shortcuts
        /// </summary>
        /// <returns>Collection of registered shortcut keys</returns>
        IEnumerable<(ModifierKeys Modifiers, Key Key)> GetRegisteredShortcuts();
    }
}
