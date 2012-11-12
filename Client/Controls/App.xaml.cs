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
				User.Default.Save();
			}
		}
    }
}
