using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using ToDoLib;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.IO;
using ColorFont;
using CommonExtensions;
using Microsoft.Win32;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;

namespace Client
{
    public class MainWindowViewModel
    {
        #region Properties

        private CollectionView _myView;
        private TaskList _taskList;
        private FileChangeObserver _changefile;
        private SortType _sortType;
        private MainWindow _window;
        private Task _updating;
        int _intelliPos;
        private int _numberOfItemsInCurrentGroup;
        private List<CollectionViewGroup> _viewGroups;
        private int _nextGroupAtTaskNumber;

        public SortType SortType
        {
            get { return _sortType; }
            set
            {
                if (_sortType != value)
                {
                    User.Default.CurrentSort = (int)value;
                    User.Default.Save();
                }

                _sortType = value;
            }
        }

        public Help HelpPage { get; private set; }

        #endregion

        #region Constructors

        public MainWindowViewModel(MainWindow window)
        {
            _window = window;

            Log.LogLevel = User.Default.DebugLoggingOn ? LogLevel.Debug : LogLevel.Error;

            _changefile = new FileChangeObserver();
            _changefile.OnFileChanged += () => _window.Dispatcher.BeginInvoke(new Action(ReloadFile));

            SortType = (SortType)User.Default.CurrentSort;

            if (!string.IsNullOrEmpty(User.Default.FilePath))
                LoadTasks(User.Default.FilePath);
        }

        #endregion

        #region Task List ListBox Management Methods

        public void LoadTasks(string filePath)
        {
            try
            {
                _taskList = new TaskList(filePath);
                User.Default.FilePath = filePath;
                User.Default.Save();
                _changefile.ObserveFile(User.Default.FilePath);
				UpdateDisplayedTasks();
            }
            catch (Exception ex)
            {
                ex.Handle("An error occurred while opening " + filePath);
            }
        }

        public void ReloadFile()
        {
            try
            {
                _taskList.ReloadTasks();
            }
            catch (Exception ex)
            {
                ex.Handle("Error loading tasks");
            }
            UpdateDisplayedTasks();
        }

        public void UpdateDisplayedTasks()
        {
            if (_taskList == null)
                return;

            var selected = _window.lbTasks.SelectedItem as Task;
            var selectedIndex = _window.lbTasks.SelectedIndex;
            string sortProperty = "";

            try
            {
                var sortedTaskList = FilterList(_taskList.Tasks);
                sortedTaskList = SortList(sortedTaskList);

                switch (SortType)
                {
                    case SortType.Project:
                        sortProperty = "Projects";
                        break;
                    case SortType.Context:
                        sortProperty = "Contexts";
                        break;
                    case SortType.DueDate:
                        sortProperty = "DueDate";
                        break;
                    case SortType.Completed:
                        sortProperty = "CompletedDate";
                        break;
                    case SortType.Priority:
                        sortProperty = "Priority";
                        break;
                    case SortType.Created:
                        sortProperty = "CreationDate";
                        break;
                }

                _myView = (CollectionView)CollectionViewSource.GetDefaultView(sortedTaskList);

                if (User.Default.AllowGrouping && SortType != SortType.Alphabetical && SortType != SortType.None)
                {
                    if (_myView.CanGroup)
                    {
                        var groupDescription = new PropertyGroupDescription(sortProperty);
                        groupDescription.Converter = new GroupConverter();

                        _myView.GroupDescriptions.Add(groupDescription);
                    }
                }
                else
                {
                    _myView.GroupDescriptions.Clear();
                }
                _window.lbTasks.ItemsSource = sortedTaskList;
            }
            catch (Exception ex)
            {
                ex.Handle("Error while sorting tasks");
            }

            if (selected == null)
            {
                _window.lbTasks.SelectedIndex = 0;
            }
            else
            {
                var match = _taskList.Tasks.FirstOrDefault(x => x.Body.Equals(selected.Body, StringComparison.InvariantCultureIgnoreCase));
                if (match == null)
                {
                    if (selectedIndex == _window.lbTasks.Items.Count)
                    {
                        _window.lbTasks.SelectedIndex = selectedIndex - 1;
                    }
                    else
                    {
                        _window.lbTasks.SelectedIndex = selectedIndex;
                    }
                }
                else
                {
                    _window.lbTasks.SelectedItem = match;
                    _window.lbTasks.ScrollIntoView(match);
                }
            }

            //Set the menu item to Bold to easily identify if there is a filter in force
            _window.filterMenu.FontWeight = User.Default.FilterText.Length == 0 ? FontWeights.Normal : FontWeights.Bold;
            _window.lbTasks.Focus();
        }

