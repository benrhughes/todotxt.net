using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		public App()
		{
			MigrateUserSettings();
		}

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

			if (os.Platform == PlatformID.Win32NT && vs.Major == 5 && vs.Minor != 0)
				return true;
			
			return false;
		}
    }
}
