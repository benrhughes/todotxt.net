using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using ToDoLib;

namespace Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static void MigrateUserSettings()
		{
			// migrate the user settings from the previous version, if necessary
			if (User.Default.FirstRun)
			{
				User.Default.Upgrade();
				User.Default.FirstRun = false;

				if (IsXP())
					User.Default.TaskListFontFamily = "Verdana"; // because segoe isn't installed on XP

				User.Default.Save();
			}
		}

		private static bool IsXP()
		{
			var os = Environment.OSVersion;
			var vs = os.Version;

			return os.Platform == PlatformID.Win32NT && vs.Major == 5 && vs.Minor != 0;
		}

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            #if PORTABLE
                var programDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Log.LogFile = Path.Combine(programDir, "log.txt");
                MakePortable(User.Default);
            #endif

            MigrateUserSettings();

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private static void MakePortable(ApplicationSettingsBase settings)
        {
            var portableSettingsProvider = new PortableSettingsProvider();
            settings.Providers.Add(portableSettingsProvider);
            foreach (SettingsProperty prop in settings.Properties)
            {
                prop.Provider = portableSettingsProvider;
            }
            settings.Reload();
        }
    }
}