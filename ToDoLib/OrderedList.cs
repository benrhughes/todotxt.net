using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToDoLib
{
    public class OrderedList<T> : IList<T>
    {
        List<T> _list;

        Func<T, T> _sort;

        public OrderedList() : this(x => x)
        {
        }

        public OrderedList(IEnumerable<T> items) : this(x => x, items)
        {

        }

        public OrderedList(Func<T,T> sortFunc, IEnumerable<T> items = null)
        {
            _list = items == null ? new List<T>() : new List<T>(items);
            _sort = sortFunc;
        }

        public Func<T, T> SortFunc
        {
            get
            {
                return _sort;
            }
            set
            {
                _sort = value;
                _list = _list.OrderBy(_sort).ToList();
            }
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            _list = _list.OrderBy(SortFunc).ToList();
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            _list = _list.OrderBy(SortFunc).ToList();
        }

        public T this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                _list[index] = value;
                _list = _list.OrderBy(SortFunc).ToList();
            }
        }

        public void Add(T item)
        {
            _list.Add(item);
            _list = _list.OrderBy(SortFunc).ToList();
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            var val = _list.Remove(item);
            _list = _list.OrderBy(SortFunc).ToList();
            return val;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
