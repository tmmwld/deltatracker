using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private F8Hotkey? _f8Hotkey;
        private Rectangle? _scanRegion;
        private QuoteService? _quoteService;
        private string _currentLanguage = "en";
        private Views.FloatingScanButton? _floatingButton;

        public MainWindow()
        {
            DiagnosticLogger.Log("=== MAINWINDOW CONSTRUCTOR START ===");
            
            try
            {
                InitializeComponent();
                DiagnosticLogger.Log("✓ MainWindow.InitializeComponent completed");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogException("MainWindow.InitializeComponent", ex);
                throw;
            }
            
            try
            {
                _dbManager = new DatabaseManager();
                DiagnosticLogger.Log("✓ DatabaseManager created");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogException("DatabaseManager creation", ex);
                throw;
            }
            
            try
            {
                _ocrEngine = new TesseractOCREngine();
                DiagnosticLogger.Log("✓ TesseractOCREngine created");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogException("TesseractOCREngine creation", ex);
                throw;
            }
            
            try
            {
                _quoteService = new QuoteService();
                DiagnosticLogger.Log("✓ QuoteService created");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogException("QuoteService creation", ex);
                throw;
            }
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            
            DiagnosticLogger.Log("=== MAINWINDOW CONSTRUCTOR END ===");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DiagnosticLogger.Log("=== MAINWINDOW_LOADED START ===");
            try
            {
                // Initialize OCR engine (may fail if tessdata missing)
                try
                {
                    _ocrEngine.Initialize();
                    System.Diagnostics.Debug.WriteLine("✓ OCR engine initialized");
                }
                catch (Exception ocrEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠ OCR initialization failed: {ocrEx.Message}");
                    UpdateStatus("OCR not available - tessdata missing");
                    // App can still run without OCR (for testing)
                }
                
                // Load saved settings (region, language)
                LoadSettings();
                
                // Load initial data
                RefreshDashboard();
                RefreshAnalytics();

                // Load Quote of the Day
                LoadRandomQuote();

                // Premium entrance animations for dashboard cards (after initialization)
                AnimationHelper.StaggerFadeIn(BalanceCard, PLCard, ActionsCard, QuoteCard);
                
                DiagnosticLogger.Log("✓ MainWindow initialization complete");
                DiagnosticLogger.Log("=== MAINWINDOW_LOADED END ===");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.Log($"✗ MainWindow_Loaded error: {ex.Message}");
                DiagnosticLogger.Log($"Stack trace: {ex.StackTrace}");
                DiagnosticLogger.LogException("MainWindow_Loaded", ex);
                MessageBox.Show($"Error during initialization: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            try
            {
                // Register F8 hotkey AFTER window handle is ready
                System.Diagnostics.Debug.WriteLine("Registering F8 global hotkey");
                
                _f8Hotkey = new F8Hotkey(this);
                _f8Hotkey.HotkeyPressed += Hotkey_Pressed;
                
                if (_f8Hotkey.Register())
                {
                    System.Diagnostics.Debug.WriteLine("✓ F8 hotkey registered successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠ F8 hotkey failed to register (may be already in use)");
                    // Don't crash - app still usable with floating button
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ F8 hotkey registration error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Silently fail - app still usable without F8 hotkey
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Save settings before exit
                SaveSettings();
                
                // Dispose all resources in proper order
                _floatingButton?.Close();
                _f8Hotkey?.Dispose();
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
            _currentLanguage = lang;
            System.Diagnostics.Debug.WriteLine($"Loading saved language: {lang}");
            App.Instance.ChangeLanguage(lang);
            
            // Update button text to current language
            LanguageToggleButton.Content = lang.ToUpper();
            
            // Load Floating Button preference
            var floatingEnabled = _dbManager.GetSetting("FloatingButtonEnabled") == "true";
            FloatingButtonToggle.IsChecked = floatingEnabled;
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
            RefreshDashboard();
        }

        private void CheaterButton_Click(object sender, RoutedEventArgs e)
        {
            // Play feedback animation
            AnimationHelper.TiltButtonFeedback(CheaterButton);
            
            // Record cheater event
            _dbManager.RecordCheater(DateTime.Now);
            
            // Refresh to update counters
            RefreshAnalytics();
            RefreshDashboard();
        }

        private void UndoCheaterButton_Click(object sender, MouseButtonEventArgs e)
        {
            _dbManager.UndoCheater(DateTime.Now);
            RefreshAnalytics();
            RefreshDashboard();
        }

        private void RedButton_Click(object sender, RoutedEventArgs e)
        {
            // Play feedback animation
            AnimationHelper.TiltButtonFeedback(RedButton);
            
            // Record red item event
            _dbManager.RecordRedItem(DateTime.Now);
            
            // Refresh to update counters
            RefreshAnalytics();
            RefreshDashboard();
        }

        private void UndoRedButton_Click(object sender, MouseButtonEventArgs e)
        {
            _dbManager.UndoRedItem(DateTime.Now);
            RefreshAnalytics();
            RefreshDashboard();
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
            var dialog = new RegionSelectorWindow();
            if (dialog.ShowDialog() == true)
            {
                _scanRegion = dialog.SelectedRegion;
                SaveSettings();
                UpdateStatus(App.Instance.GetString("Lang.Success.RegionSet"));
                RefreshAnalytics();
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
            if (_quoteService == null) return;
            
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

        private void LanguageToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle between EN and RU
            _currentLanguage = _currentLanguage == "en" ? "ru" : "en";
            
            System.Diagnostics.Debug.WriteLine($"Toggling language to: {_currentLanguage}");
            
            // Update button text
            LanguageToggleButton.Content = _currentLanguage.ToUpper();
            
            // Change language
            App.Instance.ChangeLanguage(_currentLanguage);
            _dbManager.SaveSetting("Language", _currentLanguage);
            
            // Refresh all UI elements
            RefreshDashboard();
            RefreshAnalytics();
            LoadRandomQuote();
            
            System.Diagnostics.Debug.WriteLine($"✓ Language toggled to {_currentLanguage}");
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

            // Update today counters
            var cheaters = _dbManager.GetDailyCheaterCount(today);
            var tilts = _dbManager.GetDailyTiltCount(today);
            var reds = _dbManager.GetDailyRedItemCount(today);
            
            TodayCountersText.Text = $"Cheaters {cheaters} | Tilts {tilts} | Reds {reds}";
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

            // Update event counter totals
            TotalTiltsCard.Text = _dbManager.GetTotalTilts().ToString();
            TotalCheatersText.Text = _dbManager.GetTotalCheaters().ToString();
            TotalRedsText.Text = _dbManager.GetTotalRedItems().ToString();

            // Update history grid with delta calculation
            var recentScans = _dbManager.GetRecentScans(50);
            var orderedScans = recentScans.OrderByDescending(s => s.Timestamp).ToList();
            var historyItems = CalculateDeltas(orderedScans);
            HistoryDataGrid.ItemsSource = historyItems;

            // Update chart
            UpdateChart(allScans);
        }

        private void UpdateChart(List<BalanceScan> scans)
        {
            if (scans.Count == 0) return;

            // Sort scans by timestamp to maintain chronological order
            var sortedScans = scans.OrderBy(s => s.Timestamp).ToList();
            
            // Create index-based data points
            // X = scan index (0, 1, 2, 3...), Y = balance value
            var indexedData = sortedScans
                .Select((scan, index) => new ObservablePoint(index, (double)scan.NumericValue))
                .ToList();

            // Adaptive sampling for very large datasets (100+ scans)
            // Keep all data points for better accuracy, but adjust visual density
            List<ObservablePoint> chartData;
            int labelStep;
            
            if (sortedScans.Count <= 50)
            {
                // Small dataset: show all points, show every ~5th label
                chartData = indexedData;
                labelStep = Math.Max(1, sortedScans.Count / 10);
            }
            else if (sortedScans.Count <= 150)
            {
                // Medium dataset: show all points, show every ~10th label
                chartData = indexedData;
                labelStep = Math.Max(1, sortedScans.Count / 15);
            }
            else
            {
                // Large dataset: sample to ~100 points for performance
                int step = sortedScans.Count / 100;
                chartData = indexedData.Where((p, i) => i % step == 0).ToList();
                labelStep = Math.Max(1, chartData.Count / 15);
            }

            // Neon Cyan Color
            var neonCyan = SKColor.Parse("#00F0FF");
            var neonBlue = SKColor.Parse("#0F172A");

            var series = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = chartData,
                    Fill = new SolidColorPaint(neonCyan.WithAlpha(30)), // Transparent fill
                    Stroke = new SolidColorPaint(neonCyan) { StrokeThickness = 3 },
                    GeometrySize = sortedScans.Count <= 50 ? 8 : (sortedScans.Count <= 150 ? 6 : 4), // Smaller points for large datasets
                    GeometryStroke = new SolidColorPaint(neonCyan) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(neonBlue),
                    AnimationsSpeed = TimeSpan.FromMilliseconds(800), // Smooth drawing animation
                    LineSmoothness = 0.3 // Slight smoothing for better visual flow
                }
            };

            BalanceChart.Series = series;
            
            // X-Axis: Scan indices with adaptive labeling
            BalanceChart.XAxes = new[]
            {
                new Axis
                {
                    Name = "Scan #",
                    NamePaint = new SolidColorPaint(SKColors.LightGray) { SKTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) },
                    NameTextSize = 12,
                    Labeler = value =>
                    {
                        int index = (int)value;
                        // Show only first and last scan for clean visualization
                        if (index == 0 || index == sortedScans.Count - 1)
                            return $"#{index + 1}"; // Display as #1 and #N
                        return "";
                    },
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    TextSize = 11,
                    SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(20)),
                    MinStep = 1 // Ensure integer steps
                }
            };
            
            // Y-Axis: Balance values (unchanged)
            BalanceChart.YAxes = new[]
            {
                new Axis
                {
                    Name = "Balance",
                    NamePaint = new SolidColorPaint(SKColors.LightGray) { SKTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) },
                    NameTextSize = 12,
                    Labeler = value => ValueParser.FormatBalance((decimal)value),
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    TextSize = 11,
                    SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(20))
                }
            };
        }

        private List<ScanHistoryViewModel> CalculateDeltas(List<BalanceScan> scans)
        {
            var result = new List<ScanHistoryViewModel>();
            
            for (int i = 0; i < scans.Count; i++)
            {
                var current = scans[i];
                decimal? delta = null;
                string deltaText = "";
                string deltaIndicator = "";
                string deltaColor = "#94A3B8"; // Default gray for no change
                
                // Calculate delta from previous scan (next in list since it's ordered newest first)
                if (i < scans.Count - 1)
                {
                    var previous = scans[i + 1];
                    delta = current.NumericValue - previous.NumericValue;
                    
                    if (delta > 0)
                    {
                        deltaText = ValueParser.FormatProfitLoss(delta.Value);
                        deltaIndicator = "▲";
                        deltaColor = "#00FF88"; // Green (SuccessColor)
                    }
                    else if (delta < 0)
                    {
                        deltaText = ValueParser.FormatProfitLoss(delta.Value);
                        deltaIndicator = "▼";
                        deltaColor = "#FF2A6D"; // Red (ErrorColor)
                    }
                    else
                    {
                        deltaText = "0";
                        deltaIndicator = "";
                        deltaColor = "#94A3B8"; // Gray (TextSecondary)
                    }
                }
                
                result.Add(new ScanHistoryViewModel
                {
                    Timestamp = current.Timestamp,
                    RawValue = current.RawValue,
                    NumericValue = current.NumericValue,
                    Delta = delta,
                    DeltaText = deltaText,
                    DeltaIndicator = deltaIndicator,
                    DeltaColor = deltaColor
                });
            }
            
            return result;
        }

        private void FloatingButtonToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_floatingButton == null)
            {
                _floatingButton = new Views.FloatingScanButton();
                _floatingButton.ScanRequested += (s, args) => PerformScan();
            }
            _floatingButton.Show();
            _dbManager.SaveSetting("FloatingButtonEnabled", "true");
        }

        private void FloatingButtonToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _floatingButton?.Hide();
            _dbManager.SaveSetting("FloatingButtonEnabled", "false");
        }

        private void UpdateStatus(string message)
        {
            StatusTextInline.Text = message;
        }
    }

}