        private bool IsTaskSelected()
        {
            return _window.lbTasks.SelectedItem != null;
        }

        /// <summary>
        /// Helper function to determine if the correct keysequence has been entered to create a task.
        /// Added to enable the check for Ctrl-Enter if set in options.
        /// </summary>
        /// <param name="e">The stroked key and any modifiers.</param>
        /// <returns>true if the task should be added to the list, false otherwise.</returns>
        private bool ShouldAddTask(KeyEventArgs e)
        {
            const Key NewTaskKey = Key.Enter;

            if (e.Key == NewTaskKey)
            {
                if (User.Default.RequireCtrlEnter)
                {
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                        return true;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Task List ListBox Event Handling Methods

        public void TaskListKeyUp(Key key, ModifierKeys modifierKeys = ModifierKeys.None)
        {
        	if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && key == Key.C)
		    {
                CopySelectedTaskToTextBox();
		        return;
		    }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && key == Key.C)
            {
                CopySelectedTasksToClipboard();
                return;
            }

            // create and open can be used when there's no list loaded
            switch (key)
            {
                case Key.C:
                    NewFile();
                    return;
                case Key.O:
                    OpenFile();
                    return;
            }

            if (_taskList == null)
                return;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                return;
            }

            switch (key)
            {
                case Key.N:
                    AddNewTask();
                    return;
                case Key.OemQuestion:
                    ShowHelpDialog();
                    return;
                case Key.F:
                    ShowFilterDialog();
                    return;
                case Key.RightShift:
                    AddCalendarToTitle();
                    return;
                // Filter Presets
                case Key.NumPad0:
                case Key.D0:
                    ApplyFilterPreset0();
                    return;
                case Key.NumPad1:
                case Key.D1:
                    ApplyFilterPreset1();
                    return;
                case Key.NumPad2:
                case Key.D2:
                    ApplyFilterPreset2();
                    return;
                case Key.NumPad3:
                case Key.D3:
                    ApplyFilterPreset3();
                    return;
                case Key.X:
                    ToggleCompletion();
                    return;
                case Key.D:
                    DeleteTask();
                    return;
                case Key.U:
                case Key.F2:
                    UpdateTask();
                    return;
                case Key.P:
                    PostponeTask();
                    return;
                default:
                    return;
            }
        }

