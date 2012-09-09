using System;
using System.Threading;
using System.Xml;
using ToDoLib;

namespace Client
{
    class CheckUpdate
    {
        public const string updateXMLUrl = @"https://raw.github.com/benrhughes/todotxt.net/master/Updates.xml";
        public const string updateClientUrl = @"http://benrhughes.com/todotxt.net";

        public delegate void CheckUpdateVersion(string version);
        public event CheckUpdateVersion OnCheckedUpdateVersion;

        private XmlDocument xDoc;

        public CheckUpdate()
        {
            xDoc = new XmlDocument();
        }

        public void Check()
        {
            ThreadPool.QueueUserWorkItem(x => CheckForUpdates());
        }

        private void CheckForUpdates()
        {
            try
            {
                xDoc.Load(new XmlTextReader(updateXMLUrl));

                var version = xDoc.SelectSingleNode("//version").InnerText;
                var changelog = xDoc.SelectSingleNode("//changelog").InnerText;
                OnCheckedUpdateVersion(version);
            }
            catch (Exception ex)
            {
                Log.Error("Error checking for updates", ex);
            }
        }
    }
}
