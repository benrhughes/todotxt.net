using System;
using System.Threading;
using System.Xml;
using ToDoLib;
using System.ComponentModel;

namespace Client
{
    class UpdateChecker
    {
        public const string updateXMLUrl = @"https://raw.github.com/benrhughes/todotxt.net/master/Updates.xml";
        public const string updateClientUrl = @"http://benrhughes.com/todotxt.net";

		MainWindow _window;
		public UpdateChecker(MainWindow window)
		{
			_window = window;
		}

        public void BeginCheck()
        {
			var worker = new BackgroundWorker();
			var version = "";

			worker.DoWork += (o, e) =>
			{
				try
				{
					var xDoc = new XmlDocument();
					xDoc.Load(new XmlTextReader(updateXMLUrl));

					version = xDoc.SelectSingleNode("//version").InnerText;
				}
				catch (Exception ex)
				{
					Log.Error("Error checking for updates", ex);
				}
			};

			worker.RunWorkerCompleted += (o, e) => { _window.ToggleUpdateMenu(version); };

			worker.RunWorkerAsync();
        }
    }
}
