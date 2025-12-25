using System;

namespace DeltaForceTracker.Models
{
    /// <summary>
    /// Represents a single achievement that can be unlocked by the user.
    /// </summary>
    public class Achievement
    {
        public int Id { get; set; }
        public string TitleEN { get; set; } = string.Empty;
        public string TitleRU { get; set; } = string.Empty;
        public string DescriptionEN { get; set; } = string.Empty;
        public string DescriptionRU { get; set; } = string.Empty;
        public string IconFileName { get; set; } = "0_Locked.png";
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }

        /// <summary>
        /// Gets the localized title based on current app language.
        /// </summary>
        public string GetLocalizedTitle(string language)
        {
            return language == "ru" ? TitleRU : TitleEN;
        }

        /// <summary>
        /// Gets the localized description based on current app language.
        /// </summary>
        public string GetLocalizedDescription(string language)
        {
            return language == "ru" ? DescriptionRU : DescriptionEN;
        }

        /// <summary>
        /// Gets the display icon path (locked or unlocked).
        /// </summary>
        public string GetIconPath()
        {
            if (!IsUnlocked)
                return "/Resources/achievements/0_Locked.png";
            
            return $"/Resources/achievements/{IconFileName}";
        }

        /// <summary>
        /// Gets display title (locked or actual title).
        /// </summary>
        public string GetDisplayTitle(string language)
        {
            if (!IsUnlocked)
                return language == "ru" ? "Закрыто" : "Locked";
            
            return GetLocalizedTitle(language);
        }
    }
}
