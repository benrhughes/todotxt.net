using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonExtensions;

namespace ToDoLib
{
	/// <summary>
	/// A thin data access abstraction over the actual todo.txt file
	/// </summary>
	public class TaskList
	{
		// It may look like an overly simple approach has been taken here, but it's well considered. This class
		// represents *the file itself* - when you call a method it should be as though you directly edited the file.
		// This reduces the likelihood of concurrent update conflicts by making each action as autonomous as possible.
		// Although this does lead to some extra IO, it's a small price for maintaining the integrity of the file.

		// NB, this is not the place for higher-level functions like searching, task manipulation etc. It's simply 
        // for CRUDing the todo.txt file. 

        #region Properties

        string _filePath = null;
		string _preferredLineEnding = null;
		public List<TaskItem> Tasks { get; private set; }

        // Task List MetaData
        public List<string> Projects { get; private set; }
        public List<string> Contexts { get; private set; }
        public List<string> Priorities { get; private set; }
	    public bool PreserveWhiteSpace { get; set; }

	    #endregion

        #region Constructor

        public TaskList(string filePath, bool preserveWhitespace = false)
        {
            _filePath = filePath;
            _preferredLineEnding = Environment.NewLine;
            PreserveWhiteSpace = preserveWhitespace;
            ReloadTasks();
        }

        #endregion

        #region Events
        public event EventHandler Modified;
        #endregion

        #region Task List Metadata Methods

        public void UpdateTaskListMetaData()
        {
            var UniqueProjects = new SortedSet<string>();
            var UniqueContexts = new SortedSet<string>();
            var UniquePriorities = new SortedSet<string>();

            foreach (TaskItem t in Tasks)
            {
                foreach (string p in t.Projects)
                {
                    UniqueProjects.Add(p);
                }
                foreach (string c in t.Contexts)
                {
                    UniqueContexts.Add(c);
                }
                UniquePriorities.Add(t.Priority);
            }

            this.Projects = UniqueProjects.ToList<string>();
            this.Contexts = UniqueContexts.ToList<string>();
            this.Priorities = UniquePriorities.ToList<string>();
        }

        #endregion

        public void ReloadTasks()
		{
			Log.Debug("Loading tasks from {0}.", _filePath);

			try
			{
				Tasks = new List<TaskItem>();

				var file = File.OpenRead(_filePath);
				using (var reader = new StreamReader(file))
				{
					string raw;
					while ((raw = reader.ReadLine()) != null) 
					{
						if (!raw.IsNullOrEmpty() || PreserveWhiteSpace)
                        {
                            Tasks.Add(new TaskItem(raw));
                        }                            
					}
				}

				Log.Debug("Finished loading tasks from {0}.", _filePath);
				
				_preferredLineEnding = GetPreferredFileLineEndingFromFile();
			}
			catch (IOException ex)
			{
				var msg = "There was a problem trying to read from your todo.txt file.";
				Log.Error(msg, ex);
				throw new TaskException(msg, ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				throw;
			}
            finally
            {
                UpdateTaskListMetaData();
                RaiseModifiedEvent();
            }
		}

		public void Add(TaskItem task)
		{
			try
			{
				var output = task.ToString();

				Log.Debug("Adding task '{0}'", output);

				var text = File.ReadAllText(_filePath);
                if (text.Length > 0 && !text.EndsWith(_preferredLineEnding))
                {
                    output = _preferredLineEnding + output;
                }

				File.AppendAllLines(_filePath, new string[] { output });

                Tasks.Add(task);

				Log.Debug("Task '{0}' added", output);
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
            finally
            {
                UpdateTaskListMetaData();
                RaiseModifiedEvent();
            }
		}

        private void RaiseModifiedEvent()
        {
            if(Modified != null)
            {
                Modified(this, new EventArgs());
            }
        }

        public void Delete(TaskItem task)
		{
			try
			{
				Log.Debug("Deleting task '{0}'", task.ToString());

				if (Tasks.Remove(Tasks.First(t => t.Raw == task.Raw)))
				{
					WriteAllTasksToFile();
				}

				Log.Debug("Task '{0}' deleted", task.ToString());
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
            finally
            {
                UpdateTaskListMetaData();
                RaiseModifiedEvent();
            }
		}

        /// <summary>
        /// This method updates one task in the file. It works by replacing the "current task" with the "new task".
        /// </summary>
        /// <param name="currentTask">The task to replace.</param>
        /// <param name="newTask">The replacement task.</param>
        /// <param name="reloadTasksPriorToUpdate">Optionally reload task file prior to the update. Default is TRUE.</param>
        /// <param name="writeTasks">Optionally write task file after the update. Default is TRUE.</param>
        /// <param name="reloadTasksAfterUpdate">Optionally reload task file after the update. Default is TRUE.</param>
        public void Update(TaskItem currentTask, TaskItem newTask, bool writeTasks = true)
		{
            Log.Debug("Updating task '{0}' to '{1}'", currentTask.ToString(), newTask.ToString());

			try
			{

				// ensure that the task list still contains the current task...
				if (!Tasks.Any(t => t.Raw == currentTask.Raw))
                { 
					throw new Exception("That task no longer exists in to todo.txt file.");
                }

                var currentIndex = Tasks.IndexOf(Tasks.First(t => t.Raw == currentTask.Raw));
                Tasks[currentIndex] = newTask;

                Log.Debug("Task '{0}' updated", currentTask.ToString());

                if (writeTasks)
                {
                    WriteAllTasksToFile();
                }
			}
			catch (IOException ex)
			{
				var msg = "An error occurred while trying to update your task in the task list file.";
				Log.Error(msg, ex);
				throw new TaskException(msg, ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				throw;
			}
            finally
            {
                UpdateTaskListMetaData();
                RaiseModifiedEvent();
            }
		}

		protected string GetPreferredFileLineEndingFromFile()
		{
			try
			{
				using (StreamReader fileStream = new StreamReader(_filePath))
				{
					char previousChar = '\0';
			
					// Read the first 4000 characters to try and find a newline
					for (int i = 0; i < 4000; i++)
					{
						int b = fileStream.Read();
						if (b == -1) break;
			
						char currentChar = (char)b;
			            if (currentChar == '\n')
			            {
			                return (previousChar == '\r') ? "\r\n" : "\n";
			            }
			            
			            previousChar = currentChar;
					}
			
					// if no newline found, use the default newline character for the environment
					return Environment.NewLine;
				}
			}
			catch (IOException ex)
			{
				var msg = "An error occurred while trying to read the task list file";
				Log.Error(msg, ex);
				throw new TaskException(msg, ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				throw;
			}
		}
		
		protected void WriteAllTasksToFile()
		{
			try
			{
				using (StreamWriter writer = new StreamWriter(_filePath))
				{
					writer.NewLine = _preferredLineEnding;
					Tasks.ForEach((TaskItem t) => { writer.WriteLine(t.ToString()); });
					writer.Close();
				}
			}
			catch (IOException ex)
			{
				var msg = "An error occurred while trying to write to the task list file.";
				Log.Error(msg, ex);
				throw new TaskException(msg, ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				throw;
			}
		}
	}
}
