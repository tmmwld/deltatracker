using System;
using System.Collections.Generic;
using System.Linq;
using DeltaForceTracker.Database;
using DeltaForceTracker.Models;
using DeltaForceTracker.Utils;

namespace DeltaForceTracker.Services
{
    /// <summary>
    /// Manages achievement unlock logic, daily counters, and notifications.
    /// </summary>
    public class AchievementService
    {
        private readonly DatabaseManager _dbManager;
        private DateTime _lastDateCheck;
        private DateTime? _lastCheaterMarkTime;
        private DateTime? _appLaunchTime;
        private Dictionary<int, DateTime> _lockedAchievementTaps; // achievement ID => last tap time
        private Dictionary<int, int> _lockedAchievementTapCounts; // achievement ID => tap count

        // Events for UI notifications
        public event EventHandler<Achievement>? AchievementUnlocked;

        public AchievementService(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            _lastDateCheck = DateTime.Today;
            _lockedAchievementTaps = new Dictionary<int, DateTime>();
            _lockedAchievementTapCounts = new Dictionary<int, int>();
        }

        /// <summary>
        /// Gets all 21 achievements with full data (titles, descriptions, icons).
        /// </summary>
        public List<Achievement> GetAllAchievements()
        {
            var achievements = _dbManager.GetAllAchievements();
            
            // Populate metadata for each achievement from definitions
            foreach (var ach in achievements)
            {
                PopulateAchievementMetadata(ach);
            }

            return achievements;
        }

