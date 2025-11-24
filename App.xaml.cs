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
    }
}
