using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using DeltaForceTracker.Utils;
using Tesseract;

namespace DeltaForceTracker.OCR
{
    public class TesseractOCREngine : IDisposable
    {
        private TesseractEngine? _engine;
        private readonly string _tessDataPath;
        private bool _isInitialized = false;

        public TesseractOCREngine()
        {
            // Tesseract data will be in tessdata folder relative to exe
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            _tessDataPath = Path.Combine(exeDir, "tessdata");
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Initialize with English and Russian language support
                _engine = new TesseractEngine(_tessDataPath, "eng+rus", EngineMode.Default);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize Tesseract OCR. Make sure tessdata folder exists with eng.traineddata and rus.traineddata files. Error: {ex.Message}");
            }
        }

        public (bool success, string rawValue, decimal numericValue, string error) ExtractBalanceFromRegion(Bitmap screenshot)
        {
            if (!_isInitialized || _engine == null)
            {
                return (false, "", 0, "OCR engine not initialized");
            }

            try
            {
                using var pix = PixConverter.ToPix(screenshot);
                using var page = _engine.Process(pix);
                var text = page.GetText();

                // Find the balance label and extract value
                var (found, rawValue, numericValue) = FindAndExtractBalance(text);

                if (found)
                {
                    return (true, rawValue, numericValue, "");
                }
                else
                {
                    return (false, "", 0, "Could not find 'Total Assets' or 'Общие активы' in the scanned region");
                }
            }
            catch (Exception ex)
            {
                return (false, "", 0, $"OCR error: {ex.Message}");
            }
        }

        private (bool found, string rawValue, decimal numericValue) FindAndExtractBalance(string ocrText)
        {
            // Try to find English label
            var englishPattern = @"Total\s+Assets[:\s]+([0-9.,]+\s*[KMКМкм]?)";
            var englishMatch = Regex.Match(ocrText, englishPattern, RegexOptions.IgnoreCase);

            if (englishMatch.Success)
            {
                var rawValue = englishMatch.Groups[1].Value.Trim();
                try
                {
                    var numericValue = ValueParser.ParseBalanceString(rawValue);
                    return (true, rawValue, numericValue);
                }
                catch
                {
                    // Failed to parse, continue
                }
            }

            // Try to find Russian label
            var russianPattern = @"Общие\s+активы[:\s]+([0-9.,]+\s*[KMКМкм]?)";
            var russianMatch = Regex.Match(ocrText, russianPattern, RegexOptions.IgnoreCase);

            if (russianMatch.Success)
            {
                var rawValue = russianMatch.Groups[1].Value.Trim();
                try
                {
                    var numericValue = ValueParser.ParseBalanceString(rawValue);
                    return (true, rawValue, numericValue);
                }
                catch
                {
                    // Failed to parse
                }
            }

            // Fallback: try to find any number with K/M suffix
            var fallbackPattern = @"([0-9.,]+\s*[MМм])";
            var fallbackMatch = Regex.Match(ocrText, fallbackPattern);

            if (fallbackMatch.Success)
            {
                var rawValue = fallbackMatch.Groups[1].Value.Trim();
                try
                {
                    var numericValue = ValueParser.ParseBalanceString(rawValue);
                    return (true, rawValue, numericValue);
                }
                catch
                {
                    // Failed to parse
                }
            }

            return (false, "", 0);
        }

        public void Dispose()
        {
            _engine?.Dispose();
            _isInitialized = false;
        }
    }
}
