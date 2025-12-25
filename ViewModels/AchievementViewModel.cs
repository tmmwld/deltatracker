using System;
using DeltaForceTracker.Models;

namespace DeltaForceTracker.ViewModels
{
    public class AchievementViewModel
    {
        public Achievement Model { get; }
        public string DisplayTitle { get; private set; } = string.Empty;
        public string DisplayDescription { get; private set; } = string.Empty;
        public string IconPath { get; private set; } = string.Empty;
        public bool IsUnlocked => Model.IsUnlocked;
        public double Opacity => IsUnlocked ? 1.0 : 0.5;

        public AchievementViewModel(Achievement model, string language)
        {
            Model = model;
            UpdateLanguage(language);
        }

        public void UpdateLanguage(string language)
        {
            DisplayTitle = Model.GetDisplayTitle(language);
            IconPath = Model.GetIconPath();

            if (Model.IsUnlocked)
            {
                DisplayDescription = Model.GetLocalizedDescription(language);
            }
            else
            {
                // Optionally hide description or show hint for locked
                DisplayDescription = language == "ru" ? "???" : "???"; 
            }
        }
    }
}
