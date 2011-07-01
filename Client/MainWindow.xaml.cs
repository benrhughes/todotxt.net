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
using System.Reflection;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;

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
        DispatcherTimer _dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();

            webBrowser1.Navigate("about:blank");

            // migrate the user settings from the previous version, if necessary
            if (User.Default.FirstRun)
            {
                User.Default.Upgrade();
                User.Default.FirstRun = false;
                User.Default.Save();
            }

            this.Height = User.Default.WindowHeight;
            this.Width = User.Default.WindowWidth;
            this.Left = User.Default.WindowLeft;
            this.Top = User.Default.WindowTop;

            if (!string.IsNullOrEmpty(User.Default.FilePath))
                LoadTasks(User.Default.FilePath);

            FilterAndSort((SortType)User.Default.CurrentSort);

            TimerCheck();

            ThreadPool.QueueUserWorkItem(x => CheckForUpdates());
        }


        #region private methods
        private void KeyboardShortcut(Key key)
        {
            // if there's no task list open, we only want to allow open and create shortcuts
            switch (key)
            {
                case Key.C:
                    File_New(null, null);
                    return;
                case Key.O:
                    File_Open(null, null);
                    return;
            }

            if (_taskList == null)
                return;

            switch (key)
            {
                case Key.N:
                    // create one-line string of all filter but not ones beginning with a minus, and use as the starting text for a new task
                    string filters = "";
                    foreach (var filter in User.Default.FilterText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (filter.Substring(0, 1) != "-")
                        {
                            filters = filters + " " + filter;
                        }
                    }
                    taskText.Text = filters;
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
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var msg =
@"todotxt.net: a Windows UI for todo.txt

Version " + version + @"

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
                    var comparer = User.Default.FilterCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
                    foreach (var task in _taskList.Tasks)
                    {
                        bool include = true;
                        foreach (var filter in User.Default.FilterText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (filter.Substring(0, 1) != "-")
                            {   // if the filter does not start with a minus and filter is contained in task then filter out
                                if (!task.Raw.Contains(filter, comparer))
                                    include = false;
                            }
                            else
                            {   // if the filter starts with a minus then (ignoring the minus) check if the filter is contained in the task then filter out if so
                                if (task.Raw.Contains(filter.Substring(1), comparer))
                                    include = false;
                            }
                        }

                        if (include)
                            tasks.Add(task);
                    }
                }
            }

            SetSort(sort, tasks);
        }

        private void CheckForUpdates()
        {
            const string updateXMLUrl = @"https://raw.github.com/benrhughes/todotxt.net/master/Updates.xml";

            var xDoc = new XmlDocument();

            try
            {
                xDoc.Load(new XmlTextReader(updateXMLUrl));

                var version = xDoc.SelectSingleNode("//version").InnerText;
                var changelog = xDoc.SelectSingleNode("//changelog").InnerText;

                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (version != assemblyVersion)
                {
                    Dispatcher.Invoke(new Action<string>(ShowUpdateMenu), version);
                }
            }
            catch (Exception)
            {
                // swallowing excepitons is bad, mmmk? But it really doesn't matter if the auto-update fails, so lets make an exception...
            }

        }

        private void ShowUpdateMenu(string version)
        {
            this.UpdateMenu.Header = "New version: " + version;
            this.UpdateMenu.Visibility = Visibility.Visible;
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

        private void File_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void File_Archive_Completed(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(User.Default.ArchiveFilePath))
                File_Options(this, null);

            if (!File.Exists(User.Default.ArchiveFilePath))
                return;

            var archiveList = new TaskList(User.Default.ArchiveFilePath);
            var completed = _taskList.Tasks.Where(t => t.Completed);
            foreach (var task in completed)
            {
                archiveList.Add(task);
                _taskList.Delete(task);
            }

            FilterAndSort(_currentSort);
        }

        private void File_Options(object sender, RoutedEventArgs e)
        {
            var o = new Options();

            var res = o.ShowDialog();

            if (res.Value)
            {
                User.Default.ArchiveFilePath = o.tbArchiveFile.Text;
                User.Default.AutoArchive = o.cbAutoArchive.IsChecked.Value;
                User.Default.AutoRefresh = o.cbAutoRefresh.IsChecked.Value;
                User.Default.FilterCaseSensitive = o.cbCaseSensitiveFilter.IsChecked.Value;

                User.Default.Save();

                TimerCheck();

                FilterAndSort(_currentSort);
            }
        }

        #region printing

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
            doc.execCommand("Print", true, 0);
            doc.close();

            Set_PrintControlsVisibility(false);
        }

        private void btnCancelPrint_Click(object sender, RoutedEventArgs e)
        {
            Set_PrintControlsVisibility(false);
        }

        private void Set_PrintControlsVisibility(bool PrintControlsVisibility)
        {
            if (PrintControlsVisibility)
            {   // Show Printer Controls
                webBrowser1.Visibility = Visibility.Visible;
                btnPrint.Visibility = Visibility.Visible;
                btnCancelPrint.Visibility = Visibility.Visible;
                lbTasks.Visibility = Visibility.Hidden;
                menu1.Visibility = Visibility.Hidden;
                taskText.Visibility = Visibility.Hidden;
            }
            else
            {   // Hide Printer Controls
                webBrowser1.Visibility = Visibility.Hidden;
                btnPrint.Visibility = Visibility.Hidden;
                btnCancelPrint.Visibility = Visibility.Hidden;
                lbTasks.Visibility = Visibility.Visible;
                menu1.Visibility = Visibility.Visible;
                taskText.Visibility = Visibility.Visible;
            }
        }

        private string Get_PrintContents(object sender, RoutedEventArgs e)
        {
            string printContents = "";
            string taskdetail = "";
            string printstyle = "";

            // CSS Presentation
            printstyle += "body, table, tr, th, td {font-family:Cambria,Courier New;font-size:11px;} ";
            printstyle += "td.priority{font-weight:bold;} ";
            printstyle += "td.complete{font-weight:bold;} ";
            printstyle += "tr.tbhead td {font-weight:bold;} ";
            printstyle += "span.project{color:red;} ";
            printstyle += "span.context{color:blue;} ";
            printstyle += "span.isdate{font-style:italic;color:#FF6600;font-weight:bold;} ";
            printstyle += "td.startdate{font-style:italic;color:red;} ";
            printstyle += "td.completeddate{font-style:italic;color:green;} ";

            // CSS Layout
            printstyle += "table, tr {width:7in;} ";
            printstyle += "td {vertical-align:top;} ";
            printstyle += "td.priority{text-align:center;width:20px;} ";
            printstyle += "td.startdate{width:60px;} ";
            printstyle += "td.completeddate{width:60px;} ";

            printContents = "<html><head><title>todotxt.net</title><style>" + printstyle + "</style></head><body><h2>todotxt.net</h2><table>";
            printContents += "<tr class='tbhead'><th>&nbsp;</th><th>Done</th><th>Created</th><td>Details</td></tr>";
            foreach (var task in lbTasks.Items)
            {
                // Determine which scenario the task fits
                //   Scenario 1 - Completed task (1st word is "x", 2nd word is Date_Completed, 3rd word starts Task_Details
                //   Scenario 2 - Uncompleted task with Priority with Creation_Date
                //   Scenario 3 - Uncompleted task with Priority without Creation_Date
                //   Scenario 4 - Uncompleted task without Priority with Creation_Date
                //   Scenario 5 - Uncompleted task without Priority without Creation_Date
                int scenario = 0;

                taskdetail = task.ToString().Trim();
                string[] split = taskdetail.Split(new char[] { ' ' });

                scenario = getTaskScenario(split);

                if (scenario == 1)
                {
                    printContents += "<tr class='completedTask'>";
                }
                else
                {
                    printContents += "<tr class='uncompletedTask'>";
                }


                switch(scenario)
                {
                    case 1:
                        printContents += "<td class='complete'>" + split[0] + "</td> ";
                        printContents += "<td class='completeddate'>" + split[1] + "</td> ";
                        
                        for (int i = 2; i < split.Count(); i++)
                        {
                            if (isDateField(split[i]) && i==2)
                            {
                                printContents += "<td class='startdate'>" + split[i] + "</td> <td>";
                            }
                            else
                            {
                                if (i == 2)
                                {
                                    printContents += "<td>&nbsp;</td><td>";
                                }
                                if (isProjectField(split[i]))
                                {
                                    printContents += "<span class='project'>" + split[i] + "</span> ";
                                }
                                else
                                {
                                    if (isContextField(split[i]))
                                    {
                                        printContents += "<span class='context'>" + split[i] + "</span> ";
                                    }
                                    else
                                    {
                                        if (isDateField(split[i]))
                                        {
                                            printContents += "<span class='isdate'>" + split[i] + "</span> ";
                                        }
                                        else
                                        {
                                            printContents += split[i] + " ";
                                        }
                                    }
                                }
                            }
                        }
                        printContents = printContents.Trim();
                        printContents += "</td></tr>";
                        break;

                    case 2:
                        printContents += "<td class='priority'>" + split[0] + "</td> <td>&nbsp;</td> ";
                        printContents += "<td class='startdate'>" + split[1] + "</td> ";
                        printContents += "<td>";

                        for (int i = 2; i < split.Count(); i++)
                        {
                            if (isProjectField(split[i]))
                            {
                                printContents += "<span class='project'>" + split[i] + "</span> ";
                            }
                            else
                            {
                                if (isContextField(split[i]))
                                {
                                    printContents += "<span class='context'>" + split[i] + "</span> ";
                                }
                                else
                                {
                                    if (isDateField(split[i]))
                                    {
                                        printContents += "<span class='isdate'>" + split[i] + "</span> ";
                                    }
                                    else
                                    {
                                        printContents += split[i] + " ";
                                    }
                                }
                            }
                        }
                        printContents = printContents.Trim();
                        printContents += "</td></tr>";
                        break;

                    case 3:
                        printContents += "<td class='priority'>" + split[0] + "</td><td>&nbsp;</td><td>&nbsp;</td> ";
                        printContents += "<td>";

                        for (int i = 1; i < split.Count(); i++)
                        {
                            if (isProjectField(split[i]))
                            {
                                printContents += "<span class='project'>" + split[i] + "</span> ";
                            }
                            else
                            {
                                if (isContextField(split[i]))
                                {
                                    printContents += "<span class='context'>" + split[i] + "</span> ";
                                }
                                else
                                {
                                    if (isDateField(split[i]))
                                    {
                                        printContents += "<span class='isdate'>" + split[i] + "</span> ";
                                    }
                                    else
                                    {
                                        printContents += split[i] + " ";
                                    }
                                }
                            }
                        }
                        printContents = printContents.Trim();
                        printContents += "</td></tr>";
                        break;

                    case 4:
                        printContents += "<td>&nbsp;</td><td>&nbsp;</td><td class='startdate'>" + split[0] + "</td> ";
                        printContents += "<td>";

                        for (int i = 1; i < split.Count(); i++)
                        {
                            if (isProjectField(split[i]))
                            {
                                printContents += "<span class='project'>" + split[i] + "</span> ";
                            }
                            else
                            {
                                if (isContextField(split[i]))
                                {
                                    printContents += "<span class='context'>" + split[i] + "</span> ";
                                }
                                else
                                {
                                    if (isDateField(split[i]))
                                    {
                                        printContents += "<span class='isdate'>" + split[i] + "</span> ";
                                    }
                                    else
                                    {
                                        printContents += split[i] + " ";
                                    }
                                }
                            }
                        }
                        printContents = printContents.Trim();
                        printContents += "</td></tr>";
                        break;

                    case 5:
                        printContents += "<td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>";

                        for (int i = 0; i < split.Count(); i++)
                        {
                            if (isProjectField(split[i]))
                            {
                                printContents += "<span class='project'>" + split[i] + "</span> ";
                            }
                            else
                            {
                                if (isContextField(split[i]))
                                {
                                    printContents += "<span class='context'>" + split[i] + "</span> ";
                                }
                                else
                                {
                                    if (isDateField(split[i]))
                                    {
                                        printContents += "<span class='isdate'>" + split[i] + "</span> ";
                                    }
                                    else
                                    {
                                        printContents += split[i] + " ";
                                    }
                                }
                            }
                        }
                        printContents = printContents.Trim();
                        printContents += "</td></tr>";
                        break;

                    default:

                        // we should not hit this section

                        break;
                }
            }
            printContents += "</table></body></html>";

            return printContents;
        }

        private int getTaskScenario(string[] split)
        {
            int scenario = 0;
            if (isCompletedField(split[0]))  // completed
            {
                scenario = 1;
            }
            if (!isCompletedField(split[0]))  // not completed
            {
                if (isPriorityField(split[0]))  // with priority
                {
                    if (isDateField(split[1]))  // with creation date
                    {
                        scenario = 2;
                    }
                    else  // without creation date
                    {
                        scenario = 3;
                    }
                }
                else   // without priority
                {
                    if (isDateField(split[0]))  // with creation date
                    {
                        scenario = 4;
                    }
                    else  // without creation date
                    {
                        scenario = 5;
                    }
                }
            }
            return scenario;
        }

        private bool isPriorityField(string s)
        {
            if (s.Length > 0)
            {
                if (s.Length == 3 && s.Substring(0, 1) == "(" && s.Substring(2, 1) == ")")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool isDateField(string s)
        {
            return IsDate(s);
        }

        private bool isProjectField(string s)
        {
            if (s.Length > 0)
            {
                if (s.Substring(0, 1) == "+")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool isContextField(string s)
        {
            if (s.Length > 0)
            {
                if (s.Substring(0, 1) == "@")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool isCompletedField(string s)
        {
            if (s.Length > 0)
            {
                if (s.Length == 1 && s.Substring(0, 1) == "x")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void File_PrintPreview(object sender, RoutedEventArgs e)
        {
            string printContents;
            printContents = Get_PrintContents(sender, e);

            mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
            doc.clear();
            doc.write(printContents);
            doc.close();

            Set_PrintControlsVisibility(true);
        }

        private void File_Print(object sender, RoutedEventArgs e)
        {
            string printContents;
            printContents = Get_PrintContents(sender, e);

            mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
            doc.clear();
            doc.write(printContents);
            doc.execCommand("Print", true, 0);
            doc.close();
        }

        public bool IsDate(string sdate)
        {
            DateTime dt;
            bool isDate = true;
            try
            {
                dt = DateTime.Parse(sdate);
            }
            catch
            {
                isDate = false;
            }
            return isDate;
        }

        #endregion  //printing

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

        #region Update notification
        private void Get_Update(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/benrhughes/todotxt.net/downloads");
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
            if (_taskList == null)
            {
                MessageBox.Show("You don't have a todo.txt file open - please use File\\New or File\\Open",
                    "Please open a file", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
                lbTasks.Focus();
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (_updating == null)
                {
                    try
                    {
                        // determine if task entered has a creation date, if not lets add one.
                        // also need to know if there is a priority because we'll need to add the creation date after it if there is one.
                        string taskdetail = taskText.Text.Trim();
                        string[] split = taskdetail.Split(new char[] { ' ' });
                        int scenario = 0;
                        scenario = getTaskScenario(split);
                        DateTime thisDay = DateTime.Today;

                        switch (scenario)
                        {
                            //   Scenario 1 - Completed task (1st word is "x", 2nd word is Date_Completed, 3rd word starts Task_Details
                            //   Scenario 2 - Uncompleted task with Priority with Creation_Date
                            //   Scenario 3 - Uncompleted task with Priority without Creation_Date
                            //   Scenario 4 - Uncompleted task without Priority with Creation_Date
                            //   Scenario 5 - Uncompleted task without Priority without Creation_Date

                            case 0: // couldn't determine scenario
                            case 1: // completed
                            case 2: // has creation date
                            case 4: // has creation date
                                break;  // lets just leave the task alone

                            case 3: // add in the creation date
                                split[1] = thisDay.ToString("yyyy-MM-dd") + " " + split[1]; // squeeze in the date
                                taskdetail = "";
                                for (int i = 0; i < split.Count(); i++)  // rebuild the task
                                {
                                    taskdetail += split[i] + " ";
                                }
                                break;

                            case 5: // add in the creation date
                                split[0] = thisDay.ToString("yyyy-MM-dd") + " " + split[0]; // squeeze in the date
                                taskdetail = "";
                                for (int i = 0; i < split.Count(); i++)  // rebuild the task
                                {
                                    taskdetail += split[i] + " ";
                                }
                                break;

                            default:
                                break;  // anything else just leave the task alone

                        }
                        _taskList.Add(new Task(taskdetail.Trim()));
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
                if (taskText.CaretIndex <= _intelliPos) // we've moved behind the symbol, drop out of intellisense
                {
                    Intellisense.IsOpen = false;
                    return;
                }

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
                    case Key.Escape:
                        _updating = null;
                        taskText.Text = "";
                        this.lbTasks.Focus();
                        break;
                    case Key.OemPlus:
                        List<string> projects = new List<string>();
                        _taskList.Tasks.Each(task => projects = projects.Concat(task.Projects).ToList());

                        _intelliPos = taskText.CaretIndex - 1;
                        ShowIntellisense(projects.Distinct().OrderBy(s => s), taskText.GetRectFromCharacterIndex(_intelliPos));
                        break;
                    case Key.D2:
                        List<string> contexts = new List<string>();
                        _taskList.Tasks.Each(task => contexts = contexts.Concat(task.Contexts).ToList());

                        _intelliPos = taskText.CaretIndex - 1;
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
            if (User.Default.AutoRefresh == true)
            {
                _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                _dispatcherTimer.Interval = new TimeSpan(0, 0, 20);
                _dispatcherTimer.Start();
            }
            else if (User.Default.AutoRefresh == false && _dispatcherTimer != null)
            {
                _dispatcherTimer.Stop();
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

