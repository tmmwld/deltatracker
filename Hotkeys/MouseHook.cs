using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace DeltaForceTracker.Hotkeys
{
    public class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int TRIPLE_CLICK_WINDOW_MS = 500; // Max time for 3 clicks

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelMouseProc _proc;
        private MouseButton _targetButton;

        // Triple-click detection
        private int _clickCount = 0;
        private DateTime _firstClickTime = DateTime.MinValue;
        private DispatcherTimer? _resetTimer;

        public event EventHandler? HotkeyPressed;

        public MouseHook(string buttonName)
        {
            _targetButton = buttonName == "Mouse5" ? MouseButton.XButton2 : MouseButton.XButton1;
            _proc = HookCallback;

            // Timer to reset click count
            _resetTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TRIPLE_CLICK_WINDOW_MS)
            };
            _resetTimer.Tick += (s, e) =>
            {
                _clickCount = 0;
                _resetTimer?.Stop();
            };
        }

        public bool Register()
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;

            if (curModule == null) return false;

            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);

            Debug.WriteLine($"MouseHook registered: {_hookID != IntPtr.Zero}");
            return _hookID != IntPtr.Zero;
        }

        public void Unregister()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                _resetTimer?.Stop();
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_XBUTTONDOWN)
            {
                try
                {
                    var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var xButton = (info.mouseData >> 16) & 0xFFFF;

                    // Check if it's our target button (XBUTTON1 = 1, XBUTTON2 = 2)
                    bool isTargetButton =
                        (_targetButton == MouseButton.XButton1 && xButton == 1) ||
                        (_targetButton == MouseButton.XButton2 && xButton == 2);

                    if (isTargetButton)
                    {
                        var now = DateTime.Now;

                        // Start new sequence if timeout expired
                        if ((now - _firstClickTime).TotalMilliseconds > TRIPLE_CLICK_WINDOW_MS)
                        {
                            _clickCount = 1;
                            _firstClickTime = now;
                        }
                        else
                        {
                            _clickCount++;

                            // Triple-click detected!
                            if (_clickCount == 3)
                            {
                                Debug.WriteLine("🎯 TRIPLE-CLICK DETECTED!");
                                HotkeyPressed?.Invoke(this, EventArgs.Empty);

                                // Reset
                                _clickCount = 0;
                                _resetTimer?.Stop();
                                return CallNextHookEx(_hookID, nCode, wParam, lParam);
                            }
                        }

                        // Restart reset timer
                        _resetTimer?.Stop();
                        _resetTimer?.Start();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MouseHook error: {ex.Message}");
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Unregister();
            if (_resetTimer != null)
            {
                _resetTimer.Stop();
                _resetTimer = null;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private enum MouseButton
        {
            XButton1, // Mouse Button 4
            XButton2  // Mouse Button 5
        }
    }
}
