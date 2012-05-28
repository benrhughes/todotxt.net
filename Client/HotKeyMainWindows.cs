using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ToDoLib;

namespace Client
{
    class HotKeyMainWindows: IDisposable
    {
        private Window _window;
        private HotKey _hotkey;

        public HotKeyMainWindows(Window window, ModifierKeys modifierKeys, Keys key)
        {
            try
            {
                _window = window;
                _hotkey = new HotKey(modifierKeys, key, _window);
                _hotkey.HotKeyPressed += KeyPressed;
                _window.Closed += (sender, args) => { Dispose(); };
            }
            catch (Exception ex)
            {
                var msg = "Error Global HotKey Registered";
                Log.Error(msg, ex);
                System.Windows.MessageBox.Show(ex.Message, msg, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KeyPressed(HotKey k)
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

        public void Dispose()
        {
            _hotkey.Dispose();
            _hotkey = null;
        }
    }
}

