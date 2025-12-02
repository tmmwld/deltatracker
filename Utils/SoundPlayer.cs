using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static List<MediaPlayer> _tiltPlayers = new List<MediaPlayer>();
        private static MediaPlayer _achPlayer = new MediaPlayer();
        private static Random _random = new Random();

        static SoundPlayer()
        {
            // Pre-load sound files for instant playback
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string sfxDir = Path.Combine(baseDir, "Resources", "Sfx");
                
                // Load scan sound
                string scanPath = Path.Combine(sfxDir, "scan.mp3");
                if (File.Exists(scanPath))
                {
                    _scanPlayer.Open(new Uri(scanPath, UriKind.Absolute));
                }

                // Load all tilt sounds (tilt1.wav, tilt2.wav, etc.)
                if (Directory.Exists(sfxDir))
                {
                    var tiltFiles = Directory.GetFiles(sfxDir, "tilt*.wav")
                        .OrderBy(f => f)
                        .ToList();

                    foreach (var tiltFile in tiltFiles)
                    {
                        var player = new MediaPlayer();
                        player.Open(new Uri(tiltFile, UriKind.Absolute));
                        _tiltPlayers.Add(player);
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded {_tiltPlayers.Count} tilt sounds");
                }

                // Load achievement sound
                string achPath = Path.Combine(sfxDir, "ach.mp3");
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
        /// Play random tilt button sound
        /// </summary>
        public static void PlayTiltSound()
        {
            try
            {
                if (_tiltPlayers.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No tilt sounds loaded");
                    return;
                }

                // Pick random tilt sound
                int index = _random.Next(_tiltPlayers.Count);
                var player = _tiltPlayers[index];

                player.Stop();
                player.Position = TimeSpan.Zero;
                player.Play();

                System.Diagnostics.Debug.WriteLine($"Playing tilt sound #{index + 1}");
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
