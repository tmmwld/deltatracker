using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DeltaForceTracker.Models;
using DeltaForceTracker.Utils;

namespace DeltaForceTracker.Controls
{
    public partial class AchievementToast : UserControl
    {
        private Storyboard? _slideInStoryboard;
        private Storyboard? _fadeOutStoryboard;

        public event EventHandler? ToastClosed;

        public AchievementToast()
        {
            InitializeComponent();
        }

        public void Show(Achievement achievement, string language)
        {
            // Set icon
            try
            {
                var iconPath = achievement.GetIconPath();
                if (!iconPath.StartsWith("pack://"))
                {
                    iconPath = "pack://application:,,," + iconPath;
                }
                AchIcon.Source = new BitmapImage(new Uri(iconPath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading toast icon: {ex.Message}");
                // Fallback
                try
                {
                    AchIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/achievements/0_Locked.png"));
                }
                catch { }
            }

            // Set title
            AchTitle.Text = achievement.GetLocalizedTitle(language);

            // Play sound
            SoundPlayer.PlayAchievementSound();

            // Trigger slide-in animation
            _slideInStoryboard = (Storyboard)Resources["SlideInStoryboard"];
            _slideInStoryboard.Begin();

            // Auto-dismiss after 4 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Close();
            };
            timer.Start();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Storyboards are loaded
        }

        private void Close()
        {
            _fadeOutStoryboard = (Storyboard)Resources["FadeOutStoryboard"];
            _fadeOutStoryboard.Begin();
        }

        private void FadeOutStoryboard_Completed(object? sender, EventArgs e)
        {
            ToastClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
