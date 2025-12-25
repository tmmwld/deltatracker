using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeltaForceTracker.Models;
using Microsoft.Data.Sqlite;

namespace DeltaForceTracker.Database
{
    public class DatabaseManager : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _dbPath;
        private bool _disposed = false;

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

                CREATE TABLE IF NOT EXISTS TiltEvents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS CheaterEvents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS RedItemEvents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Achievements (
                    Id INTEGER PRIMARY KEY,
                    IsUnlocked INTEGER DEFAULT 0,
                    UnlockedAt TEXT
                );

                CREATE TABLE IF NOT EXISTS DailyCounters (
                    Key TEXT PRIMARY KEY,
                    Value INTEGER DEFAULT 0,
                    LastUpdated TEXT
                );

                CREATE TABLE IF NOT EXISTS EasterEgg (
                    Id INTEGER PRIMARY KEY DEFAULT 1,
                    IsClicked INTEGER DEFAULT 0,
                    ClickedAt TEXT
                );
            ";
            command.ExecuteNonQuery();

            // Initialize achievements if table is empty (first run or upgrade from v1.x)
            InitializeAchievementsData(connection);
        }

        private void InitializeAchievementsData(SqliteConnection connection)
        {
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM Achievements";
            var count = Convert.ToInt32(checkCommand.ExecuteScalar());

            // Only initialize if table is empty (first run)
            if (count == 0)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO Achievements (Id, IsUnlocked, UnlockedAt)
                    VALUES ($id, 0, NULL)
                ";

                // Create all 21 achievements as locked (IDs 0-20)
                for (int i = 0; i <= 20; i++)
                {
                    insertCommand.Parameters.Clear();
                    insertCommand.Parameters.AddWithValue("$id", i);
                    insertCommand.ExecuteNonQuery();
                }
            }

            // Initialize easter egg state if empty
            var eggCheckCommand = connection.CreateCommand();
            eggCheckCommand.CommandText = "SELECT COUNT(*) FROM EasterEgg";
            var eggCount = Convert.ToInt32(eggCheckCommand.ExecuteScalar());

            if (eggCount == 0)
            {
                var eggInsertCommand = connection.CreateCommand();
                eggInsertCommand.CommandText = "INSERT INTO EasterEgg (Id, IsClicked, ClickedAt) VALUES (1, 0, NULL)";
                eggInsertCommand.ExecuteNonQuery();
            }
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
                    StartBalance = 0,
                    EndBalance = 0,
                    ProfitLoss = 0,
                    HighestBalance = 0,
                    LowestBalance = 0
                };
            }

            var startBalance = scans[0].NumericValue;
            var endBalance = scans[^1].NumericValue;

            return new DailyStats
            {
                Date = date.Date,
                StartBalance = startBalance,
                EndBalance = endBalance,
                ProfitLoss = endBalance - startBalance,
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

        // Tilt Event Methods
        public void RecordTilt(DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TiltEvents (Timestamp)
                VALUES ($timestamp)
            ";
            command.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));
            command.ExecuteNonQuery();
        }

        public int GetTiltCount()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM TiltEvents";
            
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public int GetDailyTiltCount(DateTime date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM TiltEvents 
                WHERE DATE(Timestamp) = DATE($date)
            ";
            command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public int GetTotalTilts()
        {
            return GetTiltCount(); // Reuse existing method
        }

        public List<TiltEvent> GetTiltsForDateRange(DateTime start, DateTime end)
        {
            var tilts = new List<TiltEvent>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Timestamp
                FROM TiltEvents
                WHERE Timestamp BETWEEN $start AND $end
                ORDER BY Timestamp ASC
            ";
            command.Parameters.AddWithValue("$start", start.ToString("o"));
            command.Parameters.AddWithValue("$end", end.ToString("o"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tilts.Add(new TiltEvent
                {
                    Id = reader.GetInt32(0),
                    Timestamp = DateTime.Parse(reader.GetString(1))
                });
            }

            return tilts;
        }

        // Cheater Event Methods
        public void RecordCheater(DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO CheaterEvents (Timestamp)
                VALUES ($timestamp)
            ";
            command.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));
            command.ExecuteNonQuery();
        }

        public void UndoCheater(DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM CheaterEvents
                WHERE Id = (
                    SELECT Id FROM CheaterEvents
                    WHERE DATE(Timestamp) = DATE($date)
                    ORDER BY Timestamp DESC
                    LIMIT 1
                )
            ";
            command.Parameters.AddWithValue("$date", timestamp.ToString("yyyy-MM-dd"));
            command.ExecuteNonQuery();
        }

        public int GetTotalCheaters()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM CheaterEvents";
            
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public int GetDailyCheaterCount(DateTime date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM CheaterEvents 
                WHERE DATE(Timestamp) = DATE($date)
            ";
            command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        // Red Item Event Methods
        public void RecordRedItem(DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO RedItemEvents (Timestamp)
                VALUES ($timestamp)
            ";
            command.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));
            command.ExecuteNonQuery();
        }

        public void UndoRedItem(DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM RedItemEvents
                WHERE Id = (
                    SELECT Id FROM RedItemEvents
                    WHERE DATE(Timestamp) = DATE($date)
                    ORDER BY Timestamp DESC
                    LIMIT 1
                )
            ";
            command.Parameters.AddWithValue("$date", timestamp.ToString("yyyy-MM-dd"));
            command.ExecuteNonQuery();
        }

        public int GetTotalRedItems()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM RedItemEvents";
            
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public int GetDailyRedItemCount(DateTime date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM RedItemEvents 
                WHERE DATE(Timestamp) = DATE($date)
            ";
            command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        // Combined counter stats for Best Day
        public (int cheaters, int tilts, int reds) GetCountersForDate(DateTime date)
        {
            return (
                GetDailyCheaterCount(date),
                GetDailyTiltCount(date),
                GetDailyRedItemCount(date)
            );
        }

        // Achievement Methods
        public List<Achievement> GetAllAchievements()
        {
            var achievements = new List<Achievement>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, IsUnlocked, UnlockedAt
                FROM Achievements
                ORDER BY Id ASC
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                achievements.Add(new Achievement
                {
                    Id = reader.GetInt32(0),
                    IsUnlocked = reader.GetInt32(1) == 1,
                    UnlockedAt = reader.IsDBNull(2) ? null : DateTime.Parse(reader.GetString(2))
                });
            }

            return achievements;
        }

        public void UnlockAchievement(int id, DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Achievements
                SET IsUnlocked = 1, UnlockedAt = $timestamp
                WHERE Id = $id AND IsUnlocked = 0
            ";
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));
            command.ExecuteNonQuery();
        }

        public bool IsAchievementUnlocked(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT IsUnlocked FROM Achievements WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);

            var result = command.ExecuteScalar();
            return result != null && Convert.ToInt32(result) == 1;
        }

        public int GetUnlockedAchievementCount()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Achievements WHERE IsUnlocked = 1";

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        // Daily Counter Methods
        public void SetDailyCounter(string key, int value, DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO DailyCounters (Key, Value, LastUpdated)
                VALUES ($key, $value, $timestamp)
            ";
            command.Parameters.AddWithValue("$key", key);
            command.Parameters.AddWithValue("$value", value);
            command.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));
            command.ExecuteNonQuery();
        }

        public int GetDailyCounter(string key)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM DailyCounters WHERE Key = $key";
            command.Parameters.AddWithValue("$key", key);

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public void IncrementDailyCounter(string key, DateTime timestamp)
        {
            var current = GetDailyCounter(key);
            SetDailyCounter(key, current + 1, timestamp);
        }

        public void ResetAllDailyCounters(DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE DailyCounters 
                SET Value = 0, LastUpdated = $timestamp
            ";
            command.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));
            command.ExecuteNonQuery();
        }

        // Easter Egg Methods
        public bool IsEasterEggClicked()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT IsClicked FROM EasterEgg WHERE Id = 1";

            var result = command.ExecuteScalar();
            return result != null && Convert.ToInt32(result) == 1;
        }

        public void MarkEasterEggClicked(DateTime timestamp)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE EasterEgg
                SET IsClicked = 1, ClickedAt = $timestamp
                WHERE Id = 1
            ";
            command.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Close all SQLite connections for this database
                    // Note: We use 'using' statements in each method, so connections should already be closed
                    // This is a final cleanup to ensure no lingering connections
                    SqliteConnection.ClearPool(new SqliteConnection(_connectionString));
                }
                _disposed = true;
            }
        }
    }
}
