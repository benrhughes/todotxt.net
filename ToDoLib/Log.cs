using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace ToDoLib
{
    public static class Log
    {
        public static bool Enabled { get; set; }

        public static string LogFile
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                          "Hughesoft", "todotxt.exe", "debug_log.txt");
            }
        }

        public static void Debug(string msg, Exception ex)
        {
            Debug(msg + Environment.NewLine + ex.ToString());
        }

        public static void Debug(string msg)
        {
            if (!Enabled)
                return;

            msg = Environment.NewLine + "[" + DateTime.Now.ToString() + "] " + msg;
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