        private void PopulateAchievementMetadata(Achievement ach)
        {
            // Achievement definitions from CSV
            switch (ach.Id)
            {
                case 0:
                    ach.TitleEN = "Locked";
                    ach.TitleRU = "Закрыто";
                    ach.DescriptionEN = "Hidden Achievement";
                    ach.DescriptionRU = "Скрытая ачивка";
                    ach.IconFileName = "0_Locked.png";
                    break;
                case 1:
                    ach.TitleEN = "Best User";
                    ach.TitleRU = "Лучший пользователь";
                    ach.DescriptionEN = "Make more than 50 scans total";
                    ach.DescriptionRU = "Сделай больше 50 сканов";
                    ach.IconFileName = "1_Best_User.png";
                    break;
                case 2:
                    ach.TitleEN = "Hodler";
                    ach.TitleRU = "Инвестор";
                    ach.DescriptionEN = "Finish your last 3 active days in profit";
                    ach.DescriptionRU = "Последние 3 игровых дня в плюсе";
                    ach.IconFileName = "2_Hodler.png";
                    break;
                case 3:
                    ach.TitleEN = "Paper Hands";
                    ach.TitleRU = "Просто инфляция";
                    ach.DescriptionEN = "Finish your last 3 active days in loss";
                    ach.DescriptionRU = "Последние 3 игровых дня в минусе";
                    ach.IconFileName = "3_Paper_Hands.png";
                    break;
                case 4:
                    ach.TitleEN = "Bad Start";
                    ach.TitleRU = "Плохой старт";
                    ach.DescriptionEN = "Start the day with loss";
                    ach.DescriptionRU = "Начните день с минуса";
                    ach.IconFileName = "4_Bad_Start.png";
                    break;
                case 5:
                    ach.TitleEN = "Bruteforce";
                    ach.TitleRU = "Брутфорс";
                    ach.DescriptionEN = "Trying harder won't unlock it… or will it?";
                    ach.DescriptionRU = "Думал, что если жать сильнее — откроется?";
                    ach.IconFileName = "5_Bruteforce.png";
                    break;
                case 6:
                    ach.TitleEN = "I Believe!";
                    ach.TitleRU = "Бам!";
                    ach.DescriptionEN = "Gain more than 3M in a single jump";
                    ach.DescriptionRU = "Получите разовый плюс > +3 млн.";
                    ach.IconFileName = "6_I_Believe!.png";
                    break;
                case 7:
                    ach.TitleEN = "Degen Moment";
                    ach.TitleRU = "Упс...";
                    ach.DescriptionEN = "Lose more than 3M in a single drop";
                    ach.DescriptionRU = "Получите разовый минус < −3 млн.";
                    ach.IconFileName = "7_Degen_Moment.png";
                    break;
                case 8:
                    ach.TitleEN = "Just One More";
                    ach.TitleRU = "Ночная каточка";
                    ach.DescriptionEN = "Open the app after midnight.";
                    ach.DescriptionRU = "Откройте приложение после полуночи.";
                    ach.IconFileName = "8_Just_One_More.png";
                    break;
                case 9:
                    ach.TitleEN = "Tilt Resistant";
                    ach.TitleRU = "Тильто-устойчивый";
                    ach.DescriptionEN = "Finish the day without tilting.";
                    ach.DescriptionRU = "Закончите день, ни разу не нажав \"Я сгорел\".";
                    ach.IconFileName = "9_Tilt_Resistant.png";
                    break;
                case 10:
                    ach.TitleEN = "Tilt Lord";
                    ach.TitleRU = "Лорд тильта";
                    ach.DescriptionEN = "Hit \"I'm Tilted\" 20 times total.";
                    ach.DescriptionRU = "Нажмите \"Я сгорел\" 20 раз всего.";
                    ach.IconFileName = "10_Tilt_Lord.png";
                    break;
                case 11:
                    ach.TitleEN = "Not sure";
                    ach.TitleRU = "Передумал..";
                    ach.DescriptionEN = "Met a cheater, then undid it";
                    ach.DescriptionRU = "Отметил читака, затем отменил решение";
                    ach.IconFileName = "11_Not_sure.png";
                    break;
                case 12:
                    ach.TitleEN = "Alt+Tab Warrior";
                    ach.TitleRU = "Alt+Tab воин";
                    ach.DescriptionEN = "Open the app 5 times in one day.";
                    ach.DescriptionRU = "Откройте приложение 5 раз за день.";
                    ach.IconFileName = "12_Alt+Tab_Warrior.png";
                    break;
                case 13:
                    ach.TitleEN = "Confidence Zero";
                    ach.TitleRU = "Уверености нет";
                    ach.DescriptionEN = "Tilt immediately after launching the app.";
                    ach.DescriptionRU = "Нажмите \"Я сгорел\" сразу после запуска.";
                    ach.IconFileName = "13_Confidence_Zero.png";
                    break;
                case 14:
                    ach.TitleEN = "Double Check";
                    ach.TitleRU = "Даблчекер";
                    ach.DescriptionEN = "Make two scans within 3 seconds.";
                    ach.DescriptionRU = "Сделайте два скана в течение 3 секунд.";
                    ach.IconFileName = "14_Double_Check.png";
                    break;
                case 15:
                    ach.TitleEN = "Microflex";
                    ach.TitleRU = "Микроплюсик";
                    ach.DescriptionEN = "Get a tiny balance change ≤100k.";
                    ach.DescriptionRU = "Получите микроизменение ≤100k.";
                    ach.IconFileName = "15_Microflex.png";
                    break;
                case 16:
                    ach.TitleEN = "Reading Enjoyer";
                    ach.TitleRU = "Цитата Энджоер";
                    ach.DescriptionEN = "Read 10 quotes in a single day.";
                    ach.DescriptionRU = "Прочитайте 10 цитат за день.";
                    ach.IconFileName = "16_Reading_Enjoyer.png";
                    break;
                case 17:
                    ach.TitleEN = "C-c-combo";
                    ach.TitleRU = "К-к-комбо";
                    ach.DescriptionEN = "Cheater + Tilt + Red in one day.";
                    ach.DescriptionRU = "Читер + Сгорел + Красная за день.";
                    ach.IconFileName = "17_C_c_combo.png";
                    break;
                case 18:
                    ach.TitleEN = "Paranoia";
                    ach.TitleRU = "Паранойя";
                    ach.DescriptionEN = "Meet 5 cheaters in one day.";
                    ach.DescriptionRU = "Отметьте 5+ читеров за день.";
                    ach.IconFileName = "18_Paranoia.png";
                    break;
                case 19:
                    ach.TitleEN = "Red day";
                    ach.TitleRU = "Красный день";
                    ach.DescriptionEN = "Find 5 Reds in one day.";
                    ach.DescriptionRU = "Нажмите Красная 5+ раз за день.";
                    ach.IconFileName = "19_Red_day.png";
                    break;
                case 20:
                    ach.TitleEN = "WOA, HOA!";
                    ach.TitleRU = "Нашел!";
                    ach.DescriptionEN = "Find the Heart of Africa in the app.";
                    ach.DescriptionRU = "Найдите Сердце Африки в приложении.";
                    ach.IconFileName = "20_WOA_HOA.png";
                    break;
            }
        }

