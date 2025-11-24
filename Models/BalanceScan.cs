using System;

namespace DeltaForceTracker.Models
{
    public class BalanceScan
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string RawValue { get; set; } = string.Empty;
        public decimal NumericValue { get; set; }
        public decimal DailyStartingBalance { get; set; }
    }
}
