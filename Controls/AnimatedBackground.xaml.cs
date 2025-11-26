using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DeltaForceTracker.Controls
{
    public partial class AnimatedBackground : UserControl
    {
        public AnimatedBackground()
        {
            InitializeComponent();
            Loaded += AnimatedBackground_Loaded;
        }

        private void AnimatedBackground_Loaded(object sender, RoutedEventArgs e)
        {
            CreateHexPattern();
            StartAnimations();
        }

        private void CreateHexPattern()
        {
            const int hexSize = 40;
            const int spacing = 10;
            var width = ActualWidth > 0 ? ActualWidth : 1080;
            var height = ActualHeight > 0 ? ActualHeight : 720;

            var hexColor = new SolidColorBrush(Color.FromArgb(40, 0, 240, 255)); // Semi-transparent cyan

            for (double y = -hexSize; y < height + hexSize; y += hexSize + spacing)
            {
                for (double x = -hexSize; x < width + hexSize; x += (hexSize * 1.5) + spacing)
                {
                    var offsetY = (Math.Floor(x / (hexSize * 1.5)) % 2 == 0) ? 0 : (hexSize / 2 + spacing / 2);
                    CreateHexagon(x, y + offsetY, hexSize, hexColor);
                }
            }
        }

        private void CreateHexagon(double x, double y, double size, Brush stroke)
        {
            var polygon = new Polygon
            {
                Stroke = stroke,
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                Points = new PointCollection
                {
                    new Point(size * 0.5, 0),
                    new Point(size, size * 0.25),
                    new Point(size, size * 0.75),
                    new Point(size * 0.5, size),
                    new Point(0, size * 0.75),
                    new Point(0, size * 0.25)
                }
            };

            Canvas.SetLeft(polygon, x);
            Canvas.SetTop(polygon, y);

            HexCanvas.Children.Add(polygon);

            // Add subtle random opacity animation to each hex
            var random = new Random(Guid.NewGuid().GetHashCode());
            var delay = TimeSpan.FromMilliseconds(random.Next(0, 3000));
            
            var opacityAnim = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(3)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = delay,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            polygon.BeginAnimation(OpacityProperty, opacityAnim);
        }

        private void StartAnimations()
        {
            // Pulsing glow animation
            var glowAnim = new DoubleAnimation
            {
                From = 0.02,
                To = 0.08,
                Duration = new Duration(TimeSpan.FromSeconds(4)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            GlowOverlay.BeginAnimation(OpacityProperty, glowAnim);
        }
    }
}
