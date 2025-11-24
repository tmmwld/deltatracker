using System;

namespace DeltaForceTracker.Models
{
    public class DailyStats
    {
        public DateTime Date { get; set; }
        public decimal StartingBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal ProfitLoss => CurrentBalance - StartingBalance;
        public int ScanCount { get; set; }
        public decimal HighestBalance { get; set; }
        public decimal LowestBalance { get; set; }
    }
}
