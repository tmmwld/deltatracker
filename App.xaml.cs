using System.Linq; // Added for FirstOrDefault
using System.Windows;

namespace DeltaForceTracker
{
    public partial class App : Application
    {
        public static App Instance => (App)Current;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Load default language (English) or saved preference
            // For now default to English, we can load from settings later
            ChangeLanguage("en");
            
            // Handle global exceptions
            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show(
                    $"An error occurred: {args.Exception.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                args.Handled = true;
            };
        }

        public void ChangeLanguage(string cultureCode)
        {
            var dict = new ResourceDictionary();
            switch (cultureCode)
            {
                case "ru":
                    dict.Source = new System.Uri("Resources/Languages/Strings.ru.xaml", System.UriKind.Relative);
                    break;
                case "en":
                default:
                    dict.Source = new System.Uri("Resources/Languages/Strings.en.xaml", System.UriKind.Relative);
                    break;
            }

            // Find and remove the old language dictionary
            var oldDict = Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));
            if (oldDict != null)
            {
                Resources.MergedDictionaries.Remove(oldDict);
            }

            // Add the new one
            Resources.MergedDictionaries.Add(dict);
        }

        public string GetString(string key)
        {
            if (Resources.Contains(key))
            {
                return Resources[key] as string ?? key;
            }
            return key;
        }
    }
}
