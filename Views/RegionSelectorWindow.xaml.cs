using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

namespace DeltaForceTracker.Views
{
    public partial class RegionSelectorWindow : Window
    {
        private bool _isSelecting = false;
        private System.Windows.Point _startPoint;
        public Rectangle SelectedRegion { get; private set; }

        public RegionSelectorWindow()
        {
            InitializeComponent();
            KeyDown += RegionSelectorWindow_KeyDown;
        }

        private void RegionSelectorWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(Canvas);
            
            SelectionRectangle.Visibility = Visibility.Visible;
            System.Windows.Controls.Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            System.Windows.Controls.Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            
            InstructionText.Visibility = Visibility.Collapsed;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            var currentPoint = e.GetPosition(Canvas);
            
            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _startPoint.X);
            var height = Math.Abs(currentPoint.Y - _startPoint.Y);

            System.Windows.Controls.Canvas.SetLeft(SelectionRectangle, x);
            System.Windows.Controls.Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting) return;

            _isSelecting = false;

            var x = (int)System.Windows.Controls.Canvas.GetLeft(SelectionRectangle);
            var y = (int)System.Windows.Controls.Canvas.GetTop(SelectionRectangle);
            var width = (int)SelectionRectangle.Width;
            var height = (int)SelectionRectangle.Height;

            if (width > 10 && height > 10)
            {
                SelectedRegion = new Rectangle(x, y, width, height);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Selected region is too small. Please try again.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                SelectionRectangle.Visibility = Visibility.Collapsed;
                InstructionText.Visibility = Visibility.Visible;
            }
        }
    }
}
