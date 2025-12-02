using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DeltaForceTracker.Utils
{
    /// <summary>
    /// Helper class for creating smooth, reusable animations
    /// </summary>
    public static class AnimationHelper
    {
        private static readonly Duration DefaultDuration = new Duration(TimeSpan.FromMilliseconds(600));
        private static readonly IEasingFunction DefaultEasing = new CubicEase { EasingMode = EasingMode.EaseOut };

        /// <summary>
        /// Animates a numeric value counting up from current to target with easing
        /// </summary>
        public static void CountUpAnimation(TextBlock textBlock, double from, double to, string format = "N2", string suffix = "M")
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
            };

            // Create a temporary property to animate
            var story = new Storyboard();
            var dummy = new DependencyObject();
            dummy.SetValue(CountProperty, from);

            Storyboard.SetTarget(animation, dummy);
            Storyboard.SetTargetProperty(animation, new PropertyPath(CountProperty));

            animation.CurrentTimeInvalidated += (s, e) =>
            {
                var currentValue = (double)dummy.GetValue(CountProperty);
                textBlock.Text = currentValue.ToString(format) + suffix;
            };

            story.Children.Add(animation);
            story.Begin();
        }

        private static readonly DependencyProperty CountProperty =
            DependencyProperty.RegisterAttached("Count", typeof(double), typeof(AnimationHelper));

        /// <summary>
        /// Fades in multiple elements sequentially with stagger delay
        /// </summary>
        public static void StaggerFadeIn(params UIElement[] elements)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                element.Opacity = 0;

                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = DefaultDuration,
                    BeginTime = TimeSpan.FromMilliseconds(i * 100),
                    EasingFunction = DefaultEasing
                };

                element.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

        /// <summary>
        /// Creates a continuous pulsing glow effect on an element
        /// </summary>
        public static void StartGlowPulse(UIElement element)
        {
            var animation = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        /// <summary>
        /// Adds scale-up effect on hover
        /// </summary>
        public static void AddHoverScale(FrameworkElement element, double scaleFrom = 1.0, double scaleTo = 1.05)
        {
            var scaleTransform = new ScaleTransform(1, 1);
            element.RenderTransform = scaleTransform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            element.MouseEnter += (s, e) =>
            {
                var scaleX = new DoubleAnimation(scaleFrom, scaleTo, new Duration(TimeSpan.FromMilliseconds(200)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var scaleY = new DoubleAnimation(scaleFrom, scaleTo, new Duration(TimeSpan.FromMilliseconds(200)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            };

            element.MouseLeave += (s, e) =>
            {
                var scaleX = new DoubleAnimation(scaleTo, scaleFrom, new Duration(TimeSpan.FromMilliseconds(200)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var scaleY = new DoubleAnimation(scaleTo, scaleFrom, new Duration(TimeSpan.FromMilliseconds(200)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            };
        }

        /// <summary>
        /// Slide in animation from left
        /// </summary>
        public static void SlideInFromLeft(UIElement element, double distance = 50)
        {
            var transform = new TranslateTransform(-distance, 0);
            element.RenderTransform = transform;
            element.Opacity = 0;

            var slideAnim = new DoubleAnimation
            {
                From = -distance,
                To = 0,
                Duration = DefaultDuration,
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = DefaultDuration,
                EasingFunction = DefaultEasing
            };

            transform.BeginAnimation(TranslateTransform.XProperty, slideAnim);
            element.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        /// <summary>
        /// Creates a ripple click effect
        /// </summary>
        public static void CreateRipple(Panel container, Point position, Color color)
        {
            var ellipse = new Ellipse
            {
                Width = 0,
                Height = 0,
                Fill = new SolidColorBrush(color) { Opacity = 0.3 },
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(position.X, position.Y, 0, 0)
            };

            container.Children.Add(ellipse);

            var sizeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 200,
                Duration = new Duration(TimeSpan.FromMilliseconds(600)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            double rippleSize = 200; // Define rippleSize here

            var sizeAnimation = new DoubleAnimation(0, rippleSize, new Duration(TimeSpan.FromMilliseconds(600)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var opacityAnimation = new DoubleAnimation(0.4, 0, new Duration(TimeSpan.FromMilliseconds(600)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            sizeAnimation.Completed += (s, e) => container.Children.Remove(ellipse);

            ellipse.BeginAnimation(Ellipse.WidthProperty, sizeAnimation);
            ellipse.BeginAnimation(Ellipse.HeightProperty, sizeAnimation);
            ellipse.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }

        /// <summary>
        /// Tilt button feedback animation - scale bump + opacity flash
        /// </summary>
        public static void TiltButtonFeedback(Button button)
        {
            // Setup transform
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            button.RenderTransform = scaleTransform;
            button.RenderTransformOrigin = new Point(0.5, 0.5);

            // Scale animation (bump up then return)
            var scaleAnimation = new DoubleAnimationUsingKeyFrames();
            scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
            scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));

            // Opacity flash
            var opacityAnimation = new DoubleAnimationUsingKeyFrames();
            opacityAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0.6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50))));
            opacityAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));

            // Start animations
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            button.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }
    }
}
