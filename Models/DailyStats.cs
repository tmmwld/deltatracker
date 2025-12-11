using System;

namespace DeltaForceTracker.Models
{
    public class DailyStats
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal StartBalance { get; set; }
        public decimal EndBalance { get; set; }
        public decimal ProfitLoss { get; set; }
        public int Tilts { get; set; }
        public int Cheaters { get; set; }
        public int RedItems { get; set; }
        public decimal HighestBalance { get; set; }
        public decimal LowestBalance { get; set; }
    }
}
