using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToDoLib;
using System.Windows;

namespace Client
{
	public static class ExceptionExtensions
	{
		public static void Handle(this Exception ex, string errorMessage)
		{
			Log.Error(errorMessage, ex);
			MessageBox.Show(errorMessage + Environment.NewLine + ex.Message + Environment.NewLine + "Please see Help -> Show Error Log for more details",
				"Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}
