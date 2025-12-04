using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeltaForceTracker.Views
{
    public partial class FloatingScanButton : Window
    {
        public event EventHandler? ScanRequested;

        public FloatingScanButton()
        {
            InitializeComponent();
            PositionWindow();
        }

        private void PositionWindow()
        {
            // Get screen dimensions
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // Position at right edge, 10% down from top (90% of height)
            this.Left = screenWidth - this.Width - 20; // 20px padding from edge
            this.Top = screenHeight * 0.10; // 10% from top
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            // Full opacity on hover
            if (sender is Border border)
            {
                border.Opacity = 1.0;
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            // Back to 85% opacity
            if (sender is Border border)
            {
                border.Opacity = 0.85;
            }
        }
    }
}
