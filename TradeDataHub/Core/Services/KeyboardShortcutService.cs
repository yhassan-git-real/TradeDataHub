using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for centralized keyboard shortcut management
    /// </summary>
    public class KeyboardShortcutService : IKeyboardShortcutService
    {
        private readonly IMenuService _menuService;
        private readonly Dictionary<ShortcutKey, Action> _shortcuts;

        public KeyboardShortcutService(IMenuService menuService)
        {
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _shortcuts = new Dictionary<ShortcutKey, Action>();
            InitializeShortcuts();
        }

        private void InitializeShortcuts()
        {
            // Function keys
            _shortcuts[new ShortcutKey(Key.F1)] = _menuService.HandleUserManualCommand;
            _shortcuts[new ShortcutKey(Key.F5)] = _menuService.HandleRefreshCommand;
            _shortcuts[new ShortcutKey(Key.F11)] = () => _menuService.HandleFullScreenCommand(Application.Current.MainWindow);

            // File operations - Ctrl combinations
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.N)] = _menuService.HandleNewCommand;
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.O)] = _menuService.HandleOpenCommand;
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.S)] = _menuService.HandleSaveCommand;
            _shortcuts[new ShortcutKey(ModifierKeys.Control | ModifierKeys.Shift, Key.S)] = _menuService.HandleSaveAsCommand;

            // Edit operations - Ctrl combinations
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.Z)] = _menuService.HandleUndoCommand;
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.Y)] = _menuService.HandleRedoCommand;
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.X)] = _menuService.HandleCutCommand;
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.C)] = _menuService.HandleCopyCommand;
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.V)] = _menuService.HandlePasteCommand;
            _shortcuts[new ShortcutKey(ModifierKeys.Control, Key.A)] = _menuService.HandleSelectAllCommand;

            // Alt combinations
            _shortcuts[new ShortcutKey(ModifierKeys.Alt, Key.F4)] = () => _menuService.HandleExitCommand(Application.Current.MainWindow);
        }

        public bool HandleKeyDown(KeyEventArgs e)
        {
            if (e == null) return false;

            var shortcutKey = new ShortcutKey(Keyboard.Modifiers, e.Key);
            
            if (_shortcuts.TryGetValue(shortcutKey, out var action))
            {
                try
                {
                    action.Invoke();
                    e.Handled = true;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        public void RegisterShortcut(ModifierKeys modifiers, Key key, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var shortcutKey = new ShortcutKey(modifiers, key);
            _shortcuts[shortcutKey] = action;
        }

        public void UnregisterShortcut(ModifierKeys modifiers, Key key)
        {
            var shortcutKey = new ShortcutKey(modifiers, key);
            _shortcuts.Remove(shortcutKey);
        }

        public bool IsShortcutRegistered(ModifierKeys modifiers, Key key)
        {
            var shortcutKey = new ShortcutKey(modifiers, key);
            return _shortcuts.ContainsKey(shortcutKey);
        }

        public IEnumerable<(ModifierKeys Modifiers, Key Key)> GetRegisteredShortcuts()
        {
            return _shortcuts.Keys.Select(sk => (sk.Modifiers, sk.Key));
        }
    }

    /// <summary>
    /// Represents a keyboard shortcut key combination
    /// </summary>
    public struct ShortcutKey : IEquatable<ShortcutKey>
    {
        public ModifierKeys Modifiers { get; }
        public Key Key { get; }

        public ShortcutKey(Key key) : this(ModifierKeys.None, key)
        {
        }

        public ShortcutKey(ModifierKeys modifiers, Key key)
        {
            Modifiers = modifiers;
            Key = key;
        }

        public bool Equals(ShortcutKey other)
        {
            return Modifiers == other.Modifiers && Key == other.Key;
        }

        public override bool Equals(object? obj)
        {
            return obj is ShortcutKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Modifiers, Key);
        }

        public static bool operator ==(ShortcutKey left, ShortcutKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShortcutKey left, ShortcutKey right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            var result = string.Empty;

            if (Modifiers.HasFlag(ModifierKeys.Control))
                result += "Ctrl+";
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                result += "Alt+";
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                result += "Shift+";
            if (Modifiers.HasFlag(ModifierKeys.Windows))
                result += "Win+";

            result += Key.ToString();
            return result;
        }
    }
}
