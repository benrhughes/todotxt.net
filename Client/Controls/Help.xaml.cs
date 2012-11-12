using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

namespace Client
{
	/// <summary>
	/// Interaction logic for Help.xaml
	/// </summary>
	public partial class Help : Window
	{
		public Help(string title, string version, string helpText, string helpUrl, string helpUrlText)
		{
			InitializeComponent();
		
			tbTitle.Text = title;
			tbVersion.Text = "Version " + version;
			tbHelpText.Text = helpText;
			hLink.NavigateUri = new Uri(helpUrl);
			tbLink.Text = helpUrlText;
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start(e.Uri.ToString());
		}
	}
}
