using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ToDoLib;

namespace ToDoTests
{
    [TestFixture]
    class OrderedListTests
    {
        [Test]
        public void List_is_sorted_by_sort_function()
        {
            var ol = new OrderedList<int>();

            ol.SortFunc = (x) => x;

            ol.Add(4);
            ol.Add(2);
            ol.Add(3);
            ol.Add(1);

            var expected = new int[] { 1, 2, 3, 4 };

            CollectionAssert.AreEqual(expected, ol);
        }

        [Test]
        public void Constructor_takes_sort_func()
        {
            Assert.DoesNotThrow(() => { var ol = new OrderedList<int>(x => x); });
        }

        [Test]
        public void Changing_the_SortFunc_property_changes_the_sort_order()
        {
            var ol = new OrderedList<string>(x => x);

            ol.Add("Cheese");
            ol.Add("Acorn");
            ol.Add("Blah");

            CollectionAssert.AreEqual(new string[] { "Acorn", "Blah", "Cheese" }, ol);

            ol.SortFunc = x => x.Last().ToString();

            CollectionAssert.AreEqual(new string[] { "Cheese", "Blah", "Acorn" }, ol);

        }

        [Test]
        public void Remove_removes_items()
        {
            var s = "a string";
            var list = new OrderedList<string>();

            list.Add(s);

            Assert.IsTrue(list.Contains(s));

            Assert.IsTrue(list.Remove(s));

            Assert.IsFalse(list.Contains(s));
        }
    }
}
