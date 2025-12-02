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
            System.Diagnostics.Debug.WriteLine($"App.ChangeLanguage called with: {cultureCode}");
            
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

            System.Diagnostics.Debug.WriteLine($"Loading dictionary: {dict.Source}");

            // Find and remove the old language dictionary
            var oldDict = Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));
            if (oldDict != null)
            {
                Resources.MergedDictionaries.Remove(oldDict);
                System.Diagnostics.Debug.WriteLine($"Removed old dictionary: {oldDict.Source}");
            }

            // Add the new one
            Resources.MergedDictionaries.Add(dict);
            System.Diagnostics.Debug.WriteLine($"Added new dictionary. Total dictionaries: {Resources.MergedDictionaries.Count}");
            
            // Force UI to update all DynamicResource bindings
            if (MainWindow != null)
            {
                MainWindow.Language = System.Windows.Markup.XmlLanguage.GetLanguage(
                    cultureCode == "ru" ? "ru-RU" : "en-US"
                );
                System.Diagnostics.Debug.WriteLine($"✓ MainWindow.Language set to {MainWindow.Language}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✗ MainWindow is null!");
            }
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
