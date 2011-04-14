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

        public IEnumerable<Task> Tasks { get { return _tasks; } }

        public TaskList()
        {
            _tasks = new List<Task>();
            foreach (var rawText in RawTaskText())
            {
                _tasks.Add(new Task(rawText));
            }
        }


        public static void Add(Task task)
        {

        }

        public static void Delete(Task task)
        {

        }


        static IEnumerable<string> RawTaskText()
        {
            return File.ReadAllLines(@"..\..\..\ToDoTests\testtasks.txt");
        }
    }
}
