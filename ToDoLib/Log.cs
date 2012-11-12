using System;
using System.IO;
using System.Text;
using System.Threading;
using CommonExtensions;

namespace ToDoLib
{
    public enum LogLevel
    {
        Error,
        Debug
    }

    public static class Log
    {
        public static LogLevel LogLevel { get; set; }

        public static string LogFile
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                          "Hughesoft", "todotxt.exe", "log.txt");
            }
        }

        public static void Debug(string msg, params string[] values)
        {
            if (LogLevel == LogLevel.Debug)
                Write(msg, values);
        }

        public static void Error(string msg, Exception ex)
        {
            Error(msg + Environment.NewLine + ex.ToString());
        }

        public static void Error(string msg, params string[] values)
        {
            if (LogLevel == LogLevel.Debug || LogLevel == LogLevel.Error)
                Write(msg, values);
        }

        private static void Write(string msg, params string[] values)
        {
            if (!values.IsNullOrEmpty())
                msg = string.Format(msg, values);

            msg = "[" + DateTime.Now.ToString() + "] " + msg + Environment.NewLine + Environment.NewLine;
         
            var logFileDir = Path.GetDirectoryName(LogFile);

            if (!Directory.Exists(logFileDir))
                Directory.CreateDirectory(logFileDir);

            // perhaps a little heavy handed, but we want to make sure no other instances of todotxt.net are
            // writing to the log file at the same time
            using (var m = new Mutex(true, LogFile.Replace(Path.DirectorySeparatorChar, '_')))
            {
                if (m.WaitOne(10000))
                    File.AppendAllText(LogFile, msg, Encoding.UTF8);
                else
                    throw new Exception("Could not obtain lock on " + LogFile);

                m.ReleaseMutex();
            }
        }
    }
}
