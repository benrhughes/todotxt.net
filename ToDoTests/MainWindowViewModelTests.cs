using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ToDoLib;
using Client;

namespace ToDoTests
{

    public class MainWindowViewModelTester : MainWindowViewModel
    {
        public MainWindowViewModelTester() : base(null)
        {
                
        }
        public void UpdateSummaryTester(List<Task> selectedTasks)
        {
            base.UpdateSummary(selectedTasks);
        }
    }

	// these tests currently point to TaskList functionality for sorting and 
	// filtering, which has been moved to the viewmodel. They need to 
	// be fixed up
    [TestFixture]
	class MainWindowViewModelTests
	{
        //    [Test]
        //    public void TestEmptyFilter()
        //    {
        //        TaskList tl = new TaskList(Data.TestDataPath);
        //        Assert.AreEqual(TaskList.FilterList(tl.Tasks, true, "").Count, tl.Tasks.Count);
        //    }

        //    [Test]
        //    public void TestFilterCountantCaseSensitive()
        //    {
        //        var tl = getTestList();
        //        Assert.AreEqual(TaskList.FilterList(tl, true, "XXXXXX").Count, 2);
        //        Assert.AreEqual(TaskList.FilterList(tl, true, "xxxxxx").Count, 1);
        //    }

        //    [Test]
        //    public void TestFilterCountantNoCaseSensitive()
        //    {
        //        var tl = getTestList();

        //        Assert.AreEqual(TaskList.FilterList(tl, false, "XXXXXX").Count, 3);
        //        Assert.AreEqual(TaskList.FilterList(tl, false, "XXXXXX").Count, 3);
        //    }

        //    [Test]
        //    public void TestFilterTextSplite()
        //    {
        //        var tl = getTestList();
        //        Assert.AreEqual(TaskList.FilterList(tl, false, "XXXXXX yyyyyy").Count, 1);
        //    }

        //    [Test]
        //    public void TestFilterExclude()
        //    {
        //        var tl = getTestList();

        //        Assert.AreEqual(TaskList.FilterList(tl, true, "-XXXXXX").Count, 5);
        //        Assert.AreEqual(TaskList.FilterList(tl, false, "-XXXXXX").Count, 4);
        //    }

        //    [Test]
        //    public void Filter_due_today()
        //    {
        //        var t = getTestList();
        //        t.Add(new Task("Oh my due:today"));

        //        Assert.AreEqual(TaskList.FilterList(t, false, "due:today").Count, 1);
        //    }

        //    [Test]
        //    public void Filter_due_future()
        //    {
        //        var t = getTestList();
        //        t.Add(new Task("Oh my due:tomorrow"));

        //        Assert.AreEqual(TaskList.FilterList(t, false, "due:future").Count, 1);
        //    }

        //    [Test]
        //    public void Filter_due_past()
        //    {
        //        var t = getTestList();

        //        Assert.AreEqual(TaskList.FilterList(t, false, "due:past").Count, 3);
        //    }

        //    [Test]
        //    public void TestSortAlphabetical()
        //    {
        //        var tasks = getTestList();
        //        var newOrder = TaskList.SortList(SortType.Alphabetical, tasks);

        //        Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[6].ToString());
        //        Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[4].ToString());
        //    }

        //    [Test]
        //    public void TestSortCompleted()
        //    {
        //        var tasks = getTestList();
        //        var newOrder = TaskList.SortList(SortType.Completed, tasks);

        //        Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[0].ToString());
        //        Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[5].ToString());
        //    }

        //    [Test]
        //    public void TestSortContext()
        //    {
        //        var tasks = getTestList();
        //        var newOrder = TaskList.SortList(SortType.Context, tasks);

        //        Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[1].ToString());
        //        Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[5].ToString());
        //    }

        //    [Test]
        //    public void TestSortDueDate()
        //    {
        //        var tasks = getTestList();
        //        var newOrder = TaskList.SortList(SortType.DueDate, tasks);

        //        Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[1].ToString());
        //        Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[5].ToString());
        //    }

        //    [Test]
        //    public void TestSortPriority()
        //    {
        //        var tasks = getTestList();
        //        var newOrder = TaskList.SortList(SortType.Priority, tasks);

        //        Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[6].ToString());
        //        Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[4].ToString());
        //    }

        //    [Test]
        //    public void TestSortProject()
        //    {
        //        var tasks = getTestList();
        //        var newOrder = TaskList.SortList(SortType.Project, tasks);

        //        Assert.AreEqual(newOrder.First<Task>().ToString(), tasks[1].ToString());
        //        Assert.AreEqual(newOrder.Last<Task>().ToString(), tasks[5].ToString());
        //    }

        //    [Test]
        //    public void TestSortNone()
        //    {
        //        var tasks = getTestList();

        //        var newOrder = TaskList.SortList(SortType.None, tasks);
        //        var lastOrder = tasks.GetEnumerator();
        //        lastOrder.MoveNext();

        //        foreach (var task in newOrder)
        //        {
        //            Assert.AreEqual(task.ToString(), lastOrder.Current.ToString());
        //            lastOrder.MoveNext();
        //        }
        //    }

        [Test]
        public void UpdateSummary_All_Counts_Zero_By_Default()
        {
            var viewModel = new MainWindowViewModelTester();
            Assert.AreEqual(0, viewModel.IncompleteTasks);
            Assert.AreEqual(0, viewModel.TasksDueToday);
            Assert.AreEqual(0, viewModel.TasksOverdue);
        }


        [Test]
        public void UpdateSummary_IncompleteTasks_Are_Counted()
        {
            var viewModel = new MainWindowViewModelTester();
            var taskList = GenerateTaskList();
            viewModel.UpdateSummaryTester(taskList);
            Assert.AreEqual(2, viewModel.IncompleteTasks);
        }

        [Test]
        public void UpdateSummary_DueToday_Counts_Incompletes_Only()
        {
            var viewModel = new MainWindowViewModelTester();
            var taskList = GenerateTaskList(); // 1 incomplete task due today, 1 task due today, but completed.
            viewModel.UpdateSummaryTester(taskList);
            Assert.AreEqual(1, viewModel.TasksDueToday);
        }

        [Test]
        public void UpdateSummary_Overdue_Counts_Incompletes_With_Past_Due_Dates()
        {
            var viewModel = new MainWindowViewModelTester();
            var taskList = GenerateTaskList(); 
            //Add an incomplete task due yesterday
            taskList.Add(new Task("C", new List<String>(), new List<string>(), "Overdue incomplete task", CreateDueDate(-2), false));
            //Add a complete task due yesterday
            taskList.Add(new Task("C", new List<String>(), new List<string>(), "Overdue incomplete task", CreateDueDate(-2), true));

            viewModel.UpdateSummaryTester(taskList);
            Assert.AreEqual(1, viewModel.TasksOverdue);
        }

        private string CreateDueDate(int offsetDays)
        {
            return DateTime.Today.AddDays(offsetDays).ToString("yyyy-MM-dd");

        }

        private static List<Task> GenerateTaskList()
        {
            String today = DateTime.Today.ToString("yyyy-MM-dd");
            Console.WriteLine(today);
            return new List<Task>
            {
                new Task("A", new List<String>(), new List<string>(), "Incomplete Task", today), // Incomplete task, due today
                new Task("A", new List<String>(), new List<string>(), "Incomplete Task2"), // Incomplete task2
                new Task("A", new List<String>(), new List<string>(), "Complete Task", today, true), // Complete task that was due today

            };
        }
    }
}
