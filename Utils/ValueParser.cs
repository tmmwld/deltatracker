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
            input = input.Trim().ToUpperInvariant();
            
            // Replace Russian comma with period
            input = input.Replace(',', '.');
            
            // Extract number and suffix
            var match = Regex.Match(input, @"([\d.]+)\s*([KMКМкм])?");
            
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
