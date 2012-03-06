using System;
using System.Windows;
using System.Windows.Forms;
using ToDoLib;
using System.IO;

using System.Windows.Input;

namespace Client
{
    class ObserverChangeFile
    {
        private string _filename = "";
        private FileSystemWatcher _watcher = new FileSystemWatcher();
        private MainWindow _window;

        public ObserverChangeFile(MainWindow window)
        {
            _window = window;
        }

        public void ViewOnFile(string filename)
        {
            if (User.Default.AutoRefresh == false)
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                    _watcher = null;
                    _filename = "";
                }
                return;
            }

            if (_filename != filename)
            {
                _filename = filename;
                if (_watcher != null)
                {
                    _watcher.Dispose();
                }
                _watcher = new FileSystemWatcher();
                _watcher.Path = System.IO.Path.GetDirectoryName(filename);
                _watcher.Filter = System.IO.Path.GetFileName(filename);
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

                // Add event handlers.
                _watcher.Changed += FileChange;

                // Begin watching.
                _watcher.EnableRaisingEvents = true;
            }
            else if (User.Default.AutoRefresh == false && _watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
            }
        }

        private void FileChange(object source, FileSystemEventArgs e)
        {
            _window.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        _window.Refresh();
                    }));

        }
    }
}
