using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BuildController
{
	public partial class Form1 : Form
	{
		Runner _runner;

		public Form1()
		{
			InitializeComponent();
			
			_runner = new Runner();

			foreach (var name in _runner.ActionNames)
				clbOptions.Items.Add(name);
		}

		private void btnRun_Click(object sender, EventArgs e)
		{
			_runner.Run(tbVersion.Text, tbChangelog.Text, clbOptions.CheckedItems.Cast<string>());
		}
	}
}
