using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DeltaForceTracker.Hotkeys
{
    public class MouseHook : IDisposable
    {
        private const int WM_INPUT = 0x00FF;
        private const int RIM_TYPEMOUSE = 0;
        private const int RI_MOUSE_BUTTON_4_DOWN = 0x0040;
        private const int RI_MOUSE_BUTTON_5_DOWN = 0x0100;
        private const int TRIPLE_CLICK_WINDOW_MS = 500;

        [DllImport("user32.dll")]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        private const uint RID_INPUT = 0x10000003;
        private const uint RIDEV_INPUTSINK = 0x00000100; // Receive input even when not focused

        private Window _window;
        private HwndSource? _hwndSource;
        private int _targetButton; // RI_MOUSE_BUTTON_4_DOWN or RI_MOUSE_BUTTON_5_DOWN
        
        // Triple-click detection
        private int _clickCount = 0;
        private DateTime _firstClickTime = DateTime.MinValue;
        private DispatcherTimer? _resetTimer;

        public event EventHandler? HotkeyPressed;

        public MouseHook(string buttonName, Window window)
        {
            _window = window;
            _targetButton = buttonName == "Mouse5" ? RI_MOUSE_BUTTON_5_DOWN : RI_MOUSE_BUTTON_4_DOWN;

            // Reset timer for click sequence
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
            try
            {
                // Get window handle
                var windowHelper = new WindowInteropHelper(_window);
                IntPtr hwnd = windowHelper.Handle;

                if (hwnd == IntPtr.Zero)
                {
                    Debug.WriteLine("❌ Window handle is null, cannot register Raw Input");
                    return false;
                }

                // Register for Raw Input
                RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
                rid[0].usUsagePage = 0x01; // HID_USAGE_PAGE_GENERIC
                rid[0].usUsage = 0x02;     // HID_USAGE_GENERIC_MOUSE
                rid[0].dwFlags = RIDEV_INPUTSINK; // Receive even when not focused!
                rid[0].hwndTarget = hwnd;

                if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
                {
                    Debug.WriteLine("❌ RegisterRawInputDevices failed");
                    return false;
                }

                // Hook into WndProc to receive WM_INPUT messages
                _hwndSource = HwndSource.FromHwnd(hwnd);
                if (_hwndSource != null)
                {
                    _hwndSource.AddHook(WndProc);
                    Debug.WriteLine($"✅ Raw Input registered for button {(_targetButton == RI_MOUSE_BUTTON_4_DOWN ? "Mouse4" : "Mouse5")} (RIDEV_INPUTSINK - works unfocused!)");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Raw Input registration error: {ex.Message}");
                return false;
            }
        }

        public void Unregister()
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
            _resetTimer?.Stop();
            Debug.WriteLine("Raw Input unregistered");
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_INPUT)
            {
                try
                {
                    uint dwSize = 0;
                    GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

                    IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
                    try
                    {
                        if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                        {
                            RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(buffer);

                            if (raw.header.dwType == RIM_TYPEMOUSE)
                            {
                                // Check for our target button press
                                if ((raw.mouse.ulButtons & _targetButton) != 0)
                                {
                                    OnButtonClick();
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WM_INPUT processing error: {ex.Message}");
                }
            }

            return IntPtr.Zero;
        }

        private void OnButtonClick()
        {
            var now = DateTime.Now;

            // Start new sequence if timeout expired
            if ((now - _firstClickTime).TotalMilliseconds > TRIPLE_CLICK_WINDOW_MS)
            {
                _clickCount = 1;
                _firstClickTime = now;
                Debug.WriteLine($"Click 1/3 detected");
            }
            else
            {
                _clickCount++;
                Debug.WriteLine($"Click {_clickCount}/3 detected");

                // Triple-click detected!
                if (_clickCount == 3)
                {
                    Debug.WriteLine("🎯 TRIPLE-CLICK DETECTED (Raw Input)!");
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    
                    // Reset
                    _clickCount = 0;
                    _resetTimer?.Stop();
                    return;
                }
            }

            // Restart reset timer
            _resetTimer?.Stop();
            _resetTimer?.Start();
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

        #region P/Invoke Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct RAWINPUT
        {
            [FieldOffset(0)] public RAWINPUTHEADER header;
            [FieldOffset(24)] public RAWMOUSE mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE
        {
            public ushort usFlags;
            public uint ulButtons;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        #endregion
    }
}
