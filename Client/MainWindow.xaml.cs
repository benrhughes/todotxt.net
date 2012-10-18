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
using ToDoLib;
using ColorFont;
using System.Windows.Media;

namespace Client
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		class WindowLocation
		{
			public WindowLocation()
			{
				Left = User.Default.WindowLeft;
				Top = User.Default.WindowTop;
				Height = User.Default.WindowHeight;
				Width = User.Default.WindowWidth;
			}

			public double Left { get; set; }
			public double Top { get; set; }
			public double Height { get; set; }
			public double Width { get; set; }
		}

		TaskList _taskList;
		SortType _currentSort;
		Task _updating;
		int _intelliPos;
		TrayMainWindows _tray;
		HotKeyMainWindows _hotkey;
		ObserverChangeFile _changefile;
		CheckUpdate _checkupdate;

		WindowLocation _previousWindowLocaiton;
		private Help _helpPage;

		/// <summary>
		/// The keyboard key that triggers a new task.
		/// </summary>
		private const Key NewTaskKey = Key.Enter; // Helps stop GUI knowledge from being tied to functionality.  

		public MainWindow()
		{
			Application.Current.DispatcherUnhandledException += (o, e) =>
				{
					HandleException("An unexpected error occurred", e.Exception);
					e.Handled = true;
				};

			InitializeComponent();

			try
			{
				if (User.Default.MinimiseToSystemTray)
				{
					//add tray icon
					_tray = new TrayMainWindows(this);

					//add global key
					_hotkey = new HotKeyMainWindows(this, ModifierKeys.Windows | ModifierKeys.Alt, System.Windows.Forms.Keys.T);
				}

				SetFont();

				//add view on change file
				_changefile = new ObserverChangeFile();
				_changefile.OnFileTaskListChange += () => Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { this.Refresh(); }));

				//CheckUpdate new version
				_checkupdate = new CheckUpdate();
				_checkupdate.OnCheckedUpdateVersion += (string version) => Dispatcher.BeginInvoke(new CheckUpdate.CheckUpdateVersion(this.ShowUpdateMenu), version);
				_checkupdate.Check();

				webBrowser1.Navigate("about:blank");

				// migrate the user settings from the previous version, if necessary
				if (User.Default.FirstRun)
				{
					User.Default.Upgrade();
					User.Default.FirstRun = false;
					User.Default.Save();
				}

				Log.LogLevel = User.Default.DebugLoggingOn ? LogLevel.Debug : LogLevel.Error;

				this.Height = User.Default.WindowHeight;
				this.Width = User.Default.WindowWidth;
				this.Left = User.Default.WindowLeft;
				this.Top = User.Default.WindowTop;

				if (!string.IsNullOrEmpty(User.Default.FilePath))
					LoadTasks(User.Default.FilePath);

				FilterAndSort((SortType)User.Default.CurrentSort);

				lbTasks.Focus();
			}
			catch (Exception ex)
			{
				HandleException("An error occurred while initialising the application", ex);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			if (_helpPage != null)
				_helpPage.Close();

			base.OnClosed(e);
		}

		#region private methods

		/// <summary>
		/// Helper function that converts the values stored in the settings into the font values
		/// and then sets the tasklist font values.
		/// </summary>
		private void SetFont()
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

			this.lbTasks.FontFamily = family;
			this.lbTasks.FontSize = size;
			this.lbTasks.FontStyle = style;
			this.lbTasks.FontStretch = stretch;
			this.lbTasks.FontWeight = weight;
			this.lbTasks.Foreground = new SolidColorBrush(color);
		}



		private void Refresh()
		{
			Reload();
			FilterAndSort(_currentSort);
		}

		private void Reload()
		{
			Try(() => _taskList.ReloadTasks(), "Error loading tasks");
		}

		private void KeyboardShortcut(Key key, ModifierKeys modifierKeys = ModifierKeys.None)
		{
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && key == Key.C)
			{
				var currentTask = lbTasks.SelectedItem as Task;
				if (currentTask != null)
					Clipboard.SetText(currentTask.Raw);

				return;
			}

			// create and open can be used when there's no list loaded
			switch (key)
			{
				case Key.C:
					File_New(null, null);
					return;
				case Key.O:
					File_Open(null, null);
					return;
			}

			if (_taskList == null)
				return;

			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
			{
				return;
			}

			switch (key)
			{
				case Key.N:
					// create one-line string of all filter but not ones beginning with a minus, and use as the starting text for a new task
					string filters = "";
					foreach (var filter in User.Default.FilterText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
					{
						if (filter.Substring(0, 1) != "-")
						{
							if (filter.Contains("active"))
							{
								// If the current filter is "active", replace it here with "today"
								filters = filters + " " + "due:today";
							}
							else
							{
								filters = filters + " " + filter;
							}
						}
					}
					taskText.Text = filters;
					taskText.Focus();
					break;
				case Key.OemQuestion:
					Help(null, null);
					break;
				case Key.F:
					Filter(null, null);
					break;

				case Key.RightShift:
					// Add Calendar to the titlebar
					AddCalendarToTitle();
					break;

				// Filter Presets
				case Key.NumPad0:
				case Key.D0:
					User.Default.FilterText = "";
					FilterAndSort(_currentSort);
					User.Default.Save();
					break;

				case Key.NumPad1:
				case Key.D1:
					User.Default.FilterText = User.Default.FilterTextPreset1;
					FilterAndSort(_currentSort);
					User.Default.Save();
					break;

				case Key.NumPad2:
				case Key.D2:
					User.Default.FilterText = User.Default.FilterTextPreset2;
					FilterAndSort(_currentSort);
					User.Default.Save();
					break;

				case Key.NumPad3:
				case Key.D3:
					User.Default.FilterText = User.Default.FilterTextPreset3;
					FilterAndSort(_currentSort);
					User.Default.Save();
					break;

				case Key.OemPeriod:
					Reload();
					FilterAndSort(_currentSort);
					break;
				case Key.X:
					ToggleComplete((Task)lbTasks.SelectedItem);
					FilterAndSort(_currentSort);
					break;
				case Key.D:
					if (modifierKeys != ModifierKeys.Windows)
					{
						var res = MessageBox.Show("Permanently delete the selected task?",
									 "Confirm Delete",
									 MessageBoxButton.YesNo,
									 MessageBoxImage.Warning);

						if (res == MessageBoxResult.Yes)
						{
							Try(() => _taskList.Delete((Task)lbTasks.SelectedItem), "Error deleting task");
							FilterAndSort(_currentSort);
						}
					}
					break;
				case Key.U:
					_updating = (Task)lbTasks.SelectedItem;
					taskText.Text = _updating.ToString();
					taskText.Focus();
					break;
				case Key.P:
					_updating = (Task)lbTasks.SelectedItem;

					int iPostponeCount = Postpone(null, null);
					if (iPostponeCount <= 0)
					{
						// User canceled, or entered a non-positive number or garbage
						break;
					}

					// Get the current DueDate from the item being updated
					DateTime dtNewDueDate;
					string postponedString;
					if (_updating.DueDate.Length > 0)
					{
						dtNewDueDate = Convert.ToDateTime(_updating.DueDate);
					}
					else
					{
						// Current item doesn't have a due date.  Use today as the due date
						dtNewDueDate = Convert.ToDateTime(DateTime.Now.ToString());
					}

					// Add days to that date
					dtNewDueDate = dtNewDueDate.AddDays(iPostponeCount);

					// Build a dummy string which we'll display so the rest of the system thinks we edited the current item.  
					// Otherwise we end up with 2 items which differ only by due date
					if (_updating.DueDate.Length > 0)
					{
						// The item has a due date, so exchange the current with the new
						postponedString = _updating.Raw.Replace(_updating.DueDate, dtNewDueDate.ToString("yyyy-MM-dd"));
					}
					else
					{
						// The item doesn't have a due date, so just append the new due date to the task
						postponedString = _updating.Raw.ToString() + " due:" + dtNewDueDate.ToString("yyyy-MM-dd");
					}

					// Display our "dummy" string.  If they cancel, no changes are committed.  
					taskText.Text = postponedString;
					taskText.Focus();
					break;
				default:
					break;
			}
		}

		// 
		//  AddCalendarToTitle
		//
		//  Add a quick calendar of the next 7 days to the title bar.  If the calendar is already displayed, toggle it off.
		//
		private void AddCalendarToTitle()
		{
			string Title = this.Title;
			string today;
			string today_letter;

			if (Title.Length < 15)
			{
				Title += "       Calendar:  ";

				for (double i = 0; i < 7; i++)
				{
					today = DateTime.Now.AddDays(i).ToString("MM-dd");
					today_letter = DateTime.Now.AddDays(i).DayOfWeek.ToString();
					today_letter = today_letter.Remove(2);
					Title += "  " + today_letter + ":" + today;
				}
			}
			else
			{
				Title = "todotxt.net";
			}
			this.Title = Title;
		}

		private void ToggleComplete(Task task)
		{
			//Ensure an empty task can not be completed.
			if(task.Body.Trim() == string.Empty)
				return;

			var newTask = new Task(task.Raw);
			newTask.Completed = !newTask.Completed;

			try
			{
				if (User.Default.AutoArchive && newTask.Completed)
				{
					if (User.Default.ArchiveFilePath.IsNullOrEmpty())
						throw new Exception("You have enabled auto-archiving but have not specified an archive file.\nPlease go to File -> Options and disable auto-archiving or specify an archive file");

					var archiveList = new TaskList(User.Default.ArchiveFilePath);
					archiveList.Add(newTask);
					_taskList.Delete(task);
				}
				else
				{
					_taskList.Update(task, newTask);
				}
			}
			catch (Exception ex)
			{
				HandleException("An error occurred while updating the task's completed status", ex);
			}
		}

		void FilterAndSort(SortType sort)
		{
			if (_currentSort != sort)
			{
				User.Default.CurrentSort = (int)sort;
				User.Default.Save();
				_currentSort = sort;
			}

			if (_taskList != null)
			{
				var selected = lbTasks.SelectedItem as Task;
				var selectedIndex = lbTasks.SelectedIndex;

				try
				{
					lbTasks.ItemsSource = _taskList.Sort(_currentSort, User.Default.FilterCaseSensitive, User.Default.FilterText);
				}
				catch (Exception ex)
				{
					HandleException("Error while sorting tasks", ex);
				}

				if (selected == null)
				{
					lbTasks.SelectedIndex = 0;
				}
				else
				{
					object match = null;
					foreach (var item in lbTasks.Items)
					{
						if (((Task)item).Body.Equals(selected.Body, StringComparison.InvariantCultureIgnoreCase))
						{
							match = item;
							break;
						}
					}

					if (match == null)
					{
						lbTasks.SelectedIndex = selectedIndex;
					}
					else
					{
						lbTasks.SelectedItem = match;
						lbTasks.ScrollIntoView(match);
					}
				}

				//Set the menu item to Bold to easily identify if there is a filter in force
				filterMenu.FontWeight = User.Default.FilterText.Length == 0 ? FontWeights.Normal : FontWeights.Bold;
			}
		}

		void SetSelected(MenuItem item)
		{
			var sortMenu = (MenuItem)item.Parent;
			foreach (MenuItem i in sortMenu.Items)
				i.IsChecked = false;

			item.IsChecked = true;

		}

		private void LoadTasks(string filePath)
		{
			try
			{
				_taskList = new TaskList(filePath);
				User.Default.FilePath = filePath;
				User.Default.Save();
				_changefile.ViewOnFile(User.Default.FilePath);
				FilterAndSort(_currentSort);
			}
			catch (Exception ex)
			{
				HandleException("An error occurred while opening " + filePath, ex);
				sortMenu.IsEnabled = false;
			}
		}


		private void Filter(object sender, RoutedEventArgs e)
		{
			var f = new FilterDialog();
			f.Left = this.Left + 10;
			f.Top = this.Top + 10;
			f.FilterText = User.Default.FilterText;
			f.FilterTextPreset1 = User.Default.FilterTextPreset1;
			f.FilterTextPreset2 = User.Default.FilterTextPreset2;
			f.FilterTextPreset3 = User.Default.FilterTextPreset3;
			if (f.ShowDialog().Value)
			{
				User.Default.FilterText = f.FilterText.Trim();
				User.Default.FilterTextPreset1 = f.FilterTextPreset1.Trim();
				User.Default.FilterTextPreset2 = f.FilterTextPreset2.Trim();
				User.Default.FilterTextPreset3 = f.FilterTextPreset3.Trim();
				User.Default.Save();

				FilterAndSort(_currentSort);
			}
		}


		private int Postpone(object sender, RoutedEventArgs e)
		{
			var p = new PostponeDialog();
			p.Left = this.Left + 10;
			p.Top = this.Top + 10;
			int iDays = 0;

			if (p.ShowDialog().Value)
			{
				string sPostpone = p.PostponeText.Trim();

				if (sPostpone.Length > 0)
				{
					try
					{
						iDays = Convert.ToInt32(sPostpone);
					}
					catch
					{
						// No action needed.  iDays will be 0, which will leave the item unaltered.
					}
				}
			}

			return iDays;

		}
		private void ShowUpdateMenu(string version)
		{
			var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			if (version != assemblyVersion)
			{
				this.UpdateMenu.Header = "New version: " + version;
				this.UpdateMenu.Visibility = Visibility.Visible;
			}
		}

		private void Try(Action action, string errorMessage)
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

		private void HandleException(string errorMessage, Exception ex)
		{
			Log.Error(errorMessage, ex);
			MessageBox.Show(errorMessage + Environment.NewLine + ex.Message + Environment.NewLine + "Please see Help -> Show Error Log for more details", 
				"Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
		#endregion

		#region UI event handling

		#region windows
		private void Window_LocationChanged(object sender, EventArgs e)
		{
			_previousWindowLocaiton = new WindowLocation();

			if (Left >= 0 && Top >= 0)
			{
				User.Default.WindowLeft = this.Left;
				User.Default.WindowTop = this.Top;
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

		private void File_New(object sender, RoutedEventArgs e)
		{
			var dialog = new SaveFileDialog();
			dialog.FileName = "todo.txt";
			dialog.DefaultExt = ".txt";
			dialog.Filter = "Text documents (.txt)|*.txt";
			var res = dialog.ShowDialog();
			if (res.Value)
				SaveFileDialog(dialog.FileName);

			if (File.Exists(dialog.FileName))
				LoadTasks(dialog.FileName);

		}

		private static void SaveFileDialog(string filename)
		{
			using (StreamWriter todofile = new StreamWriter(filename))
			{
				todofile.Write("");
			}
		}


		private void File_Open(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.DefaultExt = ".txt";
			dialog.Filter = "Text documents (.txt)|*.txt";

			var res = dialog.ShowDialog();

			if (res.Value)
				LoadTasks(dialog.FileName);
		}

		private void File_Exit(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void File_Archive_Completed(object sender, RoutedEventArgs e)
		{
			if (!File.Exists(User.Default.ArchiveFilePath))
				File_Options(this, null);

			if (!File.Exists(User.Default.ArchiveFilePath))
				return;

			var archiveList = new TaskList(User.Default.ArchiveFilePath);
			var completed = _taskList.Tasks.Where(t => t.Completed);
			foreach (var task in completed)
			{
				archiveList.Add(task);
				_taskList.Delete(task);
			}

			FilterAndSort(_currentSort);
		}

		private void File_Options(object sender, RoutedEventArgs e)
		{
			var o = new Options(FontInfo.GetControlFont(lbTasks));
			o.Owner = this;

			var res = o.ShowDialog();

			if (res.Value)
			{
				User.Default.ArchiveFilePath = o.tbArchiveFile.Text;
				User.Default.AutoArchive = o.cbAutoArchive.IsChecked.Value;
				User.Default.AutoRefresh = o.cbAutoRefresh.IsChecked.Value;
				User.Default.FilterCaseSensitive = o.cbCaseSensitiveFilter.IsChecked.Value;
				User.Default.AddCreationDate = o.cbAddCreationDate.IsChecked.Value;
				User.Default.DebugLoggingOn = o.cbDebugOn.IsChecked.Value;
				User.Default.MinimiseToSystemTray = o.cbMinToSysTray.IsChecked.Value;
				User.Default.RequireCtrlEnter = o.cbRequireCtrlEnter.IsChecked.Value;

				// Unfortunately, font classes are not serializable, so all the pieces are tracked instead.
				User.Default.TaskListFontFamily = o.TaskListFont.Family.ToString();
				User.Default.TaskListFontSize = o.TaskListFont.Size;
				User.Default.TaskListFontStyle = o.TaskListFont.Style.ToString();
				User.Default.TaskListFontStretch = o.TaskListFont.Stretch.ToString();
				User.Default.TaskListFontBrushColor = o.TaskListFont.BrushColor.ToString();

				User.Default.Save();

				Log.LogLevel = User.Default.DebugLoggingOn ? LogLevel.Debug : LogLevel.Error;

				_changefile.ViewOnFile(User.Default.FilePath);

				SetFont();

				FilterAndSort(_currentSort);
			}
		}

		#region printing

		private void btnPrint_Click(object sender, RoutedEventArgs e)
		{
			mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
			doc.execCommand("Print", true, 0);
			doc.close();

			Set_PrintControlsVisibility(false);
		}

		private void btnCancelPrint_Click(object sender, RoutedEventArgs e)
		{
			Set_PrintControlsVisibility(false);
			lbTasks.Focus();
		}

		private void Set_PrintControlsVisibility(bool PrintControlsVisibility)
		{
			if (PrintControlsVisibility)
			{   // Show Printer Controls
				webBrowser1.Visibility = Visibility.Visible;
				btnPrint.Visibility = Visibility.Visible;
				btnCancelPrint.Visibility = Visibility.Visible;
				lbTasks.Visibility = Visibility.Hidden;
				menu1.Visibility = Visibility.Hidden;
				taskText.Visibility = Visibility.Hidden;
			}
			else
			{   // Hide Printer Controls
				webBrowser1.Visibility = Visibility.Hidden;
				btnPrint.Visibility = Visibility.Hidden;
				btnCancelPrint.Visibility = Visibility.Hidden;
				lbTasks.Visibility = Visibility.Visible;
				menu1.Visibility = Visibility.Visible;
				taskText.Visibility = Visibility.Visible;
			}
		}

		private string Get_PrintContents()
		{
			if (lbTasks.Items == null || lbTasks.Items.IsEmpty)
				return "";


			var contents = new StringBuilder();

			contents.Append("<html><head>");
			contents.Append("<title>todotxt.net</title>");
			contents.Append("<style>" + Resource.CSS + "</style>");
			contents.Append("</head>");

			contents.Append("<body>");
			contents.Append("<h2>todotxt.net</h2>");
			contents.Append("<table>");
			contents.Append("<tr class='tbhead'><th>&nbsp;</th><th>Done</th><th>Created</th><th>Due</th><td>Details</td></tr>");

			foreach (Task task in lbTasks.Items)
			{
				if (task.Completed)
				{
					contents.Append("<tr class='completedTask'>");
					contents.Append("<td class='complete'>x</td> ");
					contents.Append("<td class='completeddate'>" + task.CompletedDate + "</td> ");
				}
				else
				{
					contents.Append("<tr class='uncompletedTask'>");
					if (string.IsNullOrEmpty(task.Priority))
						contents.Append("<td>&nbsp;</td>");
					else
						contents.Append("<td><span class='priority'>" + task.Priority + "</span></td>");

					contents.Append("<td>&nbsp;</td>");
				}

				if (string.IsNullOrEmpty(task.CreationDate))
					contents.Append("<td>&nbsp;</td>");
				else
					contents.Append("<td class='startdate'>" + task.CreationDate + "</td>");
				if (string.IsNullOrEmpty(task.DueDate))
					contents.Append("<td>&nbsp;</td>");
				else
					contents.Append("<td class='enddate'>" + task.DueDate + "</td>");

				contents.Append("<td>" + task.Body);

				task.Projects.ForEach(project => contents.Append(" <span class='project'>" + project + "</span> "));

				task.Contexts.ForEach(context => contents.Append(" <span class='context'>" + context + "</span> "));

				contents.Append("</td>");

				contents.Append("</tr>");
			}

			contents.Append("</table></body></html>");

			return contents.ToString();
		}


		private void File_PrintPreview(object sender, RoutedEventArgs e)
		{
			string printContents;
			printContents = Get_PrintContents();

			mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
			doc.clear();
			doc.write(printContents);
			doc.close();

			Set_PrintControlsVisibility(true);
		}

		private void File_Print(object sender, RoutedEventArgs e)
		{
			string printContents;
			printContents = Get_PrintContents();

			mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
			doc.clear();
			doc.write(printContents);
			doc.execCommand("Print", true, 0);
			doc.close();
		}

		#endregion  //printing

		#endregion

		#region sort menu
		private void Sort_Priority(object sender, RoutedEventArgs e)
		{
			FilterAndSort(SortType.Priority);
			SetSelected((MenuItem)sender);
		}

		private void Sort_None(object sender, RoutedEventArgs e)
		{
			FilterAndSort(SortType.None);
			SetSelected((MenuItem)sender);
		}

		private void Sort_Context(object sender, RoutedEventArgs e)
		{
			FilterAndSort(SortType.Context);
			SetSelected((MenuItem)sender);
		}

		private void Sort_Completed(object sender, RoutedEventArgs e)
		{
			FilterAndSort(SortType.Completed);
			SetSelected((MenuItem)sender);
		}

		private void Sort_DueDate(object sender, RoutedEventArgs e)
		{
			FilterAndSort(SortType.DueDate);
			SetSelected((MenuItem)sender);
		}

		private void Sort_Project(object sender, RoutedEventArgs e)
		{
			FilterAndSort(SortType.Project);
			SetSelected((MenuItem)sender);
		}

		private void Sort_Alphabetical(object sender, RoutedEventArgs e)
		{
			FilterAndSort(SortType.Alphabetical);
			SetSelected((MenuItem)sender);
		}
		#endregion

		#region help menu
		private void Help(object sender, RoutedEventArgs e)
		{
			var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			_helpPage = new Help("todotxt.net", version, Resource.HelpText, "http://benrhughes.com/todotxt.net", "benrhughes.com/todotxt.net");

			_helpPage.Show();
		}

		private void ViewLog(object sender, RoutedEventArgs e)
		{
			if (File.Exists(Log.LogFile))
				Process.Start(Log.LogFile);
			else
				MessageBox.Show("Log file does not exist: no errors have been logged", "Log file does not exist", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		#endregion

		#region Update notification
		private void Get_Update(object sender, RoutedEventArgs e)
		{
			Try(() => Process.Start(CheckUpdate.updateClientUrl), "Error while launching " + CheckUpdate.updateClientUrl);
		}
		#endregion

		#region lbTasks
		private void lbTasks_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			KeyboardShortcut(e.Key, e.KeyboardDevice.Modifiers);
		}

		//this is just for j and k - the nav keys. Using KeyDown allows for holding the key to navigate
		private void lbTasks_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) && lbTasks.HasItems)
			{
				var selected = lbTasks.SelectedItem as Task;
				var updated = new Task(selected.Raw);

				switch (e.SystemKey)
				{
					case Key.Up:
						updated.IncPriority();
						Try(() => _taskList.Update(selected, updated), "Error while changing priority");
						Reload();
						FilterAndSort(_currentSort);
						break;

					case Key.Down:
						updated.DecPriority();
						Try(() => _taskList.Update(selected, updated), "Error while changing priority");
						FilterAndSort(_currentSort);
						Reload();
						break;
					case Key.Left:
					case Key.Right:
						updated.SetPriority(' ');
						Try(() => _taskList.Update(selected, updated), "Error while changing priority");
						FilterAndSort(_currentSort);
						Reload();
						break;
				}

				return;
			}

			switch (e.Key)
			{
				case Key.J:
				case Key.Down:
					if (lbTasks.SelectedIndex < lbTasks.Items.Count - 1)
					{
						lbTasks.SelectedIndex++;
						lbTasks.ScrollIntoView(lbTasks.Items[lbTasks.SelectedIndex]);
					}
					e.Handled = true;
					break;
				case Key.K:
				case Key.Up:
					if (lbTasks.SelectedIndex > 0)
					{
						lbTasks.ScrollIntoView(lbTasks.Items[lbTasks.SelectedIndex - 1]);
						lbTasks.SelectedIndex = lbTasks.SelectedIndex - 1;
					}
					e.Handled = true;
					break;
				default:
					break;
			}
		}

		private void lbTasks_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			KeyboardShortcut(Key.U);
		}

		#endregion

		#region taskText

		/// <summary>
		/// Helper function to determine if the correct keysequence has been entered to create a task.
		/// Added to enable the check for Ctrl-Enter if set in options.
		/// </summary>
		/// <param name="e">The stroked key and any modifiers.</param>
		/// <returns>true if the task should be added to the list, false otherwise.</returns>
		private bool ShoudAddTask(KeyEventArgs e)
		{
			bool shouldAddTask = false;

			if (e.Key == NewTaskKey)
			{
				if (User.Default.RequireCtrlEnter)
				{
					if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
					{
						shouldAddTask = true;
					}
				}
				else
				{
					shouldAddTask = true;
				}
			}

			return shouldAddTask;
		}

		private void taskText_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (_taskList == null)
			{
				MessageBox.Show("You don't have a todo.txt file open - please use File\\New or File\\Open",
					"Please open a file", MessageBoxButton.OK, MessageBoxImage.Error);
				e.Handled = true;
				lbTasks.Focus();
				return;
			}

			if (ShoudAddTask(e))
			{
				if (_updating == null)
				{
					try
					{
						var taskDetail = taskText.Text.Trim();

						if (User.Default.AddCreationDate)
						{
							var tmpTask = new Task(taskDetail);
							var today = DateTime.Today.ToString("yyyy-MM-dd");

							if (string.IsNullOrEmpty(tmpTask.CreationDate))
							{
								if (string.IsNullOrEmpty(tmpTask.Priority))
									taskDetail = today + " " + taskDetail;
								else
									taskDetail = taskDetail.Insert(tmpTask.Priority.Length, " " + today);
							}
						}
						_taskList.Add(new Task(taskDetail));
					}
					catch (TaskException ex)
					{
						MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				else
				{
					_taskList.Update(_updating, new Task(taskText.Text.Trim()));
					_updating = null;
				}

				taskText.Text = "";
				FilterAndSort(_currentSort);

				Intellisense.IsOpen = false;
				lbTasks.Focus();

				return;
			}

			if (Intellisense.IsOpen && !IntellisenseList.IsFocused)
			{
				if (taskText.CaretIndex <= _intelliPos) // we've moved behind the symbol, drop out of intellisense
				{
					Intellisense.IsOpen = false;
					return;
				}

				switch (e.Key)
				{
					case Key.Down:
						IntellisenseList.Focus();
						Keyboard.Focus(IntellisenseList);
						IntellisenseList.SelectedIndex = 0;
						break;
					case Key.Escape:
					case Key.Space:
						Intellisense.IsOpen = false;
						break;
					default:
						var word = FindIntelliWord();
						IntellisenseList.Items.Filter = (o) => o.ToString().Contains(word);
						break;
				}
			}
			else
			{
				switch (e.Key)
				{
					case Key.Escape:
						_updating = null;
						taskText.Text = "";
						this.lbTasks.Focus();
						break;
					case Key.OemPlus:
					case Key.Add: // handles the '+' from the numpad.
						if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift || e.Key == Key.Add) // activates on '+' but not '='.
						{
							var projects = _taskList.Tasks.SelectMany(task => task.Projects);
							_intelliPos = taskText.CaretIndex - 1;
							ShowIntellisense(projects.Distinct().OrderBy(s => s), taskText.GetRectFromCharacterIndex(_intelliPos));
						}
						break;
					case Key.D2:
						if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift) // activates on '@' but not '2'.
						{
							var contexts = _taskList.Tasks.SelectMany(task => task.Contexts);
							_intelliPos = taskText.CaretIndex - 1;
							ShowIntellisense(contexts.Distinct().OrderBy(s => s), taskText.GetRectFromCharacterIndex(_intelliPos));
						}
						break;
				}
			}
		}

		private string FindIntelliWord()
		{
			return taskText.Text.Substring(_intelliPos + 1, taskText.CaretIndex - _intelliPos - 1);
		}

		#endregion

		#region intellisense

		/// <summary>
		/// Helper function to add the chosen text in the intellisense into the task string.
		/// Created to allow the use of both keyboard and mouse clicks.
		/// </summary>
		private void InsertTextIntoTaskString()
		{
			Intellisense.IsOpen = false;

			taskText.Text = taskText.Text.Remove(_intelliPos, taskText.CaretIndex - _intelliPos);

			var newText = IntellisenseList.SelectedItem.ToString();
			taskText.Text = taskText.Text.Insert(_intelliPos, newText);
			taskText.CaretIndex = _intelliPos + newText.Length;

			taskText.Focus();
		}

		/// <summary>
		/// Tab, Enter and Space keys will all added the selected text into the task string.
		/// Escape key cancels out.
		/// </summary>
		/// <param name="sender">Not used.</param>
		/// <param name="e">The key to trigger on.</param>
		private void Intellisense_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
				case Key.Tab:
				case Key.Space:
					InsertTextIntoTaskString();
					break;
				case Key.Escape:
					Intellisense.IsOpen = false;
					taskText.CaretIndex = taskText.Text.Length;
					taskText.Focus();
					break;
			}

			e.Handled = true;
		}

		private void ShowIntellisense(IEnumerable<string> s, Rect placement)
		{
			if (s.Count() == 0)
				return;

			Intellisense.PlacementTarget = taskText;
			Intellisense.PlacementRectangle = placement;

			IntellisenseList.ItemsSource = s;
			Intellisense.IsOpen = true;
			taskText.Focus();
		}

		/// <summary>
		/// Uses the click even to add the selected text from the intellisense into the task list.
		/// </summary>
		/// <param name="sender">Not used.</param>
		/// <param name="e">Not used.</param>
		private void IntellisenseList_MouseUp(object sender, MouseButtonEventArgs e)
		{
			InsertTextIntoTaskString();
		}
		#endregion

		#endregion



	}
}

