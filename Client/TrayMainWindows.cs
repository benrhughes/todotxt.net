using System;
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
                _notifyIcon.Icon = new System.Drawing.Icon("TodoTouch_512.ico");
                _notifyIcon.Visible = true;
                _notifyIcon.DoubleClick += DoubleClick;

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
				//_window.Topmost = true;
                _window.Activate();
                _window.WindowState = WindowState.Normal;
            }
            else
            {
                _window.WindowState = WindowState.Minimized;
                _window.Hide();
            }
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
