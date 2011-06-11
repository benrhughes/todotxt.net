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
            Alphabetical,
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
        int _intelliPos;
        bool _autoRefresh;
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
 
        public MainWindow()
        {
            InitializeComponent();

            this.Height = User.Default.WindowHeight;
            this.Width = User.Default.WindowWidth;
            this.Left = User.Default.WindowLeft;
            this.Top = User.Default.WindowTop;

            AutoArchiveMenuItem.IsChecked = User.Default.AutoArchive;
            AutoRefreshMenuItem.IsChecked = User.Default.AutoRefresh;
            _autoRefresh = User.Default.AutoRefresh;

            if (!string.IsNullOrEmpty(User.Default.FilePath))
                LoadTasks(User.Default.FilePath);

            FilterAndSort((SortType)User.Default.CurrentSort);

            TimerCheck();
        }

        #region private methods
        private void KeyboardShortcut(Key key)
        {
            switch (key)
            {
                case Key.C:
                    File_New(null, null);
                    break;
                case Key.O:
                    File_Open(null, null);
                    break;
                case Key.N:
                    // create one-line string of all filter but not ones beginning with a minus, and use as the starting text for a new task
                    string _filters = "";
                    foreach (var filter in User.Default.FilterText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (filter.Substring(0, 1) != "-")
                        {
                            _filters = _filters + " " + filter;
                        }
                    }
                    taskText.Text = _filters;
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
                case SortType.Alphabetical:
                    return tasks.OrderBy(t => (t.Completed ? "z" : "a") + t.Raw);
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
	- C: new todo.txt file
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
                            if (filter.Substring(0, 1) != "-")
                            {   // if the filter does not start with a minus and filter is contained in task then filter out
                                if (!task.Raw.Contains(filter))
                                    include = false;
                            }
                            else
                            {   // if the filter starts with a minus then (ignoring the minus) check if the filter is contained in the task then filter out if so
                                if (task.Raw.Contains(filter.Substring(1)))
                                {
                                    include = false;
                                }
                            }
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

        #region windows
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

        #endregion

        #region file menu

        private void File_New(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.FileName = "todo.txt";
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            var res = dialog.ShowDialog();
            if (res.Value)
                SaveFileDialog(dialog.FileName);

            if (File.Exists(dialog.FileName))
                LoadTasks(dialog.FileName);
                
        }

        private static void SaveFileDialog(string filename)
        {
            using (StreamWriter todofile = new StreamWriter(filename))
            {
                todofile.Write("");
            }
            
            
        }


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

        private void File_AutoRefresh(object sender, RoutedEventArgs e)
        {
            User.Default.AutoRefresh = ((MenuItem)sender).IsChecked;
            User.Default.Save();
            TimerCheck();
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

        private void Sort_Alphabetical(object sender, RoutedEventArgs e)
        {
            FilterAndSort(SortType.Alphabetical);
            SetSelected((MenuItem)sender);
        }
        #endregion

        #region lbTasks
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

        #endregion

        #region taskText

        private void taskText_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_taskList == null)
                {
                    MessageBox.Show("You don't have a todo.txt file open - please use File\\New or File\\Open",
                        "Please open a file", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                    lbTasks.Focus();
                    return;
                }

                if (_updating == null)
                {
                    try
                    {
                        _taskList.Add(new Task(taskText.Text.Trim()));
                    }
                    catch (TaskException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else
                {
                    _taskList.Update(_updating, new Task(taskText.Text.Trim()));
                    _updating = null;
                }

                taskText.Text = "";
                FilterAndSort(_currentSort);

                Intellisense.IsOpen = false;
                return;
            }

            if (Intellisense.IsOpen && !IntellisenseList.IsFocused)
            {
                switch (e.Key)
                {
                    case Key.Down:
                        IntellisenseList.Focus();
                        Keyboard.Focus(IntellisenseList);
                        IntellisenseList.SelectedIndex = 0;
                        break;
                    case Key.Escape:
                    case Key.Space:
                        Intellisense.IsOpen = false;
                        break;
                    default:
                        var word = FindIntelliWord();
                        IntellisenseList.Items.Filter = (o) => o.ToString().Contains(word);

                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        if (_updating == null)
                        {
                            _taskList.Add(new Task(taskText.Text.Trim()));
                        }
                        else
                        {
                            _taskList.Update(_updating, new Task(taskText.Text.Trim()));
                            _updating = null;
                        }

                        taskText.Text = "";
                        FilterAndSort(_currentSort);
                        break;
                    case Key.Escape:
                        _updating = null;
                        taskText.Text = "";
                        this.lbTasks.Focus();
                        break;
                    case Key.OemPlus:
                        List<string> projects = new List<string>();
                        foreach (var task in _taskList.Tasks)
                            projects = projects.Concat(task.Projects).ToList();

                        _intelliPos = taskText.CaretIndex-1;
                        ShowIntellisense(projects.Distinct().OrderBy(s => s), taskText.GetRectFromCharacterIndex(_intelliPos));
                        break;
                    case Key.D2:
                        List<string> contexts = new List<string>();
                        foreach (var task in _taskList.Tasks)
                            contexts = contexts.Concat(task.Contexts).ToList();

                        _intelliPos = taskText.CaretIndex-1;
                        ShowIntellisense(contexts.Distinct().OrderBy(s => s), taskText.GetRectFromCharacterIndex(_intelliPos));
                        break;
                }
            }
        }


        private string FindIntelliWord()
        {
            return taskText.Text.Substring(_intelliPos + 1, taskText.CaretIndex - _intelliPos - 1);
        }

        #endregion

        #region intellisense
        private void Intellisense_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Intellisense.IsOpen = false;

                    taskText.Text = taskText.Text.Remove(_intelliPos, taskText.CaretIndex - _intelliPos);

                    var newText = IntellisenseList.SelectedItem.ToString();
                    taskText.Text = taskText.Text.Insert(_intelliPos, newText);
                    taskText.CaretIndex = _intelliPos + newText.Length;

                    taskText.Focus();
                    break;
                case Key.Escape:
                    Intellisense.IsOpen = false;
                    taskText.CaretIndex = taskText.Text.Length;
                    taskText.Focus();
                    break;
            }

            e.Handled = true;
        }

        private void ShowIntellisense(IEnumerable<string> s, Rect placement)
        {
            if (s.Count() == 0)
                return;

            Intellisense.PlacementTarget = taskText;
            Intellisense.PlacementRectangle = placement;

            IntellisenseList.ItemsSource = s;
            Intellisense.IsOpen = true;
            taskText.Focus();
        }
        #endregion

        #endregion

        #region timer
        private void TimerCheck()
        {
            if (_autoRefresh == true)
            {
                dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 20);
                dispatcherTimer.Start();
            }
            else if (_autoRefresh == false && dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            _taskList.ReloadTasks();
            FilterAndSort(_currentSort);
        }
        #endregion
    }
}

