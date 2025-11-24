using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DeltaForceTracker.Utils
{
    public static class ValueParser
    {
        public static decimal ParseBalanceString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be empty");

            // Normalize the input
            // 1. Remove spaces (e.g. "142 5 k" -> "1425k")
            // 2. Replace comma with dot
            // 3. ToUpper
            input = input.Replace(" ", "").Replace(',', '.').ToUpperInvariant();
            
            // Extract number and suffix
            var match = Regex.Match(input, @"([\d.]+)([KMКМкм])?");
            
            if (!match.Success)
                throw new FormatException($"Cannot parse balance: {input}");

            var numberPart = match.Groups[1].Value;
            var suffixPart = match.Groups[2].Value;

            if (!decimal.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal baseValue))
                throw new FormatException($"Invalid number format: {numberPart}");

            // Apply multiplier based on suffix
            decimal multiplier = suffixPart switch
            {
                "K" or "К" or "к" => 1_000m,
                "M" or "М" or "м" => 1_000_000m,
                "" => 1m,
                _ => throw new FormatException($"Unknown suffix: {suffixPart}")
            };

            // Heuristic: If suffix is K and value >= 1000, it's likely a missing decimal point.
            // Game typically switches to M at 1000K.
            // Example: "1425K" (OCR error for 142.5K) -> 142.5K
            if (multiplier == 1_000m && baseValue >= 1_000m)
            {
                while (baseValue >= 1_000m)
                {
                    baseValue /= 10m;
                }
            }

            return baseValue * multiplier;
        }

        public static string FormatBalance(decimal value)
        {
            if (value >= 1_000_000)
            {
                return $"{value / 1_000_000:F2}M";
            }
            else if (value >= 1_000)
            {
                return $"{value / 1_000:F2}K";
            }
            else
            {
                return value.ToString("F2");
            }
        }

        public static string FormatProfitLoss(decimal value)
        {
            var prefix = value >= 0 ? "+" : "";
            return $"{prefix}{FormatBalance(value)}";
        }
    }
}
