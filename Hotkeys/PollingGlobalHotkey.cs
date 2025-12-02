using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace DeltaForceTracker.Hotkeys
{
    /// <summary>
    /// Polling-based global hotkey - works EVERYWHERE including fullscreen games
    /// Uses GetAsyncKeyState polling instead of hooks/messages
    /// </summary>
    public class PollingGlobalHotkey : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        
        private DispatcherTimer _pollTimer;
        private bool _wasPressed = false;
        
        public event EventHandler? HotkeyPressed;
        
        public Keys CurrentKey { get; private set; }
        public ModifierKeys CurrentModifiers { get; private set; }
        
        public PollingGlobalHotkey(Keys key, ModifierKeys modifiers = ModifierKeys.None)
        {
            CurrentKey = key;
            CurrentModifiers = modifiers;
            
            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Poll every 50ms
            };
            _pollTimer.Tick += PollTimer_Tick;
        }
        
        public bool Register()
        {
            _pollTimer.Start();
            Debug.WriteLine($"✓ Polling hotkey started: {CurrentKey}");
            return true;
        }
        
        public void Unregister()
        {
            _pollTimer.Stop();
            Debug.WriteLine("Polling hotkey stopped");
        }
        
        public bool UpdateHotkey(Keys newKey, ModifierKeys newModifiers = ModifierKeys.None)
        {
            CurrentKey = newKey;
            CurrentModifiers = newModifiers;
            _wasPressed = false; // Reset state
            Debug.WriteLine($"Hotkey updated to: {CurrentKey}");
            return true;
        }
        
        private void PollTimer_Tick(object? sender, EventArgs e)
        {
            // Check if our key is currently pressed
            bool isKeyPressed = (GetAsyncKeyState((int)CurrentKey) & 0x8000) != 0;
            
            if (isKeyPressed && !_wasPressed)
            {
                // Key was just pressed (rising edge)
                bool modifiersMatch = CheckModifiers();
                
                if (modifiersMatch)
                {
                    Debug.WriteLine($"🔥 POLLING HOTKEY DETECTED: {CurrentKey} at {DateTime.Now:HH:mm:ss.fff}");
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                
                _wasPressed = true;
            }
            else if (!isKeyPressed)
            {
                // Key released
                _wasPressed = false;
            }
        }
        
        private bool CheckModifiers()
        {
            bool ctrlPressed = (GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0;
            bool altPressed = (GetAsyncKeyState((int)Keys.Menu) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
            
            bool ctrlRequired = (CurrentModifiers & ModifierKeys.Control) != 0;
            bool altRequired = (CurrentModifiers & ModifierKeys.Alt) != 0;
            bool shiftRequired = (CurrentModifiers & ModifierKeys.Shift) != 0;
            
            return (ctrlPressed == ctrlRequired) && 
                   (altPressed == altRequired) && 
                   (shiftPressed == shiftRequired);
        }
        
        public void Dispose()
        {
            Unregister();
            _pollTimer = null!;
        }
        
        public static Keys ParseKeyString(string keyString)
        {
            if (Enum.TryParse<Keys>(keyString, true, out var key))
                return key;
            
            return Keys.F8; // Default
        }
    }
}
