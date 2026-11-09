using System;
using System.IO;
using System.Linq;

namespace KYLMA.WinForms
{
    /// <summary>
    /// Writes unexpected errors to %ProgramData%\KYLMA\Logs, one file per day, keeping 30 days.
    /// Connection strings and passwords are never written here.
    /// </summary>
    internal static class AppLogger
    {
        private static readonly object Gate = new object();

        public static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KYLMA", "Logs");

        public static void LogException(string context, Exception ex)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(LogDirectory);
                    var file = Path.Combine(LogDirectory, "kylma-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                    var entry = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + context + "] "
                              + ex.GetType().Name + ": " + ex.Message + Environment.NewLine
                              + ex.StackTrace + Environment.NewLine + Environment.NewLine;
                    File.AppendAllText(file, entry);
                    PurgeOldLogs();
                }
            }
            catch
            {
                // Logging must never crash the application.
            }
        }

        private static void PurgeOldLogs()
        {
            var cutoff = DateTime.Now.AddDays(-30);
            foreach (var file in Directory.GetFiles(LogDirectory, "kylma-*.log")
                                          .Where(f => File.GetLastWriteTime(f) < cutoff))
            {
                File.Delete(file);
            }
        }
    }
}
