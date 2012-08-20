using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace ToDoLib
{
	/// <summary>
	/// A thin data access abstraction over the actual todo.txt file
	/// </summary>
	public class TaskList
	{
		// It may look like an overly simple approach has been taken here, but it's well considered. This class
		// represents *the file itself* - when you call a method it should be as though you directly edited the file.
		// This reduces the liklihood of concurrent update conflicts by making each action as autonomous as possible.
		// Although this does lead to some extra IO, it's a small price for maintaining the integrity of the file.

		// NB, this is not the place for higher-level functions like searching, task manipulation etc. It's simply 
		// for CRUDing the todo.txt file. 

		List<Task> _tasks = null;
		string _filePath = null;

		public List<Task> Tasks { get { return _tasks; } }

		public TaskList(string filePath)
		{
			_filePath = filePath;
			ReloadTasks();
		}

		public void ReloadTasks()
		{

			Log.Debug("Loading tasks from {0}", _filePath);

			try
			{
				_tasks = new List<Task>();
				//foreach (var line in File.ReadAllLines(_filePath))
				//    _tasks.Add(new Task(line));

				var file = File.OpenRead(_filePath);
				using (var reader = new StreamReader(file))
				{
					string raw;
					while ((raw = reader.ReadLine()) != null)
					{
						_tasks.Add(new Task(raw));
					}

				}
				file.Close();

				Log.Debug("Finished loading tasks from {0}", _filePath);
			}
			catch (IOException ex)
			{
				var msg = "There was a problem trying to read from your todo.txt file";
				Log.Error(msg, ex);
				throw new TaskException(msg, ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				throw;
			}
		}

		public void Add(Task task)
		{
			try
			{
				var output = task.ToString();

				Log.Debug("Adding task '{0}'", output);

				var text = File.ReadAllText(_filePath);
				if (text.Length > 0 && !text.EndsWith(Environment.NewLine))
					output = Environment.NewLine + output;

				File.AppendAllLines(_filePath, new string[] { output });

				Log.Debug("Task '{0}' added", output);

				ReloadTasks();
			}
			catch (IOException ex)
			{
				var msg = "An error occurred while trying to add your task to the task list file";
				Log.Error(msg, ex);
				throw new TaskException(msg, ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				throw;
			}

		}

		public void Delete(Task task)
		{
			try
			{
				Log.Debug("Deleting task '{0}'", task.ToString());

				ReloadTasks(); // make sure we're working on the latest file

				if (_tasks.Remove(_tasks.First(t => t.Raw == task.Raw)))
					File.WriteAllLines(_filePath, _tasks.Select(t => t.ToString()));

				Log.Debug("Task '{0}' deleted", task.ToString());

				ReloadTasks();
			}
			catch (IOException ex)
			{
				var msg = "An error occurred while trying to remove your task from the task list file";
				Log.Error(msg, ex);
				throw new TaskException(msg, ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				throw;
			}
		}


		public void Update(Task currentTask, Task newTask)
		{
			try
			{
				Log.Debug("Updating task '{0}' to '{1}'", currentTask.ToString(), newTask.ToString());

				ReloadTasks();

				// ensure that the task list still contains the current task...
				if (!_tasks.Any(t => t.Raw == currentTask.Raw))
					throw new Exception("That task no longer exists in to todo.txt file");

				var currentIndex = _tasks.IndexOf(_tasks.First(t => t.Raw == currentTask.Raw));

				_tasks[currentIndex] = newTask;

				File.WriteAllLines(_filePath, _tasks.Select(t => t.ToString()));

				Log.Debug("Task '{0}' updated", currentTask.ToString());

				ReloadTasks();
			}
			catch (IOException ex)
			{
				var msg = "An error occurred while trying to update your task int the task list file";
				Log.Error(msg, ex);
				throw new TaskException(msg, ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				throw;
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
