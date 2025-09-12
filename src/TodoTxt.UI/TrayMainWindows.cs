using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using ToDoLib;

namespace Client
{
    class TrayMainWindows : IDisposable
    {
        private Window _window;
        private NotifyIcon _notifyIcon;

        public TrayMainWindows(Window window)
        {
            _window = window;
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon();
                _notifyIcon.Text = _window.Title;
                Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/TodoTouch_512.ico")).Stream;
                _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                _notifyIcon.Visible = true;
                _notifyIcon.DoubleClick += DoubleClick;

                _notifyIcon.ContextMenu = new ContextMenu();
                _notifyIcon.ContextMenu.MenuItems.Add("E&xit", new EventHandler(ExitClick));

                _window.Closed += (sender, args) => { this.Dispose(); };
                _window.StateChanged += (sender, args) => { if (_window.WindowState == WindowState.Minimized) _window.Hide(); };
            }
            catch (Exception ex)
            {
                var msg = "Error create tray icon";
                Log.Error(msg, ex);
            }
        }

        private void DoubleClick(object sender, EventArgs args)
        {
            if (_window.WindowState == WindowState.Minimized)
            {
                _window.Show();
                _window.Activate();
                _window.WindowState = WindowState.Normal;
            }
            else
            {
                _window.WindowState = WindowState.Minimized;
                _window.Hide();
            }
        }

        private void ExitClick(object sender, EventArgs args)
        {
            // Close the application by triggering the File-Exit menu item click handler.
            var mainWindow = _window as MainWindow;
            mainWindow?.ExitApplicationExecuted(null, null);

        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
