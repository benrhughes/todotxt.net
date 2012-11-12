using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToDoLib;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.IO;
using ColorFont;
using CommonExtensions;

namespace Client
{
	public class MainWindowViewModel
	{
		private TaskList _taskList;
		private ObserverChangeFile _changefile;
		private SortType _sortType;
		MainWindow _window;
		Task _updating;

		public MainWindowViewModel(MainWindow window)
		{
			Log.LogLevel = User.Default.DebugLoggingOn ? LogLevel.Debug : LogLevel.Error;

			//add view on change file
			_changefile = new ObserverChangeFile();
			_changefile.OnFileTaskListChange += () => Refresh();

			SortType = (SortType)User.Default.CurrentSort;
			UpdateDisplayedTasks();
		}

		public SortType SortType
		{
			get { return _sortType; }
			set
			{
				if (_sortType != value)
				{
					User.Default.CurrentSort = (int)value;
					User.Default.Save();
				}

				_sortType = value;
			}
		}

		public ObservableCollection<Task> Tasks { get; private set; }

		public void LoadTasks(string filePath)
		{
			try
			{
				_taskList = new TaskList(filePath);
				User.Default.FilePath = filePath;
				User.Default.Save();
				_changefile.ViewOnFile(User.Default.FilePath);
				UpdateDisplayedTasks();
			}
			catch (Exception ex)
			{
				ex.Handle("An error occurred while opening " + filePath);
			}
		}

		public void KeyboardShortcut(Key key, ModifierKeys modifierKeys = ModifierKeys.None)
		{
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && key == Key.C)
			{
				var currentTask = _window.lbTasks.SelectedItem as Task;
				if (currentTask != null)
					Clipboard.SetText(currentTask.Raw);

				return;
			}

			// create and open can be used when there's no list loaded
			switch (key)
			{
				case Key.C:
					_window.File_New(null, null);
					return;
				case Key.O:
					_window.File_Open(null, null);
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
								filters = filters + " " + "due:today"; // If the current filter is "active", replace it here with "today"
							else
								filters = filters + " " + filter;
						}
					}

					_window.taskText.Text = filters;
					_window.taskText.Focus();
					break;
				case Key.OemQuestion:
					_window.Help(null, null);
					break;
				case Key.F:
					ShowFilterDialog();
					break;

				case Key.RightShift:
					// Add Calendar to the titlebar
					AddCalendarToTitle();
					break;

				// Filter Presets
				case Key.NumPad0:
				case Key.D0:
					User.Default.FilterText = "";
					UpdateDisplayedTasks();
					User.Default.Save();
					break;

				case Key.NumPad1:
				case Key.D1:
					User.Default.FilterText = User.Default.FilterTextPreset1;
					UpdateDisplayedTasks();
					User.Default.Save();
					break;

				case Key.NumPad2:
				case Key.D2:
					User.Default.FilterText = User.Default.FilterTextPreset2;
					UpdateDisplayedTasks();
					User.Default.Save();
					break;

				case Key.NumPad3:
				case Key.D3:
					User.Default.FilterText = User.Default.FilterTextPreset3;
					UpdateDisplayedTasks();
					User.Default.Save();
					break;

				case Key.OemPeriod:
					Reload();
					UpdateDisplayedTasks();
					break;
				case Key.X:
					ToggleComplete((Task)_window.lbTasks.SelectedItem);
					UpdateDisplayedTasks();
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
							try 
							{	        
								_taskList.Delete((Task)_window.lbTasks.SelectedItem);
							}
							catch (Exception ex)
							{
								ex.Handle("Error deleting task");
							}

