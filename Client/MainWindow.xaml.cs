using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToDoLib;
using Client.Properties;
using Microsoft.Win32;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        enum SortType
        {
            Completed,
            Context,
            Priority,
            Project,
            None
        }

        TaskList _taskList;
        SortType _currentSort;
        Task _updating;
        string _filterText;


        public MainWindow()
        {
            InitializeComponent();

            this.Height = User.Default.WindowHeight;
            this.Width = User.Default.WindowWidth;

            if (!string.IsNullOrEmpty(User.Default.FilePath))
                TryOpen(User.Default.FilePath);

            SetSort((SortType)User.Default.CurrentSort);
        }

        private void KeyboardShortcut(Key key)
        {
            switch (key)
            {
                case Key.O:
                    File_Open(null, null);
                    break;
                case Key.N:
                    taskText.Focus();
                    break;
                case Key.J:
                    lbTasks.SelectedIndex = lbTasks.SelectedIndex < lbTasks.Items.Count ? lbTasks.SelectedIndex + 1 : lbTasks.SelectedIndex;
                    break;
                case Key.K:
                    lbTasks.SelectedIndex = lbTasks.SelectedIndex > 0 ? lbTasks.SelectedIndex - 1 : 0;
                    break;
                case Key.OemQuestion:
                    Help(null, null);
                    break;
                case Key.F:
                    Filter(null, null);
                    break;
                case Key.OemPeriod:
                    _taskList.ReloadTasks();
                    FilterAndSort(_currentSort);
                    break;
                case Key.X:
                    _taskList.ToggleComplete((Task)lbTasks.SelectedItem);
                    FilterAndSort(_currentSort);
                    break;
                case Key.D:
                    var res = MessageBox.Show("Permanently delete the selected task?",
                                 "Confirm Delete",
                                 MessageBoxButton.YesNo);

                    if (res == MessageBoxResult.Yes)
                    {
                        _taskList.Delete((Task)lbTasks.SelectedItem);
                        FilterAndSort(_currentSort);
                    }
                    break;
                case Key.U:
                    _updating = (Task)lbTasks.SelectedItem;
                    taskText.Text = _updating.ToString();
                    taskText.Focus();
                    break;
                default:
                    break;
            }
        }

        private IEnumerable<Task> Sort(IEnumerable<Task> tasks, SortType sort)
        {
            switch (sort)
            {
                // nb, we sub-sort by completed for most sorts by prepending eithe a or z
                case SortType.Completed:
                    return tasks.OrderBy(t => t.Completed);
                case SortType.Context:
                    return tasks.OrderBy(t =>
                        {
                            var s = t.Completed ? "z" : "a";
                            if (t.Contexts != null && t.Contexts.Count > 0)
                                s += t.Contexts.Min().Substring(1);
                            else
                                s += "zzz";
                            return s;
                        });
                case SortType.Priority:
                    return tasks.OrderBy(t => (t.Completed ? "z" : "a") + (string.IsNullOrEmpty(t.Priority) ? "zzz" : t.Priority));
                case SortType.Project:
                    return tasks.OrderBy(t =>
                        {
                            var s = t.Completed ? "z" : "a";
                            if (t.Projects != null && t.Projects.Count > 0)
                                s += t.Projects.Min().Substring(1);
                            else
                                s += "zzz";
                            return s;
                        });
                case SortType.None:
                default:
                    return tasks;
            }
        }


        private void Sort_Priority(object sender, RoutedEventArgs e)
        {
            FilterAndSort(SortType.Priority);
            SetSelected((MenuItem)sender);
        }

        private void Sort_None(object sender, RoutedEventArgs e)
        {
            FilterAndSort(SortType.None);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Context(object sender, RoutedEventArgs e)
        {
            FilterAndSort(SortType.Context);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Completed(object sender, RoutedEventArgs e)
        {
            FilterAndSort(SortType.Completed);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Project(object sender, RoutedEventArgs e)
        {
            FilterAndSort(SortType.Project);
            SetSelected((MenuItem)sender);
        }

        void SetSort(SortType sort, IEnumerable<Task> tasks = null, Task task = null)
        {
            if (tasks == null && _taskList == null)
                return;

            IEnumerable<Task> t = null;
            if (_taskList != null)
                t = _taskList.Tasks;

            if (tasks != null)
                t = tasks;

            User.Default.CurrentSort = (int)sort;
            User.Default.Save();

            _currentSort = sort;

            lbTasks.ItemsSource = Sort(t, _currentSort);

            if (task == null)
                lbTasks.SelectedIndex = 0;
            else
                lbTasks.SelectedItem = task;

            lbTasks.Focus();
        }

        void SetSelected(MenuItem item)
        {
            var sortMenu = (MenuItem)item.Parent;
            foreach (MenuItem i in sortMenu.Items)
                i.IsChecked = false;

            item.IsChecked = true;

        }

        private void taskText_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            if (e.Key == Key.Enter)
            {
                if (_updating == null)
                {
                    _taskList.Add(new Task(tb.Text.Trim()));
                }
                else
                {
                    _taskList.Update(_updating, new Task(tb.Text.Trim()));
                    _updating = null;
                }

                tb.Text = "";
                FilterAndSort(_currentSort);
            }
        }

        private void taskList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            KeyboardShortcut(e.Key);
        }

        private void File_Open(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";

            var res = dialog.ShowDialog();

            if (res.Value)
            {
                TryOpen(dialog.FileName);
            }
        }

        private void TryOpen(string filePath)
        {
            try
            {
                _taskList = new TaskList(filePath);
                User.Default.FilePath = filePath;
                User.Default.Save();
                lbTasks.ItemsSource = Sort(_taskList.Tasks, _currentSort);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occurred while openning " + filePath, MessageBoxButton.OK);
                sortMenu.IsEnabled = false;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            User.Default.WindowHeight = e.NewSize.Height;
            User.Default.WindowWidth = e.NewSize.Width;
            User.Default.Save();
        }

        private void Help(object sender, RoutedEventArgs e)
        {
            var msg =
@"todotxt.net: a Windows UI for todo.txt

Keyboard shortcuts:
	- O: open todo.txt file
	- N: new task
	- J: next task
	- K: prev task
	- X: toggle task completion
	- D: delete task (with confirmation)
	- U: update task
	- F: filter tasks (free-text, one filter condition per line)
	- .: reload tasks from file
	- ?: show help


More info at http://bit.ly/todotxtnet

Copyright 2011 Ben Hughes";
            MessageBox.Show(msg);
        }

        private void Filter(object sender, RoutedEventArgs e)
        {
            var f = new FilterDialog();
            f.FilterText = _filterText;
            if (f.ShowDialog().Value)
            {
                _filterText = f.FilterText;
                FilterAndSort(_currentSort);
            }
        }

        private void FilterAndSort(SortType sort)
        {
            List<Task> tasks = new List<Task>();

            if (string.IsNullOrEmpty(_filterText))
            {
                tasks = _taskList.Tasks.ToList();
            }
            else
            {
                foreach (var task in _taskList.Tasks)
                {
                    bool include = true;
                    foreach (var filter in _filterText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!task.Raw.Contains(filter))
                            include = false;
                    }

                    if (include)
                        tasks.Add(task);
                }
            }
            SetSort(sort, tasks);
        }

    }
}

