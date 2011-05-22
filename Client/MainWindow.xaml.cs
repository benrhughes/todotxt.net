using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToDoLib;
using Microsoft.Win32;
using System.IO;

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
            DueDate,
            Priority,
            Project,
            None
        }

        TaskList _taskList;
        SortType _currentSort;
        Task _updating;

        bool _inIntellisense = false;

        public MainWindow()
        {
            InitializeComponent();

            this.Height = User.Default.WindowHeight;
            this.Width = User.Default.WindowWidth;
            this.Left = User.Default.WindowLeft;
            this.Top = User.Default.WindowTop;

            AutoArchiveMenuItem.IsChecked = User.Default.AutoArchive;

            if (!string.IsNullOrEmpty(User.Default.FilePath))
                LoadTasks(User.Default.FilePath);

            FilterAndSort((SortType)User.Default.CurrentSort);
        }

        #region private methods
        private void KeyboardShortcut(Key key)
        {
            switch (key)
            {
                case Key.O:
                    File_Open(null, null);
                    break;
                case Key.N:
                    taskText.Text = User.Default.FilterText;
                    taskText.Focus();
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
                    ToggleComplete((Task)lbTasks.SelectedItem);
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

        private void ToggleComplete(Task task)
        {
            var newTask = new Task(task.Raw);
            newTask.Completed = !newTask.Completed;

            if (User.Default.AutoArchive && newTask.Completed)
            {
                var archiveList = new TaskList(User.Default.ArchiveFilePath);
                archiveList.Add(newTask);
                _taskList.Delete(task);
            }
            else
            {
                _taskList.Update(task, newTask);
            }
        }

        private IEnumerable<Task> Sort(IEnumerable<Task> tasks, SortType sort)
        {
            switch (sort)
            {
                // nb, we sub-sort by completed for most sorts by prepending either a or z
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
                case SortType.DueDate:
                    return tasks.OrderBy(t => (t.Completed ? "z" : "a") + (string.IsNullOrEmpty(t.DueDate) ? "zzz" : t.DueDate));
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

        private void LoadTasks(string filePath)
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
            f.Left = this.Left + 10;
            f.Top = this.Top + 10;
            f.FilterText = User.Default.FilterText;
            if (f.ShowDialog().Value)
            {
                User.Default.FilterText = f.FilterText;
                FilterAndSort(_currentSort);
            }
        }

        private void FilterAndSort(SortType sort)
        {
            List<Task> tasks = new List<Task>();

            if (_taskList != null)
            {
                if (string.IsNullOrEmpty(User.Default.FilterText))
                {
                    tasks = _taskList.Tasks.ToList();
                }
                else
                {
                    foreach (var task in _taskList.Tasks)
                    {
                        bool include = true;
                        foreach (var filter in User.Default.FilterText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!task.Raw.Contains(filter))
                                include = false;
                        }

                        if (include)
                            tasks.Add(task);
                    }
                }
            }

            SetSort(sort, tasks);
        }
        #endregion

        #region UI event handling
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            User.Default.WindowHeight = e.NewSize.Height;
            User.Default.WindowWidth = e.NewSize.Width;
            User.Default.Save();
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            User.Default.WindowLeft = this.Left;
            User.Default.WindowTop = this.Top;
            User.Default.Save();
        }

        #region file menu
        private void File_Open(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";

            var res = dialog.ShowDialog();

            if (res.Value)
                LoadTasks(dialog.FileName);
        }

        private void File_Select_Archive_File(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";

            var res = dialog.ShowDialog();

            if (res.Value)
            {
                User.Default.ArchiveFilePath = dialog.FileName;
                User.Default.Save();
            }
        }

        private void File_Archive_Completed(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(User.Default.ArchiveFilePath))
                File_Select_Archive_File(this, null);

            var archiveList = new TaskList(User.Default.ArchiveFilePath);
            var completed = _taskList.Tasks.Where(t => t.Completed);
            foreach (var task in completed)
            {
                archiveList.Add(task);
                _taskList.Delete(task);
            }

            FilterAndSort(_currentSort);
        }

        private void File_AutoArchive(object sender, RoutedEventArgs e)
        {
            User.Default.AutoArchive = ((MenuItem)sender).IsChecked;
            User.Default.Save();
        }

        #endregion

        #region sort menu
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

        private void Sort_DueDate(object sender, RoutedEventArgs e)
        {
            FilterAndSort(SortType.DueDate);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Project(object sender, RoutedEventArgs e)
        {
            FilterAndSort(SortType.Project);
            SetSelected((MenuItem)sender);
        }
        #endregion

        private void lbTasks_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            KeyboardShortcut(e.Key);
        }

        //this is just for j and k - the nav keys
        private void lbTasks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.J:
                    if (lbTasks.SelectedIndex < lbTasks.Items.Count - 1)
                    {
                        lbTasks.ScrollIntoView(lbTasks.Items[lbTasks.SelectedIndex + 1]);
                        lbTasks.SelectedIndex = lbTasks.SelectedIndex + 1;
                    }
                    break;
                case Key.K:
                    if (lbTasks.SelectedIndex > 0)
                    {
                        lbTasks.ScrollIntoView(lbTasks.Items[lbTasks.SelectedIndex - 1]);
                        lbTasks.SelectedIndex = lbTasks.SelectedIndex - 1;
                    }
                    break;
                default:
                    break;
            }
        }

        private void lbTasks_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            KeyboardShortcut(Key.U);
        }

        private void taskText_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;

            if (_inIntellisense)
            {
                _inIntellisense = false;
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
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
                    break;
                case Key.Escape:
                    _updating = null;
                    tb.Text = "";
                    this.lbTasks.Focus();
                    break;
                case Key.OemPlus:
                    List<string> projects = new List<string>();
                    foreach (var task in _taskList.Tasks)
                        projects = projects.Concat(task.Projects).ToList();

                    var pos = taskText.CaretIndex;
                    ShowIntellisense(projects.Distinct().OrderBy(s => s), taskText.GetRectFromCharacterIndex(pos));
                    break;
                case Key.D2:
                    List<string> contexts = new List<string>();
                    foreach (var task in _taskList.Tasks)
                        contexts = contexts.Concat(task.Contexts).ToList();

                    pos = taskText.CaretIndex;
                    ShowIntellisense(contexts.Distinct().OrderBy(s => s), taskText.GetRectFromCharacterIndex(pos));
                    break;
            }
        }

        private void Intellisense_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Intellisense.IsOpen = false;
                    var i = taskText.CaretIndex -1;
                    taskText.Text = taskText.Text.Remove(i, 1);
                    taskText.Text = taskText.Text.Insert(i, IntellisenseList.SelectedItem.ToString());
                    taskText.CaretIndex = taskText.Text.Length;
                    taskText.Focus();
                    //_inIntellisense = false;
                    break;
            }
        }

        private void ShowIntellisense(IEnumerable<string> s, Rect placement)
        {
            _inIntellisense = true;
            Intellisense.PlacementTarget = taskText;
            Intellisense.PlacementRectangle = placement;

            IntellisenseList.ItemsSource = s;
            Intellisense.IsOpen = true;
            IntellisenseList.Focus();
        }

        #endregion

       

    }
}

