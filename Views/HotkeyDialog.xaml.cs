using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace DeltaForceTracker.Views
{
    public partial class HotkeyDialog : Window
    {
        public Keys SelectedKey { get; private set; } = Keys.F8;

        public HotkeyDialog()
        {
            InitializeComponent();
            PreviewKeyDown += HotkeyDialog_PreviewKeyDown;
        }

        private void HotkeyDialog_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Convert WPF key to Windows Forms key
            var key = (Keys)KeyInterop.VirtualKeyFromKey(e.Key);
            
            // Only allow function keys and letter/number keys
            if ((key >= Keys.F1 && key <= Keys.F24) || 
                (key >= Keys.A && key <= Keys.Z) ||
                (key >= Keys.D0 && key <= Keys.D9))
            {
                SelectedKey = key;
                CurrentKeyText.Text = key.ToString();
                OkButton.IsEnabled = true;
            }
            
            e.Handled = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
