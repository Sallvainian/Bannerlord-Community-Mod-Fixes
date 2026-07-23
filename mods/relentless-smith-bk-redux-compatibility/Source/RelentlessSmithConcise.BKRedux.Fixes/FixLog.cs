using System;
using System.IO;

namespace RelentlessSmithConciseBKReduxFixes
{
    internal static class FixLog
    {
        private static readonly object Sync = new object();
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mount and Blade II Bannerlord",
            "Logs",
            "RelentlessSmithConcise.BKRedux.Fixes.log");

        internal static void Info(string message)
        {
            Write("INFO", message);
        }

        internal static void Warn(string message)
        {
            Write("WARN", message);
        }

        internal static void Error(string message, Exception exception = null)
        {
            Write("ERROR", exception == null ? message : message + Environment.NewLine + exception);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                    File.AppendAllText(
                        LogPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Diagnostics must never interfere with the game.
            }
        }
    }
}
