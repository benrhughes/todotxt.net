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
        enum Sort
        {
            Completed,
            Context,
            Priority,
            Project,
            None
        }

        TaskList _taskList;

        Dictionary<Key, Action> KeyboardShortcuts;

        Dictionary<Sort, Func<IEnumerable<Task>, IEnumerable<Task>>> SortActions;

        Sort CurrentSort;

        Task _updating;

        string _filterText;

        public MainWindow()
        {
            InitializeComponent();

            this.Height = User.Default.WindowHeight;
            this.Width = User.Default.WindowWidth;

            RegisterSortActions();

            RegisterKeyboardShortcuts();

            if (!string.IsNullOrEmpty(User.Default.FilePath))
                TryOpen(User.Default.FilePath);

            SetSort((Sort)User.Default.CurrentSort);
        }

        private void RegisterKeyboardShortcuts()
        {
            KeyboardShortcuts = new Dictionary<Key, Action>()
            {
                {Key.O, () => File_Open(null, null)},
                {Key.N, () => taskText.Focus()},
                {Key.J, () => lbTasks.SelectedIndex = lbTasks.SelectedIndex < lbTasks.Items.Count ? lbTasks.SelectedIndex + 1 : lbTasks.SelectedIndex},
                {Key.K, () => lbTasks.SelectedIndex = lbTasks.SelectedIndex > 0 ? lbTasks.SelectedIndex - 1 : 0},
                {Key.OemQuestion, () => Help(null, null)},
                {Key.F, () => Filter(null, null)},
                {Key.OemPeriod, () => 
                    {
                        _taskList.ReloadTasks();
                        SetSort(CurrentSort);
                    }},
                {Key.X, () => 
                    {
                        _taskList.ToggleComplete((Task)lbTasks.SelectedItem);
                        ApplyFilter();
                    }},
                {Key.D, () => 
                    {
                        var res = MessageBox.Show("Permanently delete the selected task?", 
                                    "Confirm Delete",
                                    MessageBoxButton.YesNo);

                        if (res == MessageBoxResult.Yes)
                        {
                            _taskList.Delete((Task)lbTasks.SelectedItem);
                            ApplyFilter();
                        }
                    }},
                {Key.U, () =>
                    {
                        _updating = (Task)lbTasks.SelectedItem;
                        taskText.Text = _updating.ToString();
                        taskText.Focus();
                    }}
            };
        }

        private void RegisterSortActions()
        {
            // nb, we sub-sort by completed for most sorts by prepending eithe a or z
            SortActions = new Dictionary<Sort, Func<IEnumerable<Task>, IEnumerable<Task>>>()
            {
                {Sort.Completed, x => x.OrderBy(t => t.Completed)} ,
                {Sort.Context, x => x.OrderBy(t => (t.Completed? "z" : "a") + (string.IsNullOrEmpty(t.Context) ? "zzz" : t.Context.Substring(1)))}, //ignore the @
                {Sort.Priority, x => x.OrderBy(t => (t.Completed? "z" : "a") + (t.Priority))},
                {Sort.Project, x => x.OrderBy(t => (t.Completed? "z" : "a") + (string.IsNullOrEmpty(t.Project) ? "zzz" : t.Project.Substring(1)))}, //ignore the +
                {Sort.None, x => x}
            };
        }


        private void Sort_Priority(object sender, RoutedEventArgs e)
        {
            SetSort(Sort.Priority);
            SetSelected((MenuItem)sender);
        }

        private void Sort_None(object sender, RoutedEventArgs e)
        {
            SetSort(Sort.None);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Context(object sender, RoutedEventArgs e)
        {
            SetSort(Sort.Context);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Completed(object sender, RoutedEventArgs e)
        {
            SetSort(Sort.Completed);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Project(object sender, RoutedEventArgs e)
        {
            SetSort(Sort.Project);
            SetSelected((MenuItem)sender);
        }

        void SetSort(Sort sort, IEnumerable<Task> tasks = null, Task task = null)
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

            CurrentSort = sort;

            lbTasks.ItemsSource = SortActions[CurrentSort](t);

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
                ApplyFilter();
            }
        }

        private void taskList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (KeyboardShortcuts.ContainsKey(e.Key))
                KeyboardShortcuts[e.Key]();
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
                lbTasks.ItemsSource = SortActions[CurrentSort](_taskList.Tasks);
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
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            List<Task> tasks = new List<Task>();

            if(string.IsNullOrEmpty(_filterText))
            {
                tasks = _taskList.Tasks.ToList();
            }
            else
            {
                foreach (var task in _taskList.Tasks)
                {
                    bool include = true;
                    foreach (var filter in _filterText.Split(new string[]{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!task.Raw.Contains(filter))
                            include = false;
                    }

                    if (include)
                        tasks.Add(task);
                }
            }
            SetSort(CurrentSort, tasks);
        }

    }
}

