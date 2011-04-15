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
            LoadTasks();    
        }

        private void LoadTasks()
        {
            _tasks = new List<Task>();
            foreach (var line in File.ReadAllLines(_filePath))
            {
                _tasks.Add(new Task(line));
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

                File.AppendAllLines(_filePath, new string[] {output});
                
                LoadTasks();
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
                LoadTasks(); // make sure we're working on the latest file
                if (_tasks.Remove(_tasks.First(t => t.Raw == task.Raw)))
                    File.WriteAllLines(_filePath, _tasks.Select(t => t.ToString()));

                LoadTasks();
            }
            catch (IOException ex)
            {
                throw new TaskException("An error occurred while trying to remove your task from the task list file", ex);
            }
        }

        public void Complete(Task task)
        {

        }

    }
}
