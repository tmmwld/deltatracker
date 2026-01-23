using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeltaForceTracker.Views
{
    public partial class FloatingScanButton : Window
    {
        public event EventHandler? ScanRequested;
        private double _baseOpacity = 0.15; // Default 15%

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

        /// <summary>
        /// Set the base opacity for the floating button
        /// </summary>
        public void SetOpacity(double opacity)
        {
            _baseOpacity = Math.Clamp(opacity, 0.05, 1.0);
            
            // Apply to border if not currently hovered
            if (ScanBorder != null && !ScanBorder.IsMouseOver)
            {
                ScanBorder.Opacity = _baseOpacity;
            }
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
            // Back to base opacity
            if (sender is Border border)
            {
                border.Opacity = _baseOpacity;
            }
        }
    }
}
