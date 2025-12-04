using System;
using System.IO;

namespace DeltaForceTracker.Utils
{
    public static class DiagnosticLogger
    {
        private static string LogPath
        {
            get
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var logDir = Path.Combine(appDataPath, "DeltaForceTracker");
                return Path.Combine(logDir, "crash.log");
            }
        }

        public static void Log(string message)
        {
            try
            {
                var logPath = LogPath;
                var logDir = Path.GetDirectoryName(logPath);
                
                if (!string.IsNullOrEmpty(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var timestamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n";
                File.AppendAllText(logPath, timestamped);
            }
            catch
            {
                // Silent fail - can't log if logging fails
            }
        }

        public static void LogException(string context, Exception? ex)
        {
            if (ex == null)
            {
                Log($"❌ NULL EXCEPTION in {context}");
                return;
            }

            Log($"❌ EXCEPTION in {context}:");
            Log($"   Type: {ex.GetType().FullName}");
            Log($"   Message: {ex.Message}");
            
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                Log($"   StackTrace: {ex.StackTrace}");
            }
            
            if (ex.InnerException != null)
            {
                Log($"   Inner Exception: {ex.InnerException.Message}");
                if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
                {
                    Log($"   Inner StackTrace: {ex.InnerException.StackTrace}");
                }
            }
        }

        public static void ClearLog()
        {
            try
            {
                var logPath = LogPath;
                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                }
            }
            catch
            {
                // Silent fail
            }
        }
    }
}
