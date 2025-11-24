using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeltaForceTracker.Models;
using Microsoft.Data.Sqlite;

namespace DeltaForceTracker.Database
{
    public class DatabaseManager
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DatabaseManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DeltaForceTracker"
            );
            Directory.CreateDirectory(appDataPath);
            
            _dbPath = Path.Combine(appDataPath, "balances.db");
            _connectionString = $"Data Source={_dbPath}";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Scans (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    RawValue TEXT NOT NULL,
                    NumericValue REAL NOT NULL,
                    DailyStartingBalance REAL NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );
            ";
            command.ExecuteNonQuery();
        }

        public void RecordScan(DateTime timestamp, string rawValue, decimal numericValue)
        {
            var dailyStart = GetDailyStartingBalance(timestamp.Date);
            if (dailyStart == 0)
            {
                dailyStart = numericValue;
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Scans (Timestamp, RawValue, NumericValue, DailyStartingBalance)
                VALUES ($timestamp, $rawValue, $numericValue, $dailyStart)
            ";
            command.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));
            command.Parameters.AddWithValue("$rawValue", rawValue);
            command.Parameters.AddWithValue("$numericValue", (double)numericValue);
            command.Parameters.AddWithValue("$dailyStart", (double)dailyStart);
            command.ExecuteNonQuery();
        }

        public decimal GetDailyStartingBalance(DateTime date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT NumericValue 
                FROM Scans 
                WHERE DATE(Timestamp) = DATE($date)
                ORDER BY Timestamp ASC
                LIMIT 1
            ";
            command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToDecimal(result) : 0;
        }

        public decimal GetCurrentBalance()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT NumericValue 
                FROM Scans 
                ORDER BY Timestamp DESC
                LIMIT 1
            ";

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToDecimal(result) : 0;
        }

        public List<BalanceScan> GetScansForDateRange(DateTime start, DateTime end)
        {
            var scans = new List<BalanceScan>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Timestamp, RawValue, NumericValue, DailyStartingBalance
                FROM Scans
                WHERE Timestamp BETWEEN $start AND $end
                ORDER BY Timestamp ASC
            ";
            command.Parameters.AddWithValue("$start", start.ToString("o"));
            command.Parameters.AddWithValue("$end", end.ToString("o"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                scans.Add(new BalanceScan
                {
                    Id = reader.GetInt32(0),
                    Timestamp = DateTime.Parse(reader.GetString(1)),
                    RawValue = reader.GetString(2),
                    NumericValue = Convert.ToDecimal(reader.GetDouble(3)),
                    DailyStartingBalance = Convert.ToDecimal(reader.GetDouble(4))
                });
            }

            return scans;
        }

        public List<BalanceScan> GetAllScans()
        {
            return GetScansForDateRange(DateTime.MinValue, DateTime.MaxValue);
        }

        public List<BalanceScan> GetRecentScans(int count)
        {
            var scans = new List<BalanceScan>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Timestamp, RawValue, NumericValue, DailyStartingBalance
                FROM Scans
                ORDER BY Timestamp DESC
                LIMIT $count
            ";
            command.Parameters.AddWithValue("$count", count);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                scans.Add(new BalanceScan
                {
                    Id = reader.GetInt32(0),
                    Timestamp = DateTime.Parse(reader.GetString(1)),
                    RawValue = reader.GetString(2),
                    NumericValue = Convert.ToDecimal(reader.GetDouble(3)),
                    DailyStartingBalance = Convert.ToDecimal(reader.GetDouble(4))
                });
            }

            scans.Reverse();
            return scans;
        }

        public DailyStats GetDailyStats(DateTime date)
        {
            var scans = GetScansForDateRange(
                date.Date, 
                date.Date.AddDays(1).AddTicks(-1)
            );

            if (scans.Count == 0)
            {
                return new DailyStats
                {
                    Date = date.Date,
                    StartingBalance = 0,
                    CurrentBalance = 0,
                    ScanCount = 0
                };
            }

            return new DailyStats
            {
                Date = date.Date,
                StartingBalance = scans[0].NumericValue,
                CurrentBalance = scans[^1].NumericValue,
                ScanCount = scans.Count,
                HighestBalance = scans.Max(s => s.NumericValue),
                LowestBalance = scans.Min(s => s.NumericValue)
            };
        }

        public decimal GetHighestBalanceEver()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MAX(NumericValue) FROM Scans";

            var result = command.ExecuteScalar();
            return result != DBNull.Value && result != null ? Convert.ToDecimal(result) : 0;
        }

        public DailyStats? GetBestDay()
        {
            var allScans = GetAllScans();
            if (allScans.Count == 0) return null;

            var dailyGroups = allScans.GroupBy(s => s.Timestamp.Date);
            
            DailyStats? bestDay = null;
            decimal maxPL = decimal.MinValue;

            foreach (var group in dailyGroups)
            {
                var stats = GetDailyStats(group.Key);
                if (stats.ProfitLoss > maxPL)
                {
                    maxPL = stats.ProfitLoss;
                    bestDay = stats;
                }
            }

            return bestDay;
        }

        public DailyStats? GetWorstDay()
        {
            var allScans = GetAllScans();
            if (allScans.Count == 0) return null;

            var dailyGroups = allScans.GroupBy(s => s.Timestamp.Date);
            
            DailyStats? worstDay = null;
            decimal minPL = decimal.MaxValue;

            foreach (var group in dailyGroups)
            {
                var stats = GetDailyStats(group.Key);
                if (stats.ProfitLoss < minPL)
                {
                    minPL = stats.ProfitLoss;
                    worstDay = stats;
                }
            }

            return worstDay;
        }

        public void SaveSetting(string key, string value)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Settings (Key, Value)
                VALUES ($key, $value)
            ";
            command.Parameters.AddWithValue("$key", key);
            command.Parameters.AddWithValue("$value", value);
            command.ExecuteNonQuery();
        }

        public string? GetSetting(string key)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE Key = $key";
            command.Parameters.AddWithValue("$key", key);

            var result = command.ExecuteScalar();
            return result?.ToString();
        }

        public Dictionary<string, string> GetAllSettings()
        {
            var settings = new Dictionary<string, string>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Key, Value FROM Settings";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                settings[reader.GetString(0)] = reader.GetString(1);
            }

            return settings;
        }
        public void ClearAllData()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                
                // Clear scans table
                command.CommandText = "DELETE FROM Scans";
                command.ExecuteNonQuery();

                // Reset sequence for Scans table
                command.CommandText = "DELETE FROM sqlite_sequence WHERE name='Scans'";
                command.ExecuteNonQuery();

                // Optionally clear settings or keep them? 
                // User asked to "clear analytics", usually implies keeping settings like Hotkey/Region.
                // I will keep settings for now as it's more convenient.

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
