using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ToDoLib
{
    public class TaskList
    {
        List<Task> _tasks;
        string _filePath;

        public IEnumerable<Task> Tasks { get { return _tasks; } }

        public TaskList(string filePath)
        {
            _filePath = filePath;
            ReloadTasks();
        }

        public void ReloadTasks()
        {
            try
            {
                _tasks = new List<Task>();
                foreach (var line in File.ReadAllLines(_filePath))
                {
                    _tasks.Add(new Task(line));
                }
            }
            catch (IOException ex)
            {
                throw new TaskException("There was a problem trying to read from your todo.txt file", ex);
            }
        }

        public void Add(Task task)
        {
            try
            {
                var output = task.ToString();

                var text = File.ReadAllText(_filePath);
                if (!text.EndsWith(Environment.NewLine))
                    output = Environment.NewLine + output;

                File.AppendAllLines(_filePath, new string[] { output });

                ReloadTasks();
            }
            catch (IOException ex)
            {
                throw new TaskException("An error occurred while trying to add your task to the task list file", ex);
            }

        }

        public void Delete(Task task)
        {
            try
            {
                ReloadTasks(); // make sure we're working on the latest file
                if (_tasks.Remove(_tasks.First(t => t.Raw == task.Raw)))
                    File.WriteAllLines(_filePath, _tasks.Select(t => t.ToString()));

                ReloadTasks();
            }
            catch (IOException ex)
            {
                throw new TaskException("An error occurred while trying to remove your task from the task list file", ex);
            }
        }

        public void ToggleComplete(Task task)
        {
            var newTask = new Task(task.Raw);
            newTask.Completed = !newTask.Completed;
            Update(task, newTask);
        }


        public void Update(Task currentTask, Task newTask)
        {
            try
            {
                ReloadTasks();
                var currentIndex = _tasks.IndexOf(_tasks.First(t => t.Raw == currentTask.Raw));

                _tasks[currentIndex] = newTask;

                File.WriteAllLines(_filePath, _tasks.Select(t => t.ToString()));
            }
            catch (IOException ex)
            {
                throw new TaskException("An error occurred while trying to update your task int the task list file", ex);
            }

            ReloadTasks();
        }
    }
}
