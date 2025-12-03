using System;

namespace DeltaForceTracker.Models
{
    public class ScanHistoryViewModel
    {
        public DateTime Timestamp { get; set; }
        public string RawValue { get; set; } = string.Empty;
        public decimal NumericValue { get; set; }
        public decimal? Delta { get; set; }
        public string DeltaText { get; set; } = string.Empty;
        public string DeltaIndicator { get; set; } = string.Empty;
        public string DeltaColor { get; set; } = "#94A3B8"; // Default gray
    }
}
