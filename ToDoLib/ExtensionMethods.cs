using System;
using System.Collections.Generic;
using System.Linq;

namespace ToDoLib
{
    public static class ExtensionMethods
    {
        public static void Each<T>(this IEnumerable<T> items, Action<T> action)
        {
            if (!items.IsNullOrEmpty())
            {
                foreach (var item in items)
                    if (item != null)
                        action(item);
            }
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> items)
        {
            return items == null || items.Count() == 0;
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

		public static bool IsDateGreaterThan(this string dateString, DateTime date)
		{
			if (dateString.IsNullOrEmpty())
				return false;

			DateTime comparisonDate;
			if (!DateTime.TryParse(dateString, out comparisonDate))
				return false;

			return comparisonDate.Date > date.Date;
		}

		public static bool IsDateLessThan(this string dateString, DateTime date)
		{
			if (dateString.IsNullOrEmpty())
				return false;

			DateTime comparisonDate;
			if (!DateTime.TryParse(dateString, out comparisonDate))
				return false;

			return comparisonDate.Date < date.Date;
		}
    }
}
