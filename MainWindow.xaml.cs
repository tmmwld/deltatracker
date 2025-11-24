using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
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

        public MainWindow()
        {
            InitializeComponent();
            
            _dbManager = new DatabaseManager();
            _ocrEngine = new TesseractOCREngine();
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
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
                    UpdateStatus($"Hotkey {hotkeyString} registered successfully");
                }
                else
                {
                    UpdateStatus($"Failed to register hotkey {hotkeyString}");
                }
                
                // Load initial data
                RefreshDashboard();
                RefreshAnalytics();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Initialization error: {ex.Message}\n\nMake sure tessdata folder with eng.traineddata and rus.traineddata exists in the application directory.",
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
            UpdateStatus("Scanning...");

            try
            {
                Bitmap screenshot;
                
                if (_scanRegion.HasValue)
                {
                    screenshot = ScreenCapture.CaptureRegion(_scanRegion.Value);
                }
                else
                {
                    // Use full screen if no region selected
                    screenshot = ScreenCapture.CaptureFullScreen();
                }

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
                UpdateStatus($"OCR region updated: {_scanRegion.Value.Width}x{_scanRegion.Value.Height}");
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
                            "Failed to register new hotkey. It might be in use by another application.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                }
            }
        }

        private void RefreshDashboard()
        {
            var currentBalance = _dbManager.GetCurrentBalance();
            var today = DateTime.Today;
            var dailyStats = _dbManager.GetDailyStats(today);

            CurrentBalanceText.Text = ValueParser.FormatBalance(currentBalance);
            
            var pl = dailyStats.ProfitLoss;
            DailyPLText.Text = ValueParser.FormatProfitLoss(pl);
            DailyPLText.Foreground = pl >= 0 
                ? (SolidColorBrush)FindResource("SuccessBrush")
                : (SolidColorBrush)FindResource("ErrorBrush");

            var lastScan = _dbManager.GetRecentScans(1).FirstOrDefault();
            if (lastScan != null)
            {
                LastScanText.Text = $"Last scan: {lastScan.Timestamp:yyyy-MM-dd HH:mm:ss}";
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

            var series = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Values = values,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.Cyan) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(SKColors.Cyan) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(SKColors.DarkBlue)
                }
            };

            BalanceChart.Series = series;
            BalanceChart.XAxes = new[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("MM/dd HH:mm"),
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    TextSize = 12
                }
            };
            BalanceChart.YAxes = new[]
            {
                new Axis
                {
                    Labeler = value => ValueParser.FormatBalance((decimal)value),
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    TextSize = 12
                }
            };
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }
    }
}
