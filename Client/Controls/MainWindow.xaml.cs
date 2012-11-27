using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Win32;
using ColorFont;
using System.Windows.Media;

namespace Client
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		TrayMainWindows _tray;
		HotKeyMainWindows _hotkey;
		WindowLocation _previousWindowLocaiton;

		public MainWindow()
		{
			Application.Current.DispatcherUnhandledException += (o, e) =>
				{
					e.Exception.Handle("An unexpected error occurred");
					e.Handled = true;
				};

			InitializeComponent();

			SetupTrayIcon();

			CheckForUpdates();

			webBrowser1.Navigate("about:blank");

			SetFont();

			SetWindowPosition();

			lbTasks.Focus();
		}

		public MainWindowViewModel ViewModel { get; set; }

		private void CheckForUpdates()
		{
			var updateChecker = new UpdateChecker(this);
			updateChecker.BeginCheck();
		}

		private void SetupTrayIcon()
		{
			if (User.Default.MinimiseToSystemTray)
			{
				_tray = new TrayMainWindows(this);
				_hotkey = new HotKeyMainWindows(this, ModifierKeys.Windows | ModifierKeys.Alt, System.Windows.Forms.Keys.T);
			}
		}

		private void SetWindowPosition()
		{
			Height = User.Default.WindowHeight;
			Width = User.Default.WindowWidth;
			Left = User.Default.WindowLeft;
			Top = User.Default.WindowTop;
		}

		protected override void OnClosed(EventArgs e)
		{
			if (ViewModel.HelpPage != null)
				ViewModel.HelpPage.Close();

			base.OnClosed(e);
		}


		/// <summary>
		/// Helper function that converts the values stored in the settings into the font values
		/// and then sets the tasklist font values.
		/// </summary>
		public void SetFont()
		{
            var family = new FontFamily(User.Default.TaskListFontFamily);

			double size = User.Default.TaskListFontSize;

			var styleConverter = new FontStyleConverter();

			FontStyle style = (FontStyle)styleConverter.ConvertFromString(User.Default.TaskListFontStyle);

			var stretchConverter = new FontStretchConverter();
			FontStretch stretch = (FontStretch)stretchConverter.ConvertFromString(User.Default.TaskListFontStretch);

			var weightConverter = new FontWeightConverter();
			FontWeight weight = (FontWeight)weightConverter.ConvertFromString(User.Default.TaskListFontWeight);

			Color color = (Color)ColorConverter.ConvertFromString(User.Default.TaskListFontBrushColor);

			lbTasks.FontFamily = family;
			lbTasks.FontSize = size;
			lbTasks.FontStyle = style;
			lbTasks.FontStretch = stretch;
			lbTasks.FontWeight = weight;
			lbTasks.Foreground = new SolidColorBrush(color);
		}

		void SetSelected(MenuItem item)
		{
			var sortMenu = (MenuItem)item.Parent;
			foreach (MenuItem i in sortMenu.Items)
				i.IsChecked = false;

			item.IsChecked = true;

		}

		private void Filter(object sender, RoutedEventArgs e)
		{
			ViewModel.ShowFilterDialog();
		}

		public void ToggleUpdateMenu(string version)
		{
			var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			if (version != assemblyVersion)
			{
				UpdateMenu.Header = "New version: " + version;
				UpdateMenu.Visibility = Visibility.Visible;
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			ViewModel = new MainWindowViewModel(this);
			DataContext = ViewModel;
		}

		#region window location handlers
		private void Window_LocationChanged(object sender, EventArgs e)
		{
			_previousWindowLocaiton = new WindowLocation();

			if (Left >= 0 && Top >= 0)
			{
				User.Default.WindowLeft = Left;
				User.Default.WindowTop = Top;
				User.Default.Save();
			}
		}

		private void Window_StateChanged(object sender, EventArgs e)
		{
			if (WindowState == System.Windows.WindowState.Maximized)
			{
				User.Default.WindowLeft = _previousWindowLocaiton.Left;
				User.Default.WindowTop = _previousWindowLocaiton.Top;
				User.Default.WindowHeight = _previousWindowLocaiton.Height;
				User.Default.WindowWidth = _previousWindowLocaiton.Width;
				User.Default.Save();
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (e.NewSize.Height > 0 && e.NewSize.Width > 0 && WindowState != System.Windows.WindowState.Maximized)
			{
				User.Default.WindowHeight = e.NewSize.Height;
				User.Default.WindowWidth = e.NewSize.Width;
				User.Default.Save();
			}
		}
		#endregion

		#region file menu

		public void File_New(object sender, RoutedEventArgs e)
		{
			ViewModel.NewFile();
		}

		public void File_Open(object sender, RoutedEventArgs e)
		{
			ViewModel.OpenFile();
		}

		private void File_Exit(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void File_Archive_Completed(object sender, RoutedEventArgs e)
		{
			ViewModel.ArchiveCompleted();
		}

		public void File_Options(object sender, RoutedEventArgs e)
		{
			ViewModel.ShowOptionsDialog();
		}

		#endregion

		#region sort menu
		private void Sort_Priority(object sender, RoutedEventArgs e)
		{
			ViewModel.SortType = SortType.Priority;
			ViewModel.UpdateDisplayedTasks();
			SetSelected((MenuItem)sender);
		}

		private void Sort_None(object sender, RoutedEventArgs e)
		{
			ViewModel.SortType = SortType.None;
			ViewModel.UpdateDisplayedTasks();
			SetSelected((MenuItem)sender);
		}

		private void Sort_Context(object sender, RoutedEventArgs e)
		{
			ViewModel.SortType = SortType.Context;
			ViewModel.UpdateDisplayedTasks();
			SetSelected((MenuItem)sender);
		}

		private void Sort_Completed(object sender, RoutedEventArgs e)
		{
			ViewModel.SortType = SortType.Completed;
			ViewModel.UpdateDisplayedTasks();
			SetSelected((MenuItem)sender);
		}

		private void Sort_DueDate(object sender, RoutedEventArgs e)
		{
			ViewModel.SortType = SortType.DueDate;
			ViewModel.UpdateDisplayedTasks();
			SetSelected((MenuItem)sender);
		}

		private void Sort_Project(object sender, RoutedEventArgs e)
		{
			ViewModel.SortType = SortType.Project;
			ViewModel.UpdateDisplayedTasks();
			SetSelected((MenuItem)sender);
		}

		private void Sort_Alphabetical(object sender, RoutedEventArgs e)
		{
			ViewModel.SortType = SortType.Alphabetical;
			ViewModel.UpdateDisplayedTasks();
			SetSelected((MenuItem)sender);
		}
		#endregion

		#region help menu
		public void Help(object sender, RoutedEventArgs e)
		{
			ViewModel.ShowHelpDialog();
		}

		private void ViewLog(object sender, RoutedEventArgs e)
		{
			ViewModel.ViewLog();
		}
		#endregion

		#region update menu
		private void GetUpdate(object sender, RoutedEventArgs e)
		{
			try 
			{
				Process.Start(UpdateChecker.updateClientUrl);
			}
			catch (Exception ex)
			{
				ex.Handle("Error while launching " + UpdateChecker.updateClientUrl);
			}
		}

		#endregion

		#region lbTasks
		private void lbTasks_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			ViewModel.TaskListKeyUp(e.Key, e.KeyboardDevice.Modifiers);
		}

		// Using KeyDown allows for holding the key to navigate
		private void lbTasks_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			ViewModel.TaskListPreviewKeyDown(e);
		}

		private void lbTasks_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			ViewModel.TaskListKeyUp(Key.U);
		}

		#endregion

		#region printing

		private void btnPrint_Click(object sender, RoutedEventArgs e)
		{
			mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
			doc.execCommand("Print", true, 0);
			doc.close();

			ViewModel.SetPrintControlsVisibility(false);
		}

		private void btnCancelPrint_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.SetPrintControlsVisibility(false);
			lbTasks.Focus();
		}


		private void File_PrintPreview(object sender, RoutedEventArgs e)
		{
			string printContents;
			printContents = ViewModel.GetPrintContents();

			mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
			doc.clear();
			doc.write(printContents);
			doc.close();

			ViewModel.SetPrintControlsVisibility(true);
		}

		private void File_Print(object sender, RoutedEventArgs e)
		{
			string printContents;
			printContents = ViewModel.GetPrintContents();

			mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
			doc.clear();
			doc.write(printContents);
			doc.execCommand("Print", true, 0);
			doc.close();
		}
		#endregion  //printing

		#region taskText

		private void taskText_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			ViewModel.TaskTextPreviewKeyUp(e);
		}

		#endregion

		#region intellisense
		
		private void Intellisense_KeyDown(object sender, KeyEventArgs e)
		{
			ViewModel.IntellisenseKeyDown(e);
			e.Handled = true;
		}

		private void ShowIntellisense(IEnumerable<string> s, Rect placement)
		{
			ViewModel.ShowIntellisense(s, placement);
		}

		private void IntellisenseList_MouseUp(object sender, MouseButtonEventArgs e)
		{
			ViewModel.IntellisenseMouseUp();
		}
		#endregion
	}
}

