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
                AchIcon.Source = new BitmapImage(new Uri(iconPath, UriKind.Relative));
            }
            catch
            {
                // Fallback to locked icon if load fails
                try
                {
                    AchIcon.Source = new BitmapImage(new Uri("Resources/achievements/0_Locked.png", UriKind.Relative));
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