        public void TaskListPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) && _window.lbTasks.HasItems)
            {
                if (!IsTaskSelected()) return;

                switch (e.SystemKey)
                {
                    case Key.Up:
                        IncreasePriority();
                        break;

                    case Key.Down:
                        DecreasePriority();
                        break;
                    case Key.Left:
                    case Key.Right:
                        RemovePriority();
                        break;
                }
                _window.lbTasks.Focus();
                return;
            }

            switch (e.Key)
            {
                case Key.J:
                case Key.Down:
                    if (_window.lbTasks.SelectedIndex < _window.lbTasks.Items.Count - 1)
                    {
                        _window.lbTasks.SelectedIndex++;
                        _window.lbTasks.ScrollIntoView(_window.lbTasks.Items[_window.lbTasks.SelectedIndex]);
                    }
                    e.Handled = true;
                    break;
                case Key.K:
                case Key.Up:
                    if (_window.lbTasks.SelectedIndex > 0)
                    {
                        _window.lbTasks.ScrollIntoView(_window.lbTasks.Items[_window.lbTasks.SelectedIndex - 1]);
                        _window.lbTasks.SelectedIndex = _window.lbTasks.SelectedIndex - 1;
                    }
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        public void TaskListTextInput(TextCompositionEventArgs e)
        {
            var text = e.Text.ToLowerInvariant();

            switch (text)
            {
                case "?":
                    ShowHelpDialog();
                    return;
                case ".":
                    ReloadFile();
                    return;
                default:
                    return;
            }
        }

        #endregion

        #region Filter Methods

        public void ShowFilterDialog()
        {
            var f = new FilterDialog();
            f.Owner = _window;

            f.FilterText = User.Default.FilterText;
            f.FilterTextPreset1 = User.Default.FilterTextPreset1;
            f.FilterTextPreset2 = User.Default.FilterTextPreset2;
            f.FilterTextPreset3 = User.Default.FilterTextPreset3;

            if (f.ShowDialog().Value)
            {
                User.Default.FilterText = f.FilterText.Trim();
                User.Default.FilterTextPreset1 = f.FilterTextPreset1.Trim();
                User.Default.FilterTextPreset2 = f.FilterTextPreset2.Trim();
                User.Default.FilterTextPreset3 = f.FilterTextPreset3.Trim();
                User.Default.Save();

				UpdateDisplayedTasks();
            }
        }

        public static IEnumerable<Task> FilterList(IEnumerable<Task> tasks)
        {
            var filters = User.Default.FilterText;
            var comparer = User.Default.FilterCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

            if (String.IsNullOrEmpty(filters))
                return tasks;

            var filteredTasks = new List<Task>();

            foreach (var task in tasks)
            {
                bool include = true;
                foreach (var filter in filters.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (filter.Equals("due:today", StringComparison.OrdinalIgnoreCase)
                        && task.DueDate == DateTime.Now.ToString("yyyy-MM-dd"))
                        continue;
                    else if (filter.Equals("due:future", StringComparison.OrdinalIgnoreCase)
                        && task.DueDate.IsDateGreaterThan(DateTime.Now))
                        continue;
                    else if (filter.Equals("due:past", StringComparison.OrdinalIgnoreCase)
                        && task.DueDate.IsDateLessThan(DateTime.Now))
                        continue;
                    else if (filter.Equals("due:active", StringComparison.OrdinalIgnoreCase)
                        && !task.DueDate.IsNullOrEmpty()
                        && !task.DueDate.IsDateGreaterThan(DateTime.Now))
                        continue;

                    if (filter.Substring(0, 1) == "-")
                    {
                        if (task.Raw.Contains(filter.Substring(1), comparer))
                            include = false;
                    }
                    else if (!task.Raw.Contains(filter, comparer))
                    {
                        include = false;
                    }
                }

                if (include)
                    filteredTasks.Add(task);
            }
            return filteredTasks;
        }

        public void ApplyFilterPreset0()
        {
            ApplyFilterPreset(0);
        }

        public void ApplyFilterPreset1()
        {
            ApplyFilterPreset(1);
        }

        public void ApplyFilterPreset2()
        {
            ApplyFilterPreset(2);
        }

        public void ApplyFilterPreset3()
        {
            ApplyFilterPreset(3);
        }

        private void ApplyFilterPreset(int filterPresetNumber)
        {
            switch (filterPresetNumber)
            {
                case 0:
                    User.Default.FilterText = "";
                    break;
                case 1:
                    User.Default.FilterText = User.Default.FilterTextPreset1;
                    break;
                case 2:
                    User.Default.FilterText = User.Default.FilterTextPreset2;
                    break;
                case 3:
                    User.Default.FilterText = User.Default.FilterTextPreset3;
                    break;
                default:
                    return;
            }
            UpdateDisplayedTasks();
            User.Default.Save();
        }
        
        #endregion

        #region Sort Methods

        public IEnumerable<Task> SortList(IEnumerable<Task> tasks)
        {
            Log.Debug("Sorting {0} tasks by {1}", tasks.Count().ToString(), SortType.ToString());

            switch (SortType)
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
                    return tasks.OrderBy(t => (t.Completed ? "z" : "a") + (string.IsNullOrEmpty(t.DueDate) ? "9999-99-99" : t.DueDate));
                case SortType.Priority:
                    return tasks.OrderBy(t => (t.Completed ? "z" : "a") + (string.IsNullOrEmpty(t.Priority) ? "(z)" : t.Priority));
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
				case SortType.Created: 
                    return tasks.OrderBy(t => (t.Completed ? "z" : "a") + (string.IsNullOrEmpty(t.CreationDate) ? "9999-99-99" : t.CreationDate));
                default:
                    return tasks;
            }
        }

        #endregion

        #region Task Methods

        public void AddNewTask()
        {
            // create one-line string of all filter but not ones beginning with a minus, and use as the starting text for a new task
            string filters = "";
            foreach (var filter in User.Default.FilterText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (filter.Substring(0, 1) != "-")
                {
                    if (filter.Contains("due:active"))
                        filters = filters + " " + "due:today"; // If the current filter is "active", replace it here with "today"
                    else
                        filters = filters + " " + filter;
                }
            }

            _window.taskText.Text = filters;
            _window.taskText.Focus();
        }

        public void UpdateTask()
        {
            if (!IsTaskSelected()) return;
            _updating = (Task)_window.lbTasks.SelectedItem;
            _window.taskText.Text = _updating.ToString();
            _window.taskText.Select(_window.taskText.Text.Length, 0); //puts cursor at the end
            _window.taskText.Focus();
        }

        public void DeleteTask()
        {
            if (!IsTaskSelected()) return;
            var res = MessageBox.Show("Permanently delete the selected task?",
                            "Confirm Delete",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    _taskList.Delete((Task)_window.lbTasks.SelectedItem);
                }
                catch (Exception ex)
                {
                    ex.Handle("Error deleting task");
                }

                UpdateDisplayedTasks();
            }
        }

        public void CopySelectedTaskToTextBox()
        {
            var currentTask = _window.lbTasks.SelectedItem as Task;
            if (currentTask != null)
            {
                _window.taskText.Text = currentTask.Raw;
                _window.taskText.Select(_window.taskText.Text.Length, 0); // puts cursor at the end
                _window.taskText.Focus();
            }
        }

        public void ToggleCompletion()
        {
            if (!IsTaskSelected()) return;
            ToggleComplete((Task)_window.lbTasks.SelectedItem);
            UpdateDisplayedTasks();
        }

        public void IncreasePriority()
        {
            Task selectedTask = (Task)_window.lbTasks.SelectedItem;
            if (selectedTask == null) return;

            try
            {
                Task newTask = new Task(selectedTask.Raw);
                newTask.IncPriority();
                _taskList.Update(selectedTask, newTask);
            }
            catch (Exception ex)
            {
                ex.Handle("Error while changing priority.");
            }
            finally
            {
                ReloadFile();
            }
        }

        public void DecreasePriority()
        {
            Task selectedTask = (Task)_window.lbTasks.SelectedItem;
            if (selectedTask == null) return;

            try
            {
                Task newTask = new Task(selectedTask.Raw);
                newTask.DecPriority();
                _taskList.Update(selectedTask, newTask);
            }
            catch (Exception ex)
            {
                ex.Handle("Error while changing priority.");
            }
            finally
            {
                ReloadFile();
            }
        }

        public void RemovePriority()
        {
            if (!IsTaskSelected()) return;

            try
            {
                Task selectedTask = (Task)_window.lbTasks.SelectedItem;
                Task newTask = new Task(selectedTask.Raw);
                newTask.SetPriority(' ');
                _taskList.Update(selectedTask, newTask);
            }
            catch (Exception ex)
            {
                ex.Handle("Error while changing priority.");
            }
            finally
            {
                ReloadFile();
            }
        }

        public void PostponeTask()
        {
            if (!IsTaskSelected()) return;
            PostponeTask((Task)_window.lbTasks.SelectedItem, ShowPostponeDialog());
            UpdateDisplayedTasks();
        }

        private void ToggleComplete(Task task)
        {
            //Ensure an empty task can not be completed.
            if (task.Body.Trim() == string.Empty)
                return;

            var newTask = new Task(task.Raw);
            newTask.Completed = !newTask.Completed;

            try
            {
                if (User.Default.AutoArchive && newTask.Completed)
                {
                    if (User.Default.ArchiveFilePath.IsNullOrEmpty())
                        throw new Exception("You have enabled auto-archiving but have not specified an archive file.\nPlease go to File -> Options and disable auto-archiving or specify an archive file");

                    var archiveList = new TaskList(User.Default.ArchiveFilePath);
                    archiveList.Add(newTask);
                    _taskList.Delete(task);
                }
                else
                {
                    _taskList.Update(task, newTask);
                }
            }
            catch (Exception ex)
            {
                ex.Handle("An error occurred while updating the task's completed status");
            }
        }

        private void PostponeTask(Task task, int daysToPostpone)
        {
            if (daysToPostpone == 0) // if user entered 0 or junk
            {
                return;
            }

            // Get due date of the selected task. 
            // If current item doesn't have a due date, use today as the due date.
            DateTime oldDueDate = (task.DueDate.Length > 0) ?
                Convert.ToDateTime(task.DueDate) :
                DateTime.Today;

            // Add days to that date to create the new due date.
            DateTime newDueDate = oldDueDate.AddDays(daysToPostpone);

            // If the item has a due date, exchange the current due date with the new.
            // Else if the item does not have a due date, append the new due date to the task.
            string updatedRaw = (task.DueDate.Length > 0) ?
                task.Raw.Replace("due:" + task.DueDate, "due:" + newDueDate.ToString("yyyy-MM-dd")) :
                task.Raw.ToString() + " due:" + newDueDate.ToString("yyyy-MM-dd");

            // update the task
            try
            {
                _taskList.Update(task, new Task(updatedRaw));
            }
            catch (Exception ex)
            {
                ex.Handle("An error occurred while updating the task's due date.");
            }
        }

        private int ShowPostponeDialog()
        {
            var dialog = new PostponeDialog();
            dialog.Owner = _window;

            int iDays = 0;

            if (dialog.ShowDialog().Value)
            {
                string sPostpone = dialog.PostponeText.Trim();

                if (sPostpone.Length > 0)
                {
                    try
                    {
                        iDays = Convert.ToInt32(sPostpone);
                    }
                    catch
                    {
                        // No action needed.  iDays will be 0, which will leave the item unaltered.
                    }
                }
            }

            return iDays;

        }

        public void CopySelectedTasksToClipboard()
        {
            int itemCount = 0;
            StringBuilder clipboardText = new StringBuilder("");
            foreach (var item in _window.lbTasks.SelectedItems)
            {
                itemCount++;
                if (itemCount > 1)
                {
                    clipboardText.Append(Environment.NewLine);
                }
                clipboardText.Append(item.ToString());
            }
            Clipboard.SetText(clipboardText.ToString());
        }

        #endregion

        #region File Methods

        public void NewFile()
        {
            var dialog = new SaveFileDialog();
            dialog.FileName = "todo.txt";
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";

            var res = dialog.ShowDialog();

            if (res.Value)
            {
                File.WriteAllText(dialog.FileName, "");
                LoadTasks(dialog.FileName);
            }
        }

        public void OpenFile()
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";

            var res = dialog.ShowDialog();

            if (res.Value)
                LoadTasks(dialog.FileName);
        }
        
        public void ArchiveCompleted()
        {
            if (!File.Exists(User.Default.ArchiveFilePath))
                _window.File_Options(null, null);

            if (!File.Exists(User.Default.ArchiveFilePath))
                return;

            var archiveList = new TaskList(User.Default.ArchiveFilePath);
            var completed = _taskList.Tasks.Where(t => t.Completed);
            foreach (var task in completed)
            {
                archiveList.Add(task);
                _taskList.Delete(task);
            }

			UpdateDisplayedTasks();
        }

        public void ShowOptionsDialog()
        {
            var o = new Options(FontInfo.GetControlFont(_window.lbTasks));
            o.Owner = _window;

            var res = o.ShowDialog();

            if (res.Value)
            {
                User.Default.ArchiveFilePath = o.tbArchiveFile.Text;
                User.Default.AutoArchive = o.cbAutoArchive.IsChecked.Value;
                User.Default.MoveFocusToTaskListAfterAddingNewTask = o.cbMoveFocusToTaskListAfterAddingNewTask.IsChecked.Value;
                User.Default.AutoRefresh = o.cbAutoRefresh.IsChecked.Value;
                User.Default.FilterCaseSensitive = o.cbCaseSensitiveFilter.IsChecked.Value;
                User.Default.AddCreationDate = o.cbAddCreationDate.IsChecked.Value;
                User.Default.DebugLoggingOn = o.cbDebugOn.IsChecked.Value;
                User.Default.MinimiseToSystemTray = o.cbMinToSysTray.IsChecked.Value;
                User.Default.RequireCtrlEnter = o.cbRequireCtrlEnter.IsChecked.Value;
                User.Default.AllowGrouping = o.cbAllowGrouping.IsChecked.Value;

                // Unfortunately, font classes are not serializable, so all the pieces are tracked instead.
                User.Default.TaskListFontFamily = o.TaskListFont.Family.ToString();
                User.Default.TaskListFontSize = o.TaskListFont.Size;
                User.Default.TaskListFontStyle = o.TaskListFont.Style.ToString();
                User.Default.TaskListFontWeight = o.TaskListFont.Weight.ToString();
                User.Default.TaskListFontStretch = o.TaskListFont.Stretch.ToString();
                User.Default.TaskListFontBrushColor = o.TaskListFont.BrushColor.ToString();

                User.Default.Save();

                Log.LogLevel = User.Default.DebugLoggingOn ? LogLevel.Debug : LogLevel.Error;

                _window.SetFont();

				UpdateDisplayedTasks();
            }
        }

        #endregion

        #region Help Methods

        public void ShowHelpDialog()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            HelpPage = new Help("todotxt.net", version, Resource.HelpText, Resource.SiteUrl, "benrhughes.com/todotxt.net");

            HelpPage.Show();
        }

        public void ViewLog()
        {
            if (File.Exists(Log.LogFile))
                Process.Start(Log.LogFile);
            else
                MessageBox.Show("Log file does not exist: no errors have been logged", "Log file does not exist", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void Donate()
        {
            Process.Start(Resource.SiteUrl);
        }

        //  Add a quick calendar of the next 7 days to the title bar.  If the calendar is already displayed, toggle it off.
        public void AddCalendarToTitle()
        {
            var title = _window.Title;

            if (title.Length < 15)
            {
                title += "       Calendar:  ";

                for (double i = 0; i < 7; i++)
                {
                    var today = DateTime.Now.AddDays(i).ToString("MM-dd");
                    var today_letter = DateTime.Now.AddDays(i).DayOfWeek.ToString();
                    today_letter = today_letter.Remove(2);
                    title += "  " + today_letter + ":" + today;
                }
            }
            else
            {
                title = "todotxt.net";
            }

            _window.Title = title;
        }

        #endregion

        #region New Task TextBox Event Handling Methods

        internal void TaskTextPreviewKeyUp(KeyEventArgs e)
        {
            if (_taskList == null)
            {
                MessageBox.Show("You don't have a todo.txt file open - please use File\\New or File\\Open",
                    "Please open a file", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
                _window.lbTasks.Focus();
                return;
            }

            if (ShouldAddTask(e))
            {
                if (_updating == null)
                {
                    try
                    {
                        var taskDetail = _window.taskText.Text.Trim();

                        if (taskDetail.Length > 0)
                        {
                            if (User.Default.AddCreationDate)
                            {
                                var tmpTask = new Task(taskDetail);
                                var today = DateTime.Today.ToString("yyyy-MM-dd");

                                if (string.IsNullOrEmpty(tmpTask.CreationDate))
                                {
                                    if (string.IsNullOrEmpty(tmpTask.Priority))
                                        taskDetail = today + " " + taskDetail;
                                    else
                                        taskDetail = taskDetail.Insert(tmpTask.Priority.Length, " " + today);
                                }
                            }
                            _taskList.Add(new Task(taskDetail));
                        }
                    }
                    catch (TaskException ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    _taskList.Update(_updating, new Task(_window.taskText.Text.Trim()));
                    _updating = null;
                }

                _window.taskText.Text = "";
				UpdateDisplayedTasks();

                _window.Intellisense.IsOpen = false;
                if (User.Default.MoveFocusToTaskListAfterAddingNewTask)
                {
                    _window.lbTasks.Focus();
                }
                else
                {
                    _window.taskText.Focus();
                }
                

                return;
            }

            if (_window.Intellisense.IsOpen && !_window.IntellisenseList.IsFocused)
            {
                if (_window.taskText.CaretIndex <= _intelliPos) // we've moved behind the symbol, drop out of intellisense
                {
                    _window.Intellisense.IsOpen = false;
                    return;
                }

                switch (e.Key)
                {
                    case Key.Down:
                        _window.IntellisenseList.Focus();
                        Keyboard.Focus(_window.IntellisenseList);
                        _window.IntellisenseList.SelectedIndex = 0;
                        break;
                    case Key.Escape:
                    case Key.Space:
                        _window.Intellisense.IsOpen = false;
                        break;
                    default:
                        var word = FindIntelliWord();
                        _window.IntellisenseList.Items.Filter = (o) => o.ToString().Contains(word);
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        _updating = null;
                        _window.taskText.Text = "";
                        _window.lbTasks.Focus();
                        break;
                }
            }
        }

        public void TaskTextPreviewTextChanged(TextChangedEventArgs e)
        {
            if (e.Changes.Count != 1) return;
            if (e.Changes.First().AddedLength < 1) return;
            if (_window.taskText.CaretIndex < 1) return;

            var lastAddedCharacter = _window.taskText.Text.Substring(_window.taskText.CaretIndex - 1, 1);

            switch (lastAddedCharacter)
            {
                case "+":
                    var projects = _taskList.Tasks.SelectMany(task => task.Projects);
                    _intelliPos = _window.taskText.CaretIndex - 1;
                    ShowIntellisense(projects.Distinct().OrderBy(s => s), _window.taskText.GetRectFromCharacterIndex(_intelliPos));
                    break;

                case "@":
                    var contexts = _taskList.Tasks.SelectMany(task => task.Contexts);
                    _intelliPos = _window.taskText.CaretIndex - 1;
                    ShowIntellisense(contexts.Distinct().OrderBy(s => s), _window.taskText.GetRectFromCharacterIndex(_intelliPos));
                    break;
            }
        }

        public void ShowIntellisense(IEnumerable<string> s, Rect placement)
        {
            if (s.Count() == 0)
                return;

            _window.Intellisense.PlacementTarget = _window.taskText;
            _window.Intellisense.PlacementRectangle = placement;

            _window.IntellisenseList.ItemsSource = s;
            _window.Intellisense.IsOpen = true;
            _window.taskText.Focus();
        }

        /// <summary>
        /// Tab, Enter and Space keys will all added the selected text into the task string.
        /// Escape key cancels out.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">The key to trigger on.</param>
        public void IntellisenseKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Tab:
                case Key.Space:
                    InsertTextIntoTaskString();
                    break;
                case Key.Escape:
                    _window.Intellisense.IsOpen = false;
                    _window.taskText.CaretIndex = _window.taskText.Text.Length;
                    _window.taskText.Focus();
                    break;
            }
        }

        public void IntellisenseMouseUp()
        {
            InsertTextIntoTaskString();
        }

        private string FindIntelliWord()
        {
            return _window.taskText.Text.Substring(_intelliPos + 1, _window.taskText.CaretIndex - _intelliPos - 1);
        }

        /// <summary>
        /// Helper function to add the chosen text in the intellisense into the task string.
        /// Created to allow the use of both keyboard and mouse clicks.
        /// </summary>
        private void InsertTextIntoTaskString()
        {
            _window.Intellisense.IsOpen = false;

            _window.taskText.Text = _window.taskText.Text.Remove(_intelliPos, _window.taskText.CaretIndex - _intelliPos);

            var newText = _window.IntellisenseList.SelectedItem.ToString();
            _window.taskText.Text = _window.taskText.Text.Insert(_intelliPos, newText);
            _window.taskText.CaretIndex = _intelliPos + newText.Length;

            _window.taskText.Focus();
        }


        #endregion

        #region Print Methods

        public void SetPrintControlsVisibility(bool PrintControlsVisibility)
        {
            if (PrintControlsVisibility)
            {   // Show Printer Controls
                _window.webBrowser1.Visibility = Visibility.Visible;
                _window.btnPrint.Visibility = Visibility.Visible;
                _window.btnCancelPrint.Visibility = Visibility.Visible;
                _window.lbTasks.Visibility = Visibility.Hidden;
                _window.menu1.Visibility = Visibility.Hidden;
                _window.taskText.Visibility = Visibility.Hidden;
            }
            else
            {   // Hide Printer Controls
                _window.webBrowser1.Visibility = Visibility.Hidden;
                _window.btnPrint.Visibility = Visibility.Hidden;
                _window.btnCancelPrint.Visibility = Visibility.Hidden;
                _window.lbTasks.Visibility = Visibility.Visible;
                _window.menu1.Visibility = Visibility.Visible;
                _window.taskText.Visibility = Visibility.Visible;
            }
        }

        public string GetPrintContents()
        {
            if (_window.lbTasks.Items == null || _window.lbTasks.Items.IsEmpty)
                return "";


            var contents = new StringBuilder();

            contents.Append("<html><head>");
            contents.Append("<title>todotxt.net</title>");
            contents.Append("<style>" + Resource.CSS + "</style>");
            contents.Append("</head>");

            contents.Append("<body>");
            contents.Append("<h2>todotxt.net</h2>");
            contents.Append("<table>");
            contents.Append("<tr class='tbhead'><th>&nbsp;</th><th>Done</th><th>Created</th><th>Due</th><td>Details</td></tr>");

            int currentTaskNumber = 0;
            _nextGroupAtTaskNumber = 0;

            foreach (Task task in _window.lbTasks.Items)
            {
                if (User.Default.AllowGrouping)
                {
                    //Do we need to emit a Group Header?
                    if (!_myView.Groups.IsNullOrEmpty() && currentTaskNumber == _nextGroupAtTaskNumber)
                    {
                        //We do need to emit one
                        if (_viewGroups.IsNullOrEmpty())
                        {
                            //For Group Headers
                            _viewGroups = _myView.Groups.Cast<CollectionViewGroup>().ToList();
                        }

                        List<GroupStyle> name = _window.lbTasks.GroupStyle.ToList();
                        contents.Append(EmitGroupHeader());
                        _nextGroupAtTaskNumber = _numberOfItemsInCurrentGroup + currentTaskNumber;
                    }
                }

                if (task.Completed)
                {
                    contents.Append("<tr class='completedTask'>");
                    contents.Append("<td class='complete'>x</td> ");
                    contents.Append("<td class='completeddate'>" + task.CompletedDate + "</td> ");
                }
                else
                {
                    contents.Append("<tr class='uncompletedTask'>");
                    if (string.IsNullOrEmpty(task.Priority) || task.Priority == "N/A")
                        contents.Append("<td>&nbsp;</td>");
                    else
                        contents.Append("<td><span class='priority'>" + task.Priority + "</span></td>");

                    contents.Append("<td>&nbsp;</td>");
                }

                if (string.IsNullOrEmpty(task.CreationDate) || task.CreationDate == "N/A")
                    contents.Append("<td>&nbsp;</td>");
                else
                    contents.Append("<td class='startdate'>" + task.CreationDate + "</td>");
                if (string.IsNullOrEmpty(task.DueDate) || task.DueDate == "N/A")
                    contents.Append("<td>&nbsp;</td>");
                else
                    contents.Append("<td class='enddate'>" + task.DueDate + "</td>");

                contents.Append("<td>" + task.Body);

                task.Projects.ForEach(project => contents.Append(" <span class='project'>" + project + "</span> "));

                task.Contexts.ForEach(context => contents.Append(" <span class='context'>" + context + "</span> "));

                contents.Append("</td>");

                contents.Append("</tr>");
                
                currentTaskNumber++;
            }

            contents.Append("</table></body></html>");

            return contents.ToString();
        }

        private string EmitGroupHeader()
        {

            //Emit the header

            //Remove it from the stack

            //Reset the number of items in the group

            if (!_myView.Groups.IsNullOrEmpty() && _myView.GroupDescriptions != null && _myView.Groups.Count > 0)
            {
                    _numberOfItemsInCurrentGroup = _viewGroups[0].ItemCount;
                    string name = _viewGroups[0].Name.ToString();

                    _viewGroups.RemoveAt(0);
                    return "<tr><th colspan=\"5\"><h3>" + name + "</h3></th></tr>";              
            }

            return "";

        }

        #endregion
    }
}