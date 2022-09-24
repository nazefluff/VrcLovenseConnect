using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VrcLovenseConnect.Helpers
{
    internal static class Logger
    {
        private static FileStream? log;
        private static StreamWriter? logWriter;
        private static bool disposedValue;
        private static int debugLogs = 0;
        private static int maxDebugLogs = 10000;

        public static StreamWriter? LogWriter { get => logWriter; set => logWriter = value; }

        public static void OpenLog(string path)
        {
            log = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            LogWriter = new StreamWriter(log);
            LogWriter.AutoFlush = true;
        }

        public static void LogException(Exception ex)
        {
            LogWriter?.WriteLine($"[{DateTime.Now}] ERROR: {ex.Message}");

            if (ex.StackTrace != null)
                LogWriter?.WriteLine(ex.StackTrace);
        }

#if DEBUG
        public static void LogDebugInfo(string log)
        {
            if (debugLogs < maxDebugLogs)
            {
                LogWriter?.WriteLine($"[{DateTime.Now}] DEBUG: {log}");
                debugLogs++;
            }
        }
#endif

        private static void CloseLog(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    LogWriter?.Dispose();
                    log?.Dispose();
                }

                disposedValue = true;
            }
        }

        public static void CloseLog()
        {
            CloseLog(true);
        }
    }
}
