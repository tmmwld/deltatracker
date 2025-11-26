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

        public MainWindow()
        {
            InitializeComponent();
            
            _dbManager = new DatabaseManager();
            _ocrEngine = new TesseractOCREngine();
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Premium entrance animations for dashboard cards
            AnimationHelper.StaggerFadeIn(BalanceCard, PLCard, StatusCard, ActionsCard);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize OCR
                _ocrEngine.Initialize();
                
                // Load settings
                LoadSettings();
                
                // Register hotkey
                var windowHandle = new WindowInteropHelper(this).Handle;
                var hotkeyString = _dbManager.GetSetting("Hotkey") ?? "F8";
                var key = GlobalHotkey.ParseKeyString(hotkeyString);
                
                _hotkey = new GlobalHotkey(windowHandle, key);
                _hotkey.HotkeyPressed += OnHotkeyPressed;
                
                if (_hotkey.Register())
                {
                    UpdateStatus("Hotkey registered: " + hotkeyString);
                }
                else
                {
                    UpdateStatus("Failed to register hotkey: " + hotkeyString);
                }
                
                // Load initial data
                RefreshDashboard();
                RefreshAnalytics();
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Initialization error: {ex.Message}\n\nMake sure tessdata folder exists.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void MainWindow_Closing(object? sender, EventArgs e)
        {
            _hotkey?.Dispose();
            _ocrEngine?.Dispose();
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
            App.Instance.ChangeLanguage(lang);
            
            // Update selector without triggering event
            _isInitialized = false;
            foreach (ComboBoxItem item in LanguageSelector.Items)
            {
                if (item.Tag.ToString() == lang)
                {
                    LanguageSelector.SelectedItem = item;
                    break;
                }
            }
            _isInitialized = true;
        }

        private void SaveSettings()
        {
            if (_scanRegion.HasValue)
            {
                var regionJson = JsonConvert.SerializeObject(_scanRegion.Value);
                _dbManager.SaveSetting("ScanRegion", regionJson);
            }
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => PerformScan());
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            PerformScan();
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

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (LanguageSelector.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                App.Instance.ChangeLanguage(langCode);
                _dbManager.SaveSetting("Language", langCode);
                
                // Refresh UI to update any code-behind strings if needed
                RefreshDashboard();
            }
        }

        private void RefreshDashboard()
        {
            var currentBalance = _dbManager.GetCurrentBalance();
            var today = DateTime.Today;
            var dailyStats = _dbManager.GetDailyStats(today);

            // Animate balance count-up for premium feel
            var currentText = CurrentBalanceText.Text;
            var currentValue = ValueParser.ParseBalance(currentText);
            if (Math.Abs((double)(currentBalance - currentValue)) > 0.01)
            {
                AnimationHelper.CountUpAnimation(CurrentBalanceText, (double)currentValue, (double)currentBalance);
            }
            else
            {
                CurrentBalanceText.Text = ValueParser.FormatBalance(currentBalance);
            }
            
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
            }

            var worstDay = _dbManager.GetWorstDay();
            if (worstDay != null)
            {
                WorstDayText.Text = ValueParser.FormatProfitLoss(worstDay.ProfitLoss);
            }

            var allScans = _dbManager.GetAllScans();
            TotalScansText.Text = allScans.Count.ToString();

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
                    AnimationsSpeed = TimeSpan.FromMilliseconds(800), // Smooth drawing animation
                    EasingFunction = LiveChartsCore.EasingFunctions.EasingFunctions.ExponentialOut
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
