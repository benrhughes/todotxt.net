using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ToDoLib;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace ToDoTests
{
    [TestFixture]
    class TaskListTests
    {

		[TestFixtureSetUp]
		public void TFSetup()
		{
			if (!File.Exists(Data.TestDataPath))
				File.WriteAllText(Data.TestDataPath, "");
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			if (File.Exists(Data.TestDataPath))
				File.Delete(Data.TestDataPath);
		}
        
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
        }

        [Test]
        public void Add_ToCollection()
        {
            var task = new Task("(B) Add_ToCollection +test @task");

            var tl = new TaskList(Data.TestDataPath);

            var tasks = new List<Task>(tl.Tasks);
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
            fileContents.Add("(B) Add_ToFile +test @task");

            var task = new Task(fileContents.Last());
            var tl = new TaskList(Data.TestDataPath);
            tl.Add(task);

            var newFileContents = File.ReadAllLines(Data.TestDataPath);
            CollectionAssert.AreEquivalent(fileContents, newFileContents);
        }

        [Test]
        public void Add_To_Empty_File()
        {
            // v0.3 and earlier contained a bug where a blank task was added

            File.WriteAllLines(Data.TestDataPath, new string[] { }); // empties the file

            var tl = new TaskList(Data.TestDataPath);
            tl.Add(new Task("A task"));

            Assert.AreEqual(1,tl.Tasks.Count());

        }

        [Test]
        public void Add_Multiple()
        {
            var tl = new TaskList(Data.TestDataPath);
            var c = tl.Tasks.Count();

            var task = new Task("Add_Multiple task one");
            tl.Add(task);

            var task2 = new Task("Add_Multiple task two");
            tl.Add(task2);

            Assert.AreEqual(c + 2, tl.Tasks.Count());
        }

        [Test]
        public void Delete_InCollection()
        {
            var task = new Task("(B) Delete_InCollection +test @task");
            var tl = new TaskList(Data.TestDataPath);
            tl.Add(task);

            var tasks = new List<Task>(tl.Tasks);
            tasks.Remove(tasks.Where(x => x.Raw == task.Raw).First());

            
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
        public void Update_InCollection()
        {
            var task = new Task("(B) Update_InCollection +test @task");

            var tl = new TaskList(Data.TestDataPath);
            tl.Add(task);

            var task2 = new Task(task.Raw);
            task2.Completed = true;

            tl.Update(task, task2);

            var newTask = tl.Tasks.Last();
            Assert.IsTrue(newTask.Completed);
        }

        [Test]
        public void TestEmptyFilter()
        {
            TaskList tl = new TaskList(Data.TestDataPath);
            Assert.AreEqual(TaskList.FilterList(tl.Tasks, true, "").Count, tl.Tasks.Count);
        }

        [Test]
        public void TestFilterCountantCaseSensitive()
        {
            var tl = getTestList();
            Assert.AreEqual(TaskList.FilterList(tl, true, "XXXXXX").Count, 2);
            Assert.AreEqual(TaskList.FilterList(tl, true, "xxxxxx").Count, 1);
        }

        [Test]
        public void TestFilterCountantNoCaseSensitive()
        {
            var tl = getTestList();

            Assert.AreEqual(TaskList.FilterList(tl, false, "XXXXXX").Count, 3);
            Assert.AreEqual(TaskList.FilterList(tl, false, "XXXXXX").Count, 3);
        }

        [Test]
        public void TestFilterTextSplite()
        {
            var tl = getTestList();
            Assert.AreEqual(TaskList.FilterList(tl, false, "XXXXXX yyyyyy").Count, 1);
        }

        [Test]
        public void TestFilterExclude()
        {
            var tl = getTestList();

            Assert.AreEqual(TaskList.FilterList(tl, true, "-XXXXXX").Count, 5);
            Assert.AreEqual(TaskList.FilterList(tl, false, "-XXXXXX").Count, 4);
        }

		[Test]
		public void Filter_due_today()
		{
			var t = getTestList();
			t.Add(new Task("Oh my due:today"));

			Assert.AreEqual(TaskList.FilterList(t, false, "due:today").Count, 1);
		}

		[Test]
		public void Filter_due_future()
		{
			var t = getTestList();
			t.Add(new Task("Oh my due:tomorrow"));

			Assert.AreEqual(TaskList.FilterList(t, false, "due:future").Count, 1);
		}

		[Test]
		public void Filter_due_past()
		{
			var t = getTestList();

			Assert.AreEqual(TaskList.FilterList(t, false, "due:past").Count, 3);
		}

        [Test]
        public void TestSortAlphabetical()
        {
            var tasks = getTestList();
            var newOrder = TaskList.SortList(SortType.Alphabetical, tasks);

            Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[6].ToString());
            Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[4].ToString());
        }

        [Test]
        public void TestSortCompleted()
        {
            var tasks = getTestList();
            var newOrder = TaskList.SortList(SortType.Completed, tasks);

            Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[0].ToString());
            Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[5].ToString());
        }

        [Test]
        public void TestSortContext()
        {
            var tasks = getTestList();
            var newOrder = TaskList.SortList(SortType.Context, tasks);

            Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[1].ToString());
            Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[5].ToString());
        }

        [Test]
        public void TestSortDueDate()
        {
            var tasks = getTestList();
            var newOrder = TaskList.SortList(SortType.DueDate, tasks);

            Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[1].ToString());
            Assert.AreEqual(newOrder.Last<Task>().ToString(),tasks[5].ToString());
        }

        [Test]
        public void TestSortPriority()
        {
            var tasks = getTestList();
            var newOrder = TaskList.SortList(SortType.Priority, tasks);

            Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[6].ToString());
            Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[4].ToString());
        }

        [Test]
        public void TestSortProject()
        {
            var tasks = getTestList();
            var newOrder = TaskList.SortList(SortType.Project, tasks);

            Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[1].ToString());
            Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[5].ToString());
        }

        [Test]
        public void TestSortNone()
        {
            var tasks = getTestList();

            var newOrder = TaskList.SortList(SortType.None, tasks);
            var lastOrder = tasks.GetEnumerator();
            lastOrder.MoveNext();

            foreach (var task in newOrder)
            {
                Assert.AreEqual (task.ToString(), lastOrder.Current.ToString());
                lastOrder.MoveNext();
            }
        }

		[Test]
		public void Read_when_file_is_open_in_another_process()
		{
			var t = new TaskList(Data.TestDataPath);
		
			var thread = new Thread(x =>
				{
					try
					{
						var f = File.Open(Data.TestDataPath, FileMode.Open, FileAccess.ReadWrite);
						using (var s = new StreamWriter(f))
						{
							s.WriteLine("hello");
							s.Flush();
						}
						Thread.Sleep(500);						
					}
					catch (Exception ex)
					{
						Console.WriteLine("Exception while opening in background thread " + ex.Message);
					}
				});

			thread.Start();
			Thread.Sleep(100);

			try
			{
				t.ReloadTasks();
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
			finally
			{
				thread.Join();
			}

		}

        private List<Task> getTestList()
        {
            var tl = new List<Task>();
            tl.Add(new Task("(c) 3test +test2 due:2000-01-03"));//0
            tl.Add(new Task("(d) 1test +test1 @test1 due:2000-01-01"));//1
            tl.Add(new Task("x test XXXXXX "));//2
            tl.Add(new Task("x test xxxxxx due:2000-01-01"));//3
            tl.Add(new Task("x test XXXXXX yyyyyy"));//4
            tl.Add(new Task("x (a) test YYYYYY"));//5
            tl.Add(new Task("(b) 2test +test1 @test2 "));//6
            return tl;
        }

    }
}