        /// <summary>
        /// Called when the app launches. Checks #8 Just One More (midnight launch).
        /// </summary>
        public void OnAppLaunched(DateTime launchTime)
        {
            _appLaunchTime = launchTime;
            CheckDayChange(launchTime);

            // Increment app opens counter
            _dbManager.IncrementDailyCounter("app_opens_today", launchTime);

            // Check #8: Just One More (launch between 00:00 and 05:00)
            if (launchTime.Hour >= 0 && launchTime.Hour < 5)
            {
                TryUnlockAchievement(8, launchTime);
            }

            // #12: Alt+Tab Warrior condition moved to OnAppActivated
        }

        /// <summary>
        /// Called when the app window is activated (gained focus). 
        /// Tracks #12: Alt+Tab Warrior.
        /// </summary>
        public void OnAppActivated(DateTime timestamp)
        {
            CheckDayChange(timestamp);

            // Increment activation counter
            _dbManager.IncrementDailyCounter("app_activations_today", timestamp);

            // Check #12: Alt+Tab Warrior (5+ activations today)
            var activations = _dbManager.GetDailyCounter("app_activations_today");
            if (activations >= 5)
            {
                TryUnlockAchievement(12, timestamp);
            }
        }

        /// <summary>
        /// Called when a scan is recorded. Checks scan-based achievements.
        /// </summary>
        public void OnScanRecorded(BalanceScan currentScan, BalanceScan? previousScan)
        {
            CheckDayChange(currentScan.Timestamp);

            var totalScans = _dbManager.GetTotalScansCount();

            // #1: Best User (50+ total scans)
            if (totalScans > 50)
            {
                TryUnlockAchievement(1, currentScan.Timestamp);
            }

            // Calculate delta if there's a previous scan
            if (previousScan != null)
            {
                var delta = currentScan.NumericValue - previousScan.NumericValue;

                // #6: I Believe! (+3M jump)
                if (delta > 3_000_000)
                {
                    TryUnlockAchievement(6, currentScan.Timestamp);
                }

                // #7: Degen Moment (-3M drop)
                if (delta < -3_000_000)
                {
                    TryUnlockAchievement(7, currentScan.Timestamp);
                }

                // #15: Microflex (tiny change ≤100k, exclude zero)
                var absDelta = Math.Abs(delta);
                if (absDelta > 0 && absDelta <= 100_000)
                {
                    TryUnlockAchievement(15, currentScan.Timestamp);
                }

                // #14: Double Check (scans within 3 seconds)
                var timeDelta = currentScan.Timestamp - previousScan.Timestamp;
                if (timeDelta.TotalSeconds <= 3)
                {
                    TryUnlockAchievement(14, currentScan.Timestamp);
                }
            }

            // #4: Bad Start (first scan of day has negative balance compared to previous day end)
            var today = currentScan.Timestamp.Date;
            var todayScans = _dbManager.GetScansForDateRange(today, today.AddDays(1).AddTicks(-1));
            
            if (todayScans.Count == 1) // This is the first scan of the day
            {
                // Get yesterday's last scan
                var yesterday = today.AddDays(-1);
                var yesterdayScans = _dbManager.GetScansForDateRange(yesterday, yesterday.AddDays(1).AddTicks(-1));
                
                if (yesterdayScans.Count > 0)
                {
                    var yesterdayEnd = yesterdayScans[yesterdayScans.Count - 1].NumericValue;
                    if (currentScan.NumericValue < yesterdayEnd)
                    {
                        TryUnlockAchievement(4, currentScan.Timestamp);
                    }
                }
            }
        }

