using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
	public class WindowLocation
	{
		public WindowLocation()
		{
			Left = User.Default.WindowLeft;
			Top = User.Default.WindowTop;
			Height = User.Default.WindowHeight;
			Width = User.Default.WindowWidth;
		}

		public double Left { get; set; }
		public double Top { get; set; }
		public double Height { get; set; }
		public double Width { get; set; }
	}
}
