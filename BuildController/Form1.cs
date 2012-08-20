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

		private void button1_Click(object sender, EventArgs e)
		{
			var message = 
@"The build controller is mostly useful if you want to build and deploy 
the todotxt.net installer, including pushing version changes to github.
That is, it's really for core contributors. However, if you have a fork 
you may find it helpful for building the installer etc.";

			MessageBox.Show(message, "Huh?", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
