using System;
using System.IO;
using System.Media;
using System.Windows.Media;

namespace DeltaForceTracker.Utils
{
    /// <summary>
    /// Utility class for playing sound effects
    /// </summary>
    public static class SoundPlayer
    {
        private static MediaPlayer _scanPlayer = new MediaPlayer();
        private static MediaPlayer _tiltPlayer = new MediaPlayer();
        private static MediaPlayer _achPlayer = new MediaPlayer();

        static SoundPlayer()
        {
            // Pre-load sound files for instant playback
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                string scanPath = Path.Combine(baseDir, "Resources", "Sfx", "scan.mp3");
                if (File.Exists(scanPath))
                {
                    _scanPlayer.Open(new Uri(scanPath, UriKind.Absolute));
                }

                string tiltPath = Path.Combine(baseDir, "Resources", "Sfx", "tilt.mp3");
                if (File.Exists(tiltPath))
                {
                    _tiltPlayer.Open(new Uri(tiltPath, UriKind.Absolute));
                }

                string achPath = Path.Combine(baseDir, "Resources", "Sfx", "ach.mp3");
                if (File.Exists(achPath))
                {
                    _achPlayer.Open(new Uri(achPath, UriKind.Absolute));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading sounds: {ex.Message}");
            }
        }

        /// <summary>
        /// Play scan success sound
        /// </summary>
        public static void PlayScanSound()
        {
            try
            {
                _scanPlayer.Stop();
                _scanPlayer.Position = TimeSpan.Zero;
                _scanPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing scan sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Play tilt button sound
        /// </summary>
        public static void PlayTiltSound()
        {
            try
            {
                _tiltPlayer.Stop();
                _tiltPlayer.Position = TimeSpan.Zero;
                _tiltPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing tilt sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Play achievement sound
        /// </summary>
        public static void PlayAchievementSound()
        {
            try
            {
                _achPlayer.Stop();
                _achPlayer.Position = TimeSpan.Zero;
                _achPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing achievement sound: {ex.Message}");
            }
        }
    }
}
