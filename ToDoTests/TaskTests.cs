using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ToDoLib;

namespace ToDoTests
{
    [TestFixture]
    public class TaskTests
    {
        [Test]
        public void Create_Priority_Body_Project_Context()
        {
            var task = new Task("(A) This is a test task +test @work");

            var expectedTask = new Task("(A)", "+test", "@work", "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Priority_Body_Context_Project()
        {
            var task = new Task("(A) This is a test task @work +test");

            var expectedTask = new Task("(A)", "+test", "@work", "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Trailing_Whitespace()
        {
            var task = new Task("(A) This is a test task @work +test  ");

            var expectedTask = new Task("(A)", "+test", "@work", "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Null_Priority()
        {
            var task = new Task("This is a test task @work +test ");

            var expectedTask = new Task("z", "+test", "@work", "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        
        [Test]
        public void Create_Priority_In_Body()
        {
            var task = new Task("Oh (A) This is a test task @work +test ");

            var expectedTask = new Task("z", "+test", "@work", "Oh (A) This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Priority_Context_Project_Body()
        {
            var task = new Task("(A) @work +test This is a test task");

            var expectedTask = new Task("(A)", "+test", "@work", "This is a test task");
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_Completed()
        {
            var task = new Task("X (A) @work +test This is a test task");

            var expectedTask = new Task("(A)", "+test", "@work", "This is a test task", true);
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void Create_UnCompleted()
        {
            var task = new Task("(A) @work +test This is a test task");

            var expectedTask = new Task("(A)", "+test", "@work", "This is a test task", false);
            AssertEquivalence(expectedTask, task);
        }

        [Test]
        public void ToString_From_Raw()
        {
            var task = new Task("(A) @work +test This is a test task");
            Assert.AreEqual("(A) @work +test This is a test task", task.ToString());
        }

        [Test]
        public void ToString_From_Parameters()
        {
            var task = new Task("(A)", "+test", "@work", "This is a test task");
            Assert.AreEqual("(A) This is a test task +test @work", task.ToString());
        }

        void AssertEquivalence(Task t1, Task t2)
        {
            Assert.AreEqual(t1.Priority, t2.Priority);
            Assert.AreEqual(t1.Project, t2.Project);
            Assert.AreEqual(t1.Context, t2.Context);
            Assert.AreEqual(t1.Body, t2.Body);
        }
    }
}