        /// <summary>
        /// Called when tilt button is pressed.
        /// </summary>
        public void OnTiltPressed(DateTime timestamp)
        {
            CheckDayChange(timestamp);

            // Increment daily tilt counter
            _dbManager.IncrementDailyCounter("tilts_today", timestamp);

            // #10: Tilt Lord (20+ total tilts)
            var totalTilts = _dbManager.GetTotalTilts();
            if (totalTilts >= 20)
            {
                TryUnlockAchievement(10, timestamp);
            }

            // #13: Confidence Zero (tilt within 5s of app launch)
            if (_appLaunchTime.HasValue)
            {
                var timeSinceLaunch = timestamp - _appLaunchTime.Value;
                if (timeSinceLaunch.TotalSeconds <= 5)
                {
                    TryUnlockAchievement(13, timestamp);
                }
            }

            // Check for #17: C-c-combo (cheater + tilt + red today)
            CheckComboAchievement(timestamp);
        }

        /// <summary>
        /// Called when cheater button is clicked.
        /// </summary>
        public void OnCheaterMarked(DateTime timestamp)
        {
            CheckDayChange(timestamp);
            _lastCheaterMarkTime = timestamp;

            // Increment daily cheater counter
            _dbManager.IncrementDailyCounter("cheaters_today", timestamp);

            var cheatersToday = _dbManager.GetDailyCheaterCount(timestamp.Date);

            // #18: Paranoia (5+ cheaters today)
            if (cheatersToday >= 5)
            {
                TryUnlockAchievement(18, timestamp);
            }

            // Check for #17: C-c-combo
            CheckComboAchievement(timestamp);
        }

        /// <summary>
        /// Called when cheater undo is clicked.
        /// </summary>
        public void OnCheaterUnmarked(DateTime timestamp)
        {
            CheckDayChange(timestamp);

            // #11: Not sure (undo within 5s of marking)
            if (_lastCheaterMarkTime.HasValue)
            {
                var timeDelta = timestamp - _lastCheaterMarkTime.Value;
                if (timeDelta.TotalSeconds <= 5)
                {
                    TryUnlockAchievement(11, timestamp);
                }
            }

            // Decrement counter
            var current = _dbManager.GetDailyCounter("cheaters_today");
            _dbManager.SetDailyCounter("cheaters_today", Math.Max(0, current - 1), timestamp);
        }

        /// <summary>
        /// Called when red button is clicked.
        /// </summary>
        public void OnRedPressed(DateTime timestamp)
        {
            CheckDayChange(timestamp);

            // Increment daily red counter
            _dbManager.IncrementDailyCounter("reds_today", timestamp);

            var redsToday = _dbManager.GetDailyRedItemCount(timestamp.Date);

            // #19: Red day (5+ reds today)
            if (redsToday >= 5)
            {
                TryUnlockAchievement(19, timestamp);
            }

            // Check for #17: C-c-combo
            CheckComboAchievement(timestamp);
        }

        /// <summary>
        /// Called when quote is refreshed.
        /// </summary>
        public void OnQuoteRefreshed(DateTime timestamp)
        {
            CheckDayChange(timestamp);

            // Increment quote counter
            _dbManager.IncrementDailyCounter("quotes_read_today", timestamp);

            var quotesRead = _dbManager.GetDailyCounter("quotes_read_today");

            // #16: Reading Enjoyer (10+ quotes today)
            if (quotesRead >= 10)
            {
                TryUnlockAchievement(16, timestamp);
            }
        }

        /// <summary>
        /// Called when a locked achievement is tapped (for #5 Bruteforce).
        /// </summary>
        public void OnLockedAchievementTapped(int achievementId, DateTime timestamp)
        {
            if (_dbManager.IsAchievementUnlocked(achievementId))
                return; // Already unlocked, ignore

            // Track taps per achievement
            if (!_lockedAchievementTaps.ContainsKey(achievementId))
            {
                _lockedAchievementTaps[achievementId] = timestamp;
                _lockedAchievementTapCounts[achievementId] = 1;
            }
            else
            {
                var lastTap = _lockedAchievementTaps[achievementId];
                var timeSinceLastTap = timestamp - lastTap;

                if (timeSinceLastTap.TotalSeconds <= 5)
                {
                    // Within 5 second window, increment
                    _lockedAchievementTapCounts[achievementId]++;

                    // #5: Bruteforce (5+ taps on same achievement within 5s)
                    if (_lockedAchievementTapCounts[achievementId] >= 5)
                    {
                        TryUnlockAchievement(5, timestamp);
                        _lockedAchievementTapCounts[achievementId] = 0; // Reset
                    }
                }
                else
                {
                    // Outside window, reset
                    _lockedAchievementTapCounts[achievementId] = 1;
                }

                _lockedAchievementTaps[achievementId] = timestamp;
            }
        }

