using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;

namespace DeltaForceTracker.Hotkeys
{
    public class GlobalHotkey : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_NOREPEAT = 0x4000; // Prevents auto-repeat on Windows 7+
        private readonly int _hotkeyId;
        private IntPtr _windowHandle;
        private HwndSource? _source;
        private bool _isRegistered = false;

        public event EventHandler? HotkeyPressed;

        public Keys CurrentKey { get; private set; }
        public ModifierKeys CurrentModifiers { get; private set; }

        public GlobalHotkey(IntPtr windowHandle, Keys key, ModifierKeys modifiers = ModifierKeys.None)
        {
            _windowHandle = windowHandle;
            _hotkeyId = GetHashCode();
            CurrentKey = key;
            CurrentModifiers = modifiers;
        }

        public bool Register()
        {
            if (_isRegistered)
                return true;

            _source = HwndSource.FromHwnd(_windowHandle);
            if (_source == null)
                return false;

            _source.AddHook(WndProc);

            // Combine user modifiers with MOD_NOREPEAT for reliable global hotkey
            uint modifiers = (uint)CurrentModifiers | MOD_NOREPEAT;
            uint vk = (uint)CurrentKey;

            _isRegistered = RegisterHotKey(_windowHandle, _hotkeyId, modifiers, vk);
            return _isRegistered;
        }

        public void Unregister()
        {
            if (!_isRegistered)
                return;

            UnregisterHotKey(_windowHandle, _hotkeyId);
            
            if (_source != null)
            {
                _source.RemoveHook(WndProc);
                _source = null;
            }

            _isRegistered = false;
        }

        public bool UpdateHotkey(Keys newKey, ModifierKeys newModifiers = ModifierKeys.None)
        {
            Unregister();
            CurrentKey = newKey;
            CurrentModifiers = newModifiers;
            return Register();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
        }

        public static Keys ParseKeyString(string keyString)
        {
            if (Enum.TryParse<Keys>(keyString, true, out var key))
                return key;
            
            return Keys.F8; // Default
        }

        public static string GetKeyDisplayName(Keys key)
        {
            return key.ToString();
        }
    }

    [Flags]
    public enum ModifierKeys : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}
