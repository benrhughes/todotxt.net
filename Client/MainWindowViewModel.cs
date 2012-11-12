using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToDoLib;
using System.Collections.ObjectModel;
using System.Windows;

namespace Client
{
	public class MainWindowViewModel
	{
		private TaskList _taskList;
		private ObserverChangeFile _changefile;
		private SortType _currentSort;
		MainWindow _window;

		public MainWindowViewModel(MainWindow window)
		{
			//add view on change file
			_changefile = new ObserverChangeFile();
			_changefile.OnFileTaskListChange += () => Refresh();
		}

		public ObservableCollection<Task> Tasks 
		{
			get { return new ObservableCollection<Task>(_taskList.Tasks); } 
		}

		public void LoadTasks(string filePath)
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
			}
		}

		public void FilterAndSort(SortType sort)
		{
			if (_currentSort != sort)
			{
				User.Default.CurrentSort = (int)sort;
				User.Default.Save();
				_currentSort = sort;
			}

			if (_taskList != null)
			{
				var selected = _window.lbTasks.SelectedItem as Task;
				var selectedIndex = _window.lbTasks.SelectedIndex;

				try
				{
					_taskList.Sort(_currentSort, User.Default.FilterCaseSensitive, User.Default.FilterText);
				}
				catch (Exception ex)
				{
					HandleException("Error while sorting tasks", ex);
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
			FilterAndSort(_currentSort);
		}

		private void Reload()
		{
			Try(() => _taskList.ReloadTasks(), "Error loading tasks");
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

				FilterAndSort(_currentSort);
			}
		}

		public IEnumerable<Task> Sort(SortType sort, bool FilterCaseSensitive, string Filter)
		{
			return SortList(sort, FilterList(_tasks, FilterCaseSensitive, Filter));
		}

		public static List<Task> FilterList(List<Task> tasks, bool FilterCaseSensitive, string Filter)
		{
			var comparer = FilterCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

			if (String.IsNullOrEmpty(Filter))
				return tasks;

			List<Task> tasksFilter = new List<Task>();

			foreach (var task in tasks)
			{
				bool include = true;
				foreach (var filter in Filter.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
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
					tasksFilter.Add(task);
			}
			return tasksFilter;
		}

		public static IEnumerable<Task> SortList(SortType sort, List<Task> tasks)
		{
			Log.Debug("Sorting {0} tasks by {1}", tasks.Count().ToString(), sort.ToString());

			switch (sort)
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
	}
}
