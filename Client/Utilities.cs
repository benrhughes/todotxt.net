using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToDoLib;
using System.Windows;

namespace Client
{
	public class Utilities
	{
		public void Try(Action action, string errorMessage)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				HandleException(errorMessage, ex);
			}
		}

		public void HandleException(string errorMessage, Exception ex)
		{
			Log.Error(errorMessage, ex);
			MessageBox.Show(errorMessage + Environment.NewLine + ex.Message + Environment.NewLine + "Please see Help -> Show Error Log for more details",
				"Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}
