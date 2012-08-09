using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BuildController
{
	// Runs the specified build steps.
	// To add a new build step, add a new item to the _actions dictionary. It will 
	// automatically appear in the UI
	public class Runner
	{
		private Dictionary<string, Action> _actions;
		private string _version, _changelog;

		public IEnumerable<string> ActionNames { get { return _actions.Keys; } }

		public Runner()
		{
			_actions = new Dictionary<string, Action>()
			{
				{"Update Assembly Info", () => UpdateAssemblyInfo()},
				{"MSBuild", () => MSBuild()},
				{"nUnit", () => NUnit()},
				{"Update Installer", () =>UpdateInstaller()},
				{"Build Installer", () => BuildInstaller()},
				{"Update updates.xml", () => UpdateXML()},
				{"Push to Github", () => PushToGithub()}
			};
		}

		public void Run(string version, string changelog, IEnumerable<string> actionNames)
		{
			_version = version;
			_changelog = changelog;

			foreach (var name in actionNames)
				_actions[name]();
		}

		private void UpdateAssemblyInfo()
		{
			MessageBox.Show("Update Assembly Info");
		}

		private void MSBuild()
		{
			throw new NotImplementedException();
		}

		private void NUnit()
		{
			MessageBox.Show("nUnit");
		}

		private void UpdateInstaller()
		{
			throw new NotImplementedException();
		}

		private void BuildInstaller()
		{
			throw new NotImplementedException();
		}

		private void UpdateXML()
		{
			throw new NotImplementedException();
		}

		private void PushToGithub()
		{
			throw new NotImplementedException();
		}

	}
}
