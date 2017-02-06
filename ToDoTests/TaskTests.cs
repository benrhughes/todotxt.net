﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using ToDoLib;

namespace ToDoTests
{
    [TestFixture]
    public class TaskTests
    {
        List<string> _projects = new List<string>() { "+test" };
        List<string> _contexts = new List<string>() { "@work" };

        #region Create
        [Test]
        public void Create_Priority_Body_Project_Context()
        {
            var task = new TaskItem("(A) This is a test task +test @work");

            var expectedTask = new TaskItem("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Priority_Body_Context_Project()
        {
            var task = new TaskItem("(A) This is a test task @work +test");

            var expectedTask = new TaskItem("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Trailing_Whitespace()
        {
            var task = new TaskItem("(A) This is a test task @work +test  ");

            var expectedTask = new TaskItem("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Null_Priority()
        {
            var task = new TaskItem("This is a test task @work +test ");

            var expectedTask = new TaskItem("", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        
        [Test]
        public void Create_Priority_In_Body()
        {
            var task = new TaskItem("Oh (A) This is a test task @work +test ");

            var expectedTask = new TaskItem("", _projects, _contexts, "Oh (A) This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Priority_Context_Project_Body()
        {
            var task = new TaskItem("(A) @work +test This is a test task");

            var expectedTask = new TaskItem("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Completed()
        {
            var task = new TaskItem("X @work +test This is a test task");

            var expectedTask = new TaskItem("", _projects, _contexts, "This is a test task", "", true);
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_CompletedData()
        {
            var task = new TaskItem("X 2011-05-10 (A) @work +test This is a test task");

            Assert.AreEqual("2011-05-10", task.CompletedDate);
        }

        [Test]
        public void Create_UnCompleted()
        {
            var task = new TaskItem("(A) @work +test This is a test task");

            var expectedTask = new TaskItem("(A)", _projects, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Multiple_Projects()
        {
            var task = new TaskItem("(A) @work +test +test2 This is a test task");

            var expectedTask = new TaskItem("(A)", new List<string>(){"+test", "+test2"}, _contexts, "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Multiple_Contexts()
        {
            var task = new TaskItem("(A) @work @home +test This is a test task");

            var expectedTask = new TaskItem("(A)", _projects, new List<string>(){"@work" , "@home"} , "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_DueDate()
        {
            var task = new TaskItem("(A) due:2011-05-08 @work @home +test This is a test task");

            var expectedTask = new TaskItem("(A)", _projects, new List<string>() { "@work", "@home" }, "This is a test task", "2011-05-08", false);
            
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_DueToday()
        {
            var task = new TaskItem("(A) due:today @work @home +test This is a test task");

            string due = DateTime.Now.ToString("yyyy-MM-dd");

            var expectedTask = new TaskItem("(A)", _projects, new List<string>() { "@work", "@home" }, "This is a test task", due, false);
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_DueTomorrow()
        {
            var task = new TaskItem("(A) due:tOmORRoW @work @home +test This is a test task");

            string due = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            var expectedTask = new TaskItem("(A)", _projects, new List<string>() { "@work", "@home" }, "This is a test task", due, false);
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_DueDayOfWeek()
        {
            var task = new TaskItem("(A) due:thUrsday @work @home +test This is a test task");

            var dueDate=DateTime.Now;
            do{
                dueDate=dueDate.AddDays(1);
            } while (!string.Equals(dueDate.ToString("dddd"), "thursday", StringComparison.CurrentCultureIgnoreCase));
            string due = dueDate.ToString("yyyy-MM-dd");

            var expectedTask = new TaskItem("(A)", _projects, new List<string>() { "@work", "@home" }, "This is a test task", due, false);
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_CreationDate()
        {
            var task = new TaskItem("(A) 2011-05-07 due:2011-05-08 @work @home +test This is a test task");

            Assert.AreEqual("2011-05-07", task.CreationDate);
        }

		[Test]
		public void Create_Project_with_non_alpha()
		{
			var task = new TaskItem("This is a test task +work&home");

			Assert.AreEqual("+work&home", task.Projects[0]);
		}
        #endregion

        #region ToString
        [Test]
        public void ToString_From_Raw()
        {
            var task = new TaskItem("(A) @work +test This is a test task");
            Assert.AreEqual("(A) @work +test This is a test task", task.ToString());
        }

        [Test]
        public void ToString_From_Parameters()
        {
            var task = new TaskItem("(A)", _projects, _contexts, "This is a test task");
            Assert.AreEqual("(A) This is a test task +test @work", task.ToString());
        }
        #endregion

        [Test]
        public void Completed_adds_x_to_begining()
        {
            var t = new TaskItem("A new task");
            t.Completed = true;


        }

        void AssertEquivalence(TaskItem t1, TaskItem t2)
        {
            Assert.AreEqual(t1.Priority, t2.Priority);
            CollectionAssert.AreEquivalent(t1.Projects, t2.Projects);
            CollectionAssert.AreEquivalent(t1.Contexts, t2.Contexts);
            Assert.AreEqual(t1.DueDate, t2.DueDate);
            Assert.AreEqual(t1.Completed, t2.Completed);
            Assert.AreEqual(t1.Body, t2.Body);
        }

        #region Test Propery IsTaskDue

        [Test]
        public void Task_with_out_due_date()
        {
            var t = new TaskItem("Task with out due date task");
            Assert.AreEqual(t.IsTaskDue, Due.NotDue);
        }

        [Test]
        public void Task_Complete_with_out_due_date()
        {
            var t = new TaskItem("x Task Complete with out due date task");
            Assert.AreEqual(t.IsTaskDue, Due.NotDue);
        }

        [Test]
        public void Task_with_future_due_date()
        {
            var t = new TaskItem("Task with future task due:" + DateTime.Now.AddDays(2).ToString("yyyy-MM-dd"));
            Assert.AreEqual(t.IsTaskDue, Due.NotDue);
        }

        [Test]
        public void Task_Complete_with_future_due_date()
        {
            var t = new TaskItem("x Task Complete with future task due:" + DateTime.Now.AddDays(2).ToString("yyyy-MM-dd"));
            Assert.AreEqual(t.IsTaskDue, Due.NotDue);
        }

        [Test]
        public void Task_with_today_due_date()
        {
            var t = new TaskItem("Task with today due:" + DateTime.Now.ToString("yyyy-MM-dd"));
            Assert.AreEqual(t.IsTaskDue, Due.Today);
        }

        [Test]
        public void Task_Complete_with_today_due_date()
        {
            var t = new TaskItem("x Task Complete with today due:" + DateTime.Now.ToString("yyyy-MM-dd"));
            Assert.AreEqual(t.IsTaskDue, Due.NotDue);
        }

        [Test]
        public void Task_with_over_due_date()
        {
            var t = new TaskItem("Task with overdue date due:" + DateTime.Now.AddDays(-4).ToString("yyyy-MM-dd"));
            Assert.AreEqual(t.IsTaskDue, Due.Overdue);

        }

        [Test]
        public void Task_Complete_with_over_due_date()
        {
            var t = new TaskItem("x Task Complete with overdue date due:" + DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"));
            Assert.AreEqual(t.IsTaskDue, Due.NotDue);
        }

        #endregion
    }
}