							UpdateDisplayedTasks();
						}
					}
					break;
				case Key.U:
					_updating = (Task)_window.lbTasks.SelectedItem;
					_window.taskText.Text = _updating.ToString();
					_window.taskText.Focus();
					break;
				case Key.P:
					_updating = (Task)_window.lbTasks.SelectedItem;

					int iPostponeCount = ShowPostponeDialog();
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
					_window.taskText.Text = postponedString;
					_window.taskText.Focus();
					break;
				default:
					break;
			}
		}

		public void UpdateDisplayedTasks()
		{
			if (_taskList != null)
			{
				var selected = _window.lbTasks.SelectedItem as Task;
				var selectedIndex = _window.lbTasks.SelectedIndex;

				try
				{
					var tasks = FilterList(_taskList.Tasks);
					tasks = SortList(tasks);
					Tasks = new ObservableCollection<Task>(tasks);
				}
				catch (Exception ex)
				{
					ex.Handle("Error while sorting tasks");
				}

				if (selected == null)
				{
					_window.lbTasks.SelectedIndex = 0;
				}
				else
				{
					object match = null;
					foreach (var task in Tasks)
					{
						if (task.Body.Equals(selected.Body, StringComparison.InvariantCultureIgnoreCase))
						{
							match = task;
							break;
						}
					}

					if (match == null)
					{
						_window.lbTasks.SelectedIndex = selectedIndex;
					}
					else
					{
						_window.lbTasks.SelectedItem = match;
						_window.lbTasks.ScrollIntoView(match);
					}
				}

				//Set the menu item to Bold to easily identify if there is a filter in force
				_window.filterMenu.FontWeight = User.Default.FilterText.Length == 0 ? FontWeights.Normal : FontWeights.Bold;
			}
		}

		private void Refresh()
		{
			Reload();
			UpdateDisplayedTasks();
		}

		private void Reload()
		{
			try 
			{	        
				_taskList.ReloadTasks();
			}
			catch (Exception ex)
			{
				ex.Handle("Error loading tasks");
			}
		}

		public void ShowFilterDialog()
		{
			var f = new FilterDialog();
			f.Owner = _window;

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

				UpdateDisplayedTasks();
			}
		}

		public static IEnumerable<Task> FilterList(IEnumerable<Task> tasks)
		{
			var filters = User.Default.FilterText;
			var comparer = User.Default.FilterCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

			if (String.IsNullOrEmpty(filters))
				return tasks;

			var filteredTasks = new List<Task>();

			foreach (var task in tasks)
			{
				bool include = true;
				foreach (var filter in filters.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
				{
					if (filter.Equals("due:today", StringComparison.OrdinalIgnoreCase)
						&& task.DueDate == DateTime.Now.ToString("yyyy-MM-dd"))
						continue;
					else if (filter.Equals("due:future", StringComparison.OrdinalIgnoreCase)
						&& task.DueDate.IsDateGreaterThan(DateTime.Now))
						continue;
					else if (filter.Equals("due:past", StringComparison.OrdinalIgnoreCase)
						&& task.DueDate.IsDateLessThan(DateTime.Now))
						continue;
					else if (filter.Equals("due:active", StringComparison.OrdinalIgnoreCase)
						&& !task.DueDate.IsNullOrEmpty()
						&& !task.DueDate.IsDateGreaterThan(DateTime.Now))
						continue;

					if (filter.Substring(0, 1) == "-")
					{
						if (task.Raw.Contains(filter.Substring(1), comparer))
							include = false;
					}
					else if (!task.Raw.Contains(filter, comparer))
					{
						include = false;
					}
				}

				if (include)
					filteredTasks.Add(task);
			}
			return filteredTasks;
		}

		public IEnumerable<Task> SortList(IEnumerable<Task> tasks)
		{
			Log.Debug("Sorting {0} tasks by {1}", tasks.Count().ToString(), SortType.ToString());

			switch (SortType)
			{
				// nb, we sub-sort by completed for most sorts by prepending either a or z
				case SortType.Completed:
					return tasks.OrderBy(t => t.Completed);
				case SortType.Context:
					return tasks.OrderBy(t =>
					{
						var s = t.Completed ? "z" : "a";
						if (t.Contexts != null && t.Contexts.Count > 0)
							s += t.Contexts.Min().Substring(1);
						else
							s += "zzz";
						return s;
					});
				case SortType.Alphabetical:
					return tasks.OrderBy(t => (t.Completed ? "z" : "a") + t.Raw);
				case SortType.DueDate:
					return tasks.OrderBy(t => (t.Completed ? "z" : "a") + (string.IsNullOrEmpty(t.DueDate) ? "9999-99-99" : t.DueDate));
				case SortType.Priority:
					return tasks.OrderBy(t => (t.Completed ? "z" : "a") + (string.IsNullOrEmpty(t.Priority) ? "(z)" : t.Priority));
				case SortType.Project:
					return tasks.OrderBy(t =>
					{
						var s = t.Completed ? "z" : "a";
						if (t.Projects != null && t.Projects.Count > 0)
							s += t.Projects.Min().Substring(1);
						else
							s += "zzz";
						return s;
					});
				case SortType.None:
				default:
					return tasks;
			}
		}

		//  Add a quick calendar of the next 7 days to the title bar.  If the calendar is already displayed, toggle it off.
		private void AddCalendarToTitle()
		{
			var title = _window.Title;

			if (title.Length < 15)
			{
				title += "       Calendar:  ";

				for (double i = 0; i < 7; i++)
				{
					var today = DateTime.Now.AddDays(i).ToString("MM-dd");
					var today_letter = DateTime.Now.AddDays(i).DayOfWeek.ToString();
					today_letter = today_letter.Remove(2);
					title += "  " + today_letter + ":" + today;
				}
			}
			else
			{
				title = "todotxt.net";
			}

			_window.Title = title;
		}

		private void ToggleComplete(Task task)
		{
			//Ensure an empty task can not be completed.
			if (task.Body.Trim() == string.Empty)
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
				ex.Handle("An error occurred while updating the task's completed status");
			}
		}

		private int ShowPostponeDialog()
		{
			var dialog = new PostponeDialog();
			dialog.Owner = _window;

			int iDays = 0;

			if (dialog.ShowDialog().Value)
			{
				string sPostpone = dialog.PostponeText.Trim();

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

		public void ArchiveCompleted()
		{
			if (!File.Exists(User.Default.ArchiveFilePath))
				_window.File_Options(null, null);

			if (!File.Exists(User.Default.ArchiveFilePath))
				return;

			var archiveList = new TaskList(User.Default.ArchiveFilePath);
			var completed = _taskList.Tasks.Where(t => t.Completed);
			foreach (var task in completed)
			{
				archiveList.Add(task);
				_taskList.Delete(task);
			}

			UpdateDisplayedTasks();
		}

		public void ShowOptionsDialog()
		{
			var o = new Options(FontInfo.GetControlFont(_window.lbTasks));
			o.Owner = _window;

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

				_window.SetFont();

				UpdateDisplayedTasks();
			}
		}
	}
}
