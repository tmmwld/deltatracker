using System.Linq; // Added for FirstOrDefault
using System.Windows;
using DeltaForceTracker.Utils;

namespace DeltaForceTracker
{
    public partial class App : Application
    {
        public static App Instance => (App)Current;

        public App()
        {
            DiagnosticLogger.ClearLog(); // Start fresh
            DiagnosticLogger.Log("=== APP CONSTRUCTOR START ===");
            
            try
            {
                InitializeComponent();
                DiagnosticLogger.Log("✓ InitializeComponent() completed");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogException("App.InitializeComponent", ex);
                throw;
            }
            
            DiagnosticLogger.Log("=== APP CONSTRUCTOR END ===");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            DiagnosticLogger.Log("=== ONSTARTUP START ===");
            
            // Global unhandled exception handlers
            AppDomainUnhandledExceptionHandler();
            DispatcherUnhandledExceptionHandler();
            
            try
            {
                base.OnStartup(e);
                DiagnosticLogger.Log("✓ base.OnStartup completed");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogException("base.OnStartup", ex);
                throw;
            }
            
            try
            {
                // Load default language (English) or saved preference
                ChangeLanguage("en");
                DiagnosticLogger.Log("✓ ChangeLanguage completed");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogException("ChangeLanguage", ex);
                throw;
            }
            
            DiagnosticLogger.Log("=== ONSTARTUP END ===");
        }

        private void AppDomainUnhandledExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                DiagnosticLogger.LogException("AppDomain.UnhandledException", ex);
                DiagnosticLogger.Log($"IsTerminating: {args.IsTerminating}");
            };
        }

        private void DispatcherUnhandledExceptionHandler()
        {
            DispatcherUnhandledException += (s, args) =>
            {
                DiagnosticLogger.LogException("Dispatcher.UnhandledException", args.Exception);
                
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var logPath = System.IO.Path.Combine(appData, "DeltaForceTracker", "crash.log");
                
                MessageBox.Show(
                    $"Application startup error:\n\n{args.Exception.Message}\n\nLog file:\n{logPath}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                
                args.Handled = true; // Prevent crash
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
