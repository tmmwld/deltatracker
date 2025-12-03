using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace DeltaForceTracker.Hotkeys
{
    public class MouseHook : IDisposable
    {
        private const int VK_XBUTTON1 = 0x05; // Mouse Button 4
        private const int VK_XBUTTON2 = 0x06; // Mouse Button 5
        private const int TRIPLE_CLICK_WINDOW_MS = 500;
        private const int POLLING_INTERVAL_MS = 50;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private DispatcherTimer? _pollingTimer;
        private DispatcherTimer? _resetTimer;
        private int _targetButton;
        private bool _wasPressed = false;

        // Triple-click detection
        private int _clickCount = 0;
        private DateTime _firstClickTime = DateTime.MinValue;

        public event EventHandler? HotkeyPressed;

        public MouseHook(string buttonName)
        {
            _targetButton = buttonName == "Mouse5" ? VK_XBUTTON2 : VK_XBUTTON1;

            // Polling timer (check button state every 50ms)
            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(POLLING_INTERVAL_MS)
            };
            _pollingTimer.Tick += PollingTimer_Tick;

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
            _pollingTimer?.Start();
            Debug.WriteLine($"MouseHook polling started for VK {_targetButton:X2} (50ms interval)");
            return true; // Polling always succeeds
        }

        public void Unregister()
        {
            _pollingTimer?.Stop();
            _resetTimer?.Stop();
            Debug.WriteLine("MouseHook polling stopped");
        }

        private void PollingTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Check if button is currently pressed (high-order bit set)
                bool isPressed = (GetAsyncKeyState(_targetButton) & 0x8000) != 0;

                // Detect button press (edge detection: not pressed -> pressed)
                if (isPressed && !_wasPressed)
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
                            Debug.WriteLine("🎯 TRIPLE-CLICK DETECTED!");
                            HotkeyPressed?.Invoke(this, EventArgs.Empty);
                            
                            // Reset
                            _clickCount = 0;
                            _resetTimer?.Stop();
                            _wasPressed = isPressed;
                            return;
                        }
                    }

                    // Restart reset timer
                    _resetTimer?.Stop();
                    _resetTimer?.Start();
                }

                _wasPressed = isPressed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MouseHook polling error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Unregister();
            if (_pollingTimer != null)
            {
                _pollingTimer.Stop();
                _pollingTimer = null;
            }
            if (_resetTimer != null)
            {
                _resetTimer.Stop();
                _resetTimer = null;
            }
        }
    }
}
