using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeltaForceTracker.Hotkeys
{
    /// <summary>
    /// Low-level global hotkey using keyboard hook - works even in fullscreen applications
    /// </summary>
    public class LowLevelGlobalHotkey : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        public event EventHandler? HotkeyPressed;
        
        public Keys CurrentKey { get; private set; }
        public ModifierKeys CurrentModifiers { get; private set; }
        
        public LowLevelGlobalHotkey(Keys key, ModifierKeys modifiers = ModifierKeys.None)
        {
            CurrentKey = key;
            CurrentModifiers = modifiers;
            _proc = HookCallback;
        }
        
        public bool Register()
        {
            if (_hookID != IntPtr.Zero)
                return true; // Already registered
                
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            
            Debug.WriteLine($"Low-level hotkey registered: {_hookID != IntPtr.Zero}");
            return _hookID != IntPtr.Zero;
        }
        
        public void Unregister()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                Debug.WriteLine("Low-level hotkey unregistered");
            }
        }
        
        public bool UpdateHotkey(Keys newKey, ModifierKeys newModifiers = ModifierKeys.None)
        {
            CurrentKey = newKey;
            CurrentModifiers = newModifiers;
            // No need to re-register, hook is already active
            return true;
        }
        
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys pressedKey = (Keys)vkCode;
                
                // Check if this is our hotkey
                if (pressedKey == CurrentKey)
                {
                    bool modifiersMatch = CheckModifiers();
                    
                    if (modifiersMatch)
                    {
                        Debug.WriteLine($"🔥 Low-level hotkey detected: {CurrentKey}");
                        HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        
        private bool CheckModifiers()
        {
            bool ctrlPressed = (Control.ModifierKeys & System.Windows.Forms.Keys.Control) != 0;
            bool altPressed = (Control.ModifierKeys & System.Windows.Forms.Keys.Alt) != 0;
            bool shiftPressed = (Control.ModifierKeys & System.Windows.Forms.Keys.Shift) != 0;
            
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
        }
        
        public static Keys ParseKeyString(string keyString)
        {
            if (Enum.TryParse<Keys>(keyString, true, out var key))
                return key;
            
            return Keys.F8; // Default
        }
    }
}