        /// <summary>
        /// Called when easter egg is clicked.
        /// </summary>
        public void OnEasterEggClicked(DateTime timestamp)
        {
            if (!_dbManager.IsEasterEggClicked())
            {
                _dbManager.MarkEasterEggClicked(timestamp);
                TryUnlockAchievement(20, timestamp);
            }
        }

        /// <summary>
        /// Checks if date has changed and resets daily counters if needed.
        /// </summary>
        private void CheckDayChange(DateTime currentTime)
        {
            if (currentTime.Date != _lastDateCheck)
            {
                // New day detected!
                var yesterday = _lastDateCheck;

                // Check #9: Tilt Resistant (yesterday had 0 tilts and >0 scans)
                var yesterdayScans = _dbManager.GetScansForDateRange(yesterday, yesterday.AddDays(1).AddTicks(-1));
                var yesterdayTilts = _dbManager.GetDailyTiltCount(yesterday);

                if (yesterdayScans.Count > 0 && yesterdayTilts == 0)
                {
                    TryUnlockAchievement(9, currentTime);
                }

                // Check #2 & #3: Hodler / Paper Hands (last 3 active days)
                CheckLast3DaysAchievements(currentTime);

                // Reset all daily counters
                _dbManager.ResetAllDailyCounters(currentTime);

                _lastDateCheck = currentTime.Date;
            }
        }

        /// <summary>
        /// Checks #2 Hodler and #3 Paper Hands (last 3 active days all profit/loss).
        /// </summary>
        private void CheckLast3DaysAchievements(DateTime currentTime)
        {
            var dailyPLs = _dbManager.GetLast3ActiveDaysPL();

            if (dailyPLs.Count == 3)
            {
                // #2: Hodler (all 3 days positive)
                if (dailyPLs.All(pl => pl > 0))
                {
                    TryUnlockAchievement(2, currentTime);
                }

                // #3: Paper Hands (all 3 days negative)
                if (dailyPLs.All(pl => pl < 0))
                {
                    TryUnlockAchievement(3, currentTime);
                }
            }
        }

        /// <summary>
        /// Checks #17: C-c-combo (cheater + tilt + red all today).
        /// </summary>
        private void CheckComboAchievement(DateTime timestamp)
        {
            var today = timestamp.Date;
            var cheaters = _dbManager.GetDailyCheaterCount(today);
            var tilts = _dbManager.GetDailyTiltCount(today);
            var reds = _dbManager.GetDailyRedItemCount(today);

            if (cheaters >= 1 && tilts >= 1 && reds >= 1)
            {
                TryUnlockAchievement(17, timestamp);
            }
        }

        /// <summary>
        /// Attempts to unlock an achievement. Does nothing if already unlocked.
        /// Raises AchievementUnlocked event if successful.
        /// </summary>
        private void TryUnlockAchievement(int id, DateTime timestamp)
        {
            if (!_dbManager.IsAchievementUnlocked(id))
            {
                _dbManager.UnlockAchievement(id, timestamp);

                // Get full achievement data and raise event
                var achievement = _dbManager.GetAllAchievements().FirstOrDefault(a => a.Id == id);
                if (achievement != null)
                {
                    PopulateAchievementMetadata(achievement);
                    AchievementUnlocked?.Invoke(this, achievement);
                }
            }
        }

        /// <summary>
        /// Gets global achievement progress (unlocked / total).
        /// </summary>
        public (int unlocked, int total) GetProgress()
        {
            var unlocked = _dbManager.GetUnlockedAchievementCount();
            return (unlocked, 20); // 20 real achievements (ID 1-20, excluding 0)
        }
        /// <summary>
        /// Resets all achievements and counters. FOR DEBUGGING ONLY.
        /// </summary>
        public void ResetAll()
        {
            _dbManager.ResetAllAchievements();
            _lockedAchievementTaps.Clear();
            _lockedAchievementTapCounts.Clear();
        }
    }
}
