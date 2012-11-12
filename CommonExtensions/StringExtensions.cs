using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonExtensions
{
	public static class StringExtensions
	{
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
