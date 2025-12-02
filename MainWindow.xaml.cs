using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using DeltaForceTracker.Database;
using DeltaForceTracker.Hotkeys;
using DeltaForceTracker.Models;
using DeltaForceTracker.OCR;
using DeltaForceTracker.Utils;
using DeltaForceTracker.Views;
using DeltaForceTracker.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Newtonsoft.Json;

namespace DeltaForceTracker
{
    public partial class MainWindow : Window
    {
        private DatabaseManager _dbManager;
        private TesseractOCREngine _ocrEngine;
        private GlobalHotkey? _hotkey;
        private Rectangle? _scanRegion;
        private bool _isInitialized = false;
        private QuoteService _quoteService;

        public MainWindow()
        {
            InitializeComponent();
            
            _dbManager = new DatabaseManager();
            _ocrEngine = new TesseractOCREngine();
            _quoteService = new QuoteService();
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize OCR engine
                _ocrEngine.Initialize();
                
                // Load saved settings (region, hotkey, language)
                LoadSettings();
                
                // Register global hotkey (MOVED HERE from constructor)
                var hotkeyString = _dbManager.GetSetting("Hotkey") ?? "F8";
                var key = GlobalHotkey.ParseKeyString(hotkeyString);
                
                var windowHandle = new WindowInteropHelper(this).Handle;
                System.Diagnostics.Debug.WriteLine($"Window handle: {windowHandle}");
                
                _hotkey = new GlobalHotkey(windowHandle, key);
                _hotkey.HotkeyPressed += Hotkey_Pressed;
                
                if (_hotkey.Register())
                {
                    UpdateStatus($"Hotkey registered: {hotkeyString}");
                    System.Diagnostics.Debug.WriteLine($"✓ Hotkey {hotkeyString} registered successfully");
                }
                else
                {
                    UpdateStatus("Failed to register hotkey: " + hotkeyString);
                    System.Diagnostics.Debug.WriteLine($"✗ Failed to register hotkey {hotkeyString}");
                }
                
                // Load initial data
                RefreshDashboard();
                RefreshAnalytics();

                // Load Quote of the Day
                LoadRandomQuote();

                // Premium entrance animations for dashboard cards (after initialization)
                AnimationHelper.StaggerFadeIn(BalanceCard, PLCard, StatusCard, ActionsCard, QuoteCard);
                
                // IMPORTANT: Set _isInitialized LAST so language selector doesn't fire during setup
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("✓ MainWindow initialization complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during initialization: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Save settings before exit
                SaveSettings();
                
                // Dispose all resources in proper order
                _hotkey?.Dispose();
                _ocrEngine?.Dispose();
                _quoteService?.Dispose();
                _dbManager?.Dispose();
                
                // Force SQLite to release all file locks
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            }
            catch (Exception ex)
            {
                // Log error but don't prevent closing
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            var regionJson = _dbManager.GetSetting("ScanRegion");
            if (!string.IsNullOrEmpty(regionJson))
            {
                try
                {
                    var rect = JsonConvert.DeserializeObject<Rectangle>(regionJson);
                    _scanRegion = rect;
                }
                catch { }
            }
            
            // Load Language
            var lang = _dbManager.GetSetting("Language") ?? "en";
            System.Diagnostics.Debug.WriteLine($"Loading saved language: {lang}");
            App.Instance.ChangeLanguage(lang);
            
            // Update selector to match (without triggering SelectionChanged since _isInitialized is still false)
            foreach (ComboBoxItem item in LanguageSelector.Items)
            {
                if (item.Tag?.ToString() == lang)
                {
                    LanguageSelector.SelectedItem = item;
                    System.Diagnostics.Debug.WriteLine($"Set LanguageSelector to: {lang}");
                    break;
                }
            }
        }

        private void SaveSettings()
        {
            if (_scanRegion.HasValue)
            {
                var regionJson = JsonConvert.SerializeObject(_scanRegion.Value);
                _dbManager.SaveSetting("ScanRegion", regionJson);
            }
        }

        private void Hotkey_Pressed(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"🔥 HOTKEY PRESSED! Timestamp: {DateTime.Now:HH:mm:ss}");
            
            // TEMPORARY: Show message box to confirm hotkey is firing (even when minimized)
            System.Windows.MessageBox.Show($"Hotkey triggered! {DateTime.Now:HH:mm:ss}", "Debug", MessageBoxButton.OK);
            
            try
            {
                // Invoke on UI thread to avoid cross-thread issues
                Dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine("Executing PerformScan from hotkey...");
                    PerformScan();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in Hotkey_Pressed: {ex.Message}");
                // Show error to user since app might be minimized
                System.Windows.MessageBox.Show($"Hotkey error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                // Assuming MaximizeButton is a control in your XAML
                // You might need to cast sender or access it by name if it's not a direct property
                // For example: ((Button)sender).Content = "□"; or MaximizeButton.Content = "□";
                // For now, I'll assume MaximizeButton is accessible directly.
                MaximizeButton.Content = "□"; 
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "❐";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            PerformScan();
        }

        private void TiltButton_Click(object sender, RoutedEventArgs e)
        {
            // Play tilt sound
            SoundPlayer.PlayTiltSound();
            
            // Play feedback animation
            AnimationHelper.TiltButtonFeedback(TiltButton);
            
            // Record tilt event
            _dbManager.RecordTilt(DateTime.Now);
            
            // Refresh analytics to update counters
            RefreshAnalytics();
        }

        private void PerformScan()
        {
            if (!_scanRegion.HasValue)
            {
                System.Windows.MessageBox.Show(
                    App.Instance.GetString("Lang.Error.NoRegion"),
                    App.Instance.GetString("Lang.Error.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            UpdateStatus(App.Instance.GetString("Lang.Status.Scanning"));

            try
            {
                Bitmap screenshot = ScreenCapture.CaptureRegion(_scanRegion.Value);

                var result = _ocrEngine.ExtractBalanceFromRegion(screenshot);
                screenshot.Dispose();

                if (result.success)
                {
                    var now = DateTime.Now;
                    _dbManager.RecordScan(now, result.rawValue, result.numericValue);
                    
                    // Play success sound
                    SoundPlayer.PlayScanSound();
                    
                    UpdateStatus($"Scan successful: {result.rawValue}");
                    RefreshDashboard();
                    RefreshAnalytics();
                }
                else
                {
                    UpdateStatus($"Scan failed: {result.error}");
                    System.Windows.MessageBox.Show(
                        result.error,
                        "OCR Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Scan error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void SelectRegionButton_Click(object sender, RoutedEventArgs e)
        {
            var selector = new RegionSelectorWindow();
            if (selector.ShowDialog() == true)
            {
                _scanRegion = selector.SelectedRegion;
                SaveSettings();
                UpdateStatus($"Region updated: {_scanRegion.Value.Width}x{_scanRegion.Value.Height}");
            }
        }

        private void ChangeHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HotkeyDialog();
            if (dialog.ShowDialog() == true)
            {
                var newKey = dialog.SelectedKey;
                
                if (_hotkey != null)
                {
                    if (_hotkey.UpdateHotkey(newKey))
                    {
                        _dbManager.SaveSetting("Hotkey", newKey.ToString());
                        UpdateStatus($"Hotkey changed to {newKey}");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "Failed to register new hotkey.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                }
            }
        }

        private void ResetDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                App.Instance.GetString("Lang.Confirm.Reset"),
                App.Instance.GetString("Lang.Confirm.Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbManager.ClearAllData();
                    RefreshDashboard();
                    RefreshAnalytics();
                    UpdateStatus(App.Instance.GetString("Lang.Success.Reset"));
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Failed to reset data: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        private void RefreshQuoteButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRandomQuote(animate: true);
        }

        private void LoadRandomQuote(bool animate = false)
        {
            var newQuote = _quoteService.GetRandomQuote();

            if (animate)
            {
                // Fade out
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(200))
                };

                fadeOut.Completed += (s, args) =>
                {
                    QuoteText.Text = newQuote;

                    // Fade in
                    var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = new Duration(TimeSpan.FromMilliseconds(400))
                    };

                    QuoteText.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };

                QuoteText.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                QuoteText.Text = newQuote;
            }
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"LanguageSelector_SelectionChanged fired. _isInitialized={_isInitialized}");
            
            if (!_isInitialized) return;

            if (LanguageSelector.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                System.Diagnostics.Debug.WriteLine($"Switching language to: {langCode}");
                App.Instance.ChangeLanguage(langCode);
                _dbManager.SaveSetting("Language", langCode);
                
                // Refresh all UI elements to apply language changes
                RefreshDashboard();
                RefreshAnalytics();
                
                // Reload quote in new language context
                LoadRandomQuote();
                
                System.Diagnostics.Debug.WriteLine($"✓ Language switched to {langCode}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✗ Language selector item or tag is null");
            }
        }

        private void RefreshDashboard()
        {
            var currentBalance = _dbManager.GetCurrentBalance();
            var today = DateTime.Today;
            var dailyStats = _dbManager.GetDailyStats(today);

            // Simple text update (count-up animation causes parsing issues)
            CurrentBalanceText.Text = ValueParser.FormatBalance(currentBalance);
            
            var pl = dailyStats.ProfitLoss;
            DailyPLText.Text = ValueParser.FormatProfitLoss(pl);
            DailyPLText.Foreground = pl >= 0 
                ? (SolidColorBrush)FindResource("SuccessBrush")
                : (SolidColorBrush)FindResource("ErrorBrush");

            var lastScan = _dbManager.GetRecentScans(1).FirstOrDefault();
            if (lastScan != null)
            {
                LastScanText.Text = $"{lastScan.Timestamp:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                LastScanText.Text = App.Instance.GetString("Lang.Status.NoScans");
            }
        }

        private void RefreshAnalytics()
        {
            // Update statistics
            var highestBalance = _dbManager.GetHighestBalanceEver();
            HighestBalanceText.Text = ValueParser.FormatBalance(highestBalance);

            var bestDay = _dbManager.GetBestDay();
            if (bestDay != null)
            {
                BestDayText.Text = ValueParser.FormatProfitLoss(bestDay.ProfitLoss);
                BestDayText.Foreground = bestDay.ProfitLoss >= 0 
                    ? (SolidColorBrush)FindResource("SuccessBrush")
                    : (SolidColorBrush)FindResource("ErrorBrush");
            }

            var worstDay = _dbManager.GetWorstDay();
            if (worstDay != null)
            {
                WorstDayText.Text = ValueParser.FormatProfitLoss(worstDay.ProfitLoss);
                WorstDayText.Foreground = worstDay.ProfitLoss >= 0 
                    ? (SolidColorBrush)FindResource("SuccessBrush")
                    : (SolidColorBrush)FindResource("ErrorBrush");
            }

            var allScans = _dbManager.GetAllScans();
            TotalScansText.Text = allScans.Count.ToString();

            // Update tilt counters
            var totalTilts = _dbManager.GetTiltCount();
            TotalTiltsText.Text = totalTilts.ToString();
            
            var todayTilts = _dbManager.GetDailyTiltCount(DateTime.Today);
            TodayTiltsText.Text = todayTilts.ToString();

            // Update history grid
            var recentScans = _dbManager.GetRecentScans(50);
            HistoryDataGrid.ItemsSource = recentScans.OrderByDescending(s => s.Timestamp).ToList();

            // Update chart
            UpdateChart(allScans);
        }

        private void UpdateChart(List<BalanceScan> scans)
        {
            if (scans.Count == 0) return;

            var values = scans.Select(s => new DateTimePoint(s.Timestamp, (double)s.NumericValue)).ToList();

            // Neon Cyan Color
            var neonCyan = SKColor.Parse("#00F0FF");
            var neonBlue = SKColor.Parse("#0F172A");

            var series = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Values = values,
                    Fill = new SolidColorPaint(neonCyan.WithAlpha(30)), // Transparent fill
                    Stroke = new SolidColorPaint(neonCyan) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(neonCyan) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(neonBlue),
                    AnimationsSpeed = TimeSpan.FromMilliseconds(800) // Smooth drawing animation
                }
            };

            BalanceChart.Series = series;
            BalanceChart.XAxes = new[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("MM/dd HH:mm"),
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(20))
                }
            };
            BalanceChart.YAxes = new[]
            {
                new Axis
                {
                    Labeler = value => ValueParser.FormatBalance((decimal)value),
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(20))
                }
            };
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }
    }

}
