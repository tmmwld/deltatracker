using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DeltaForceTracker.Hotkeys
{
    public class F8Hotkey : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _windowHandle;
        private HwndSource? _source;

        public event EventHandler? HotkeyPressed;

        public F8Hotkey(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.Handle;
        }

        public bool Register()
        {
            // VK_F8 = 0x77, MOD_NOREPEAT = 0x4000
            if (RegisterHotKey(_windowHandle, HOTKEY_ID, 0x4000, 0x77))
            {
                _source = HwndSource.FromHwnd(_windowHandle);
                _source?.AddHook(WndProc);
                return true;
            }
            return false;
        }

        public void Unregister()
        {
            _source?.RemoveHook(WndProc);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
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
    }
}
