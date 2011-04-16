using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ToDoLib;
using System.IO;

namespace ToDoTests
{
    [TestFixture]
    class TaskListTests
    {
        [Test]
        public void Construct()
        {
            var tl = new TaskList(Data.TestDataPath);
        }


        [Test]
        public void Load_From_File()
        {
            var tl = new TaskList(Data.TestDataPath);
            var tasks = tl.Tasks;

            Assert.AreEqual(2, tasks.Count(x => x.Priority == "(A)"));
        }

        [Test]
        public void Add_ToCollection()
        {
            var task = new Task("(B) This is a test task +test @task");

            var tl = new TaskList(Data.TestDataPath);

            var tasks = tl.Tasks.ToList();
            tasks.Add(task);

            tl.Add(task);

            var newTasks = tl.Tasks.ToList();

            Assert.AreEqual(tasks.Count, newTasks.Count);

            for (int i = 0; i < tasks.Count; i++)
                Assert.AreEqual(tasks[i].ToString(), newTasks[i].ToString());
        }

        [Test]
        public void Add_ToFile()
        {
            var fileContents = File.ReadAllLines(Data.TestDataPath).ToList();
            fileContents.Add("(B) This is a test task +test @task");

            var task = new Task(fileContents.Last());
            var tl = new TaskList(Data.TestDataPath);
            tl.Add(task);

            var newFileContents = File.ReadAllLines(Data.TestDataPath);
            CollectionAssert.AreEquivalent(fileContents, newFileContents);
        }

        [Test]
        public void Delete_InCollection()
        {
            var task = new Task("(B) This is a test task +test @task");
            var tl = new TaskList(Data.TestDataPath);
            tl.Add(task);

            var tasks = tl.Tasks.ToList();
            tasks.Remove(tasks.Last());

            
            tl.Delete(task);

            var newTasks = tl.Tasks.ToList();

            Assert.AreEqual(tasks.Count, newTasks.Count);

            for (int i = 0; i < tasks.Count; i++)
                Assert.AreEqual(tasks[i].ToString(), newTasks[i].ToString());
        }

        [Test]
        public void Delete_InFile()
        {
            var fileContents = File.ReadAllLines(Data.TestDataPath).ToList();
            var task = new Task(fileContents.Last());
            fileContents.Remove(fileContents.Last());

            var tl = new TaskList(Data.TestDataPath);
            tl.Delete(task);

            var newFileContents = File.ReadAllLines(Data.TestDataPath);
            CollectionAssert.AreEquivalent(fileContents, newFileContents);
        }

        [Test]
        public void ToggleComplete_On_InCollection()
        {
            var task = new Task("(B) This is a test task +test @task");
            var tl = new TaskList(Data.TestDataPath);
            tl.Add(task);

            task = tl.Tasks.Last();

            tl.ToggleComplete(task);

            task = tl.Tasks.Last();

            Assert.IsTrue(task.Completed);
        }


        [Test]
        public void ToggleComplete_Off_InCollection()
        {
            var task = new Task("X (B) This is a test task +test @task");
            var tl = new TaskList(Data.TestDataPath);
            tl.Add(task);

            task = tl.Tasks.Last();

            tl.ToggleComplete(task);

            task = tl.Tasks.Last();

            Assert.IsFalse(task.Completed);
        }
    }
}
