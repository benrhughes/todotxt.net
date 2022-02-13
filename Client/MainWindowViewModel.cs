using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using ToDoLib;
using System.Windows;
using System.Windows.Input;
using System.IO;
using ColorFont;
using CommonExtensions;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Win32;
using System.Windows.Threading;

namespace Client
{
    // INotifyPropertyChanged interface implemented to notify UI for status bar changes.
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private CollectionView _myView;
        private FileChangeObserver _changefile;
        private SortType _sortType;
        private MainWindow _window;
        private Task _updating;
        private int _numberOfItemsInCurrentGroup;
        private List<CollectionViewGroup> _viewGroups;
        private int _nextGroupAtTaskNumber;
        private List<Task> _selectedTasks;

        public TaskList TaskList { get; set; }
        public Help HelpPage { get; private set; }
        public SortType SortType
        {
            get { return _sortType; }
            set
            {
                bool raiseEvent = false;
                if (_sortType != value)
                {
                    User.Default.CurrentSort = (int)value;
                    User.Default.Save();
                    raiseEvent = true;
                }

                _sortType = value;

                if(raiseEvent)
                {
                    RaiseProperyChanged(nameof(SortType));
                }
            }
        }

        private int _activeFilterNumber = -1;
        public int ActiveFilterNumber
        {
            get
            {
                return _activeFilterNumber;
            }
            private set
            {
                if (_activeFilterNumber != value)
                {
                    _activeFilterNumber = value;
                    RaiseProperyChanged(nameof(ActiveFilterNumber));
                }
            }
        }

        private int totalTasks = 0;
        public int TotalTasks
        {
            get
            {
                return totalTasks;
            }

            set
            {
                if (totalTasks != value)
                {
                    totalTasks = value;
                    RaiseProperyChanged(nameof(TotalTasks));
                }
            }
        }

        private int filteredTasks = 0;
        public int FilteredTasks
        {
            get
            {
                return filteredTasks;
            }

            set
            {
                if (filteredTasks != value)
                {
                    filteredTasks = value;
                    RaiseProperyChanged(nameof(FilteredTasks));
                }
            }
        }

        private int incompleteTasks = 0;
        public int IncompleteTasks
        {
            get
            {
                return incompleteTasks;
            }

            set
            {
                if (incompleteTasks != value)
                {
                    incompleteTasks = value;
                    RaiseProperyChanged(nameof(IncompleteTasks));
                }
            }
        }

        private int tasksDueToday = 0;

        public int TasksDueToday
        {
            get
            {
                return tasksDueToday;
            }

            set
            {
                if (tasksDueToday != value)
                {
                    tasksDueToday = value;
                    RaiseProperyChanged(nameof(TasksDueToday));
                }
            }
        }

        public int TasksOverdue
        {
            get
            {
                return tasksOverdue;
            }

            set
            {
                if (tasksOverdue != value)
                {
                    tasksOverdue = value;
                    RaiseProperyChanged(nameof(TasksOverdue));
                }
            }
        }

        private int tasksOverdue = 0;




        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel(MainWindow window)
        {
            _window = window;
            _selectedTasks = new List<Task>();

            Log.LogLevel = User.Default.DebugLoggingOn ? LogLevel.Debug : LogLevel.Error;

            Log.Debug("Initializing Todotxt.net");

            SortType = (SortType)User.Default.CurrentSort;

            ActiveFilterNumber=0;

            if (!string.IsNullOrEmpty(User.Default.FilePath))
            {
                LoadTasks(User.Default.FilePath);
            }

            window.sbTaskSummary.Visibility = User.Default.DisplayStatusBar ? Visibility.Visible : Visibility.Collapsed;
        }

        #region File Change Observer

        private void EnableFileChangeObserver()
        {
            if (!User.Default.AutoRefresh)
            {
                return;
            }

            Log.Debug("Enabling file change observer for '{0}'", User.Default.FilePath);
            _changefile = new FileChangeObserver();
            _changefile.OnFileChanged += () => _window.Dispatcher.BeginInvoke(new Action(ReloadFile));
            _changefile.ObserveFile(User.Default.FilePath);
            Log.Debug("File change observer enabled");
        }

        private void DisableFileChangeObserver()
        {
            if (_changefile == null)
            {
                return;
            }

            Log.Debug("Disabling file change observer for '{0}'", User.Default.FilePath);
            _changefile.Dispose();
            _changefile = null;
            Log.Debug("File change observer disabled");
        }

        #endregion

        #region Task List ListBox Management Methods

        /// <summary>
        /// Captures the list box's selection on the task list. To be used prior to modifying or sorting and reloading the task list.
        /// The SetSelectedTasks() method reapplies the selections captured in this method.
        /// </summary>
        private void GetSelectedTasks()
        {
            _selectedTasks.Clear();
            foreach (var task in _window.lbTasks.SelectedItems)
            {
                _selectedTasks.Add((Task)task);
            }
        }

        /// <summary>
        /// Restores the task list box's selection that was captured by the GetSelectedTasks() method.
        /// To be used after reloading the task list after a modification or sort.
        /// </summary>
        private void SetSelectedTasks()
        {
            if (_selectedTasks == null || _selectedTasks.Count == 0)
            {
                _window.lbTasks.SelectedIndex = 0;
                return;
            }

            _window.lbTasks.SelectedItems.Clear();
            int selectedItemCount = 0;

            // Loop through the listbox tasks, then loop through the items that should be selected. If the items match, select them in the list box.
            for (int i = 0; i < _window.lbTasks.Items.Count; i++)
            {
                Task listBoxItemTask = _window.lbTasks.Items[i] as Task;
                int j = 0;
                while (j < _selectedTasks.Count)
                {
                    Task task = _selectedTasks[j];
                    if (listBoxItemTask.Raw.Equals(task.Raw))
                    {
                        _window.lbTasks.SelectedItems.Add(_window.lbTasks.Items[i]);

                        // Make sure the keyboard focus is on the uppermost or only task that is to be selected in the task list.
                        selectedItemCount++;
                        if (selectedItemCount == 1)
                        {
                            SelectTaskByIndex(i);
                        }

                        _selectedTasks.RemoveAt(j);
                        break;
                    }
                    else
                    {
                        j++;
                    }
                }
            }

            if (selectedItemCount == 0)
            {
                _window.lbTasks.SelectedIndex = 0; 
                SelectTaskByIndex(0);
            }
        }

        /// <summary>
        /// Selects a task in the listbox based on its index in _window.lbtasks.Items
        /// </summary>
        /// <param name="index">Index of the task to select</param>
        private void SelectTaskByIndex(int index)
        {
            try
            {
                var listBoxItem = (ListBoxItem)_window.lbTasks.ItemContainerGenerator.ContainerFromItem(_window.lbTasks.Items[index]);
                listBoxItem.Focus();
            }
            catch
            {
                _window.lbTasks.Focus();
            }
        }

        public void LoadTasks(string filePath)
        {
            Log.Debug("Loading tasks"); 
            try
            {
                TaskList = new TaskList(filePath, User.Default.PreserveWhiteSpace);
                if (TaskList != null)
                {
                    // The first time the task list has been modified before we got a chance to hook to the modified event
                    // so call the method and then hook to the Modified event
                    TaskList.Modified += TaskList_Modified;
                    TaskList_Modified(TaskList, EventArgs.Empty);
                }
                User.Default.FilePath = filePath;
                User.Default.Save();
                EnableFileChangeObserver();
				UpdateDisplayedTasks();
                _window.SetSelectionOfMenuItem("FutureTasks", User.Default.FilterFutureTasks);
                _window.SetSelectionOfMenuItem("HiddenTasks", User.Default.ShowHidenTasks);
            }
            catch (Exception ex)
            {
                ex.Handle("An error occurred while opening " + filePath);
            }
        }

        private void TaskList_Modified(object sender, EventArgs e)
        {
            this.TotalTasks = TaskList.Tasks.Count;
        }

        public void ReloadFile()
        {
            Log.Debug("Reloading file");
            try
            {
                TaskList.ReloadTasks();
            }
            catch (Exception ex)
            {
                ex.Handle("Error loading tasks");
            }
            GetSelectedTasks();
            UpdateDisplayedTasks();
            SetSelectedTasks();
        }

        public void UpdateDisplayedTasks()
        {
            if (TaskList == null)
            {
                return;
            }

            string sortProperty = "";

            try
            {
                var sortedTaskList = FilterList(TaskList.Tasks);
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

                var selectedTasksList = sortedTaskList.ToList();
                _window.lbTasks.ItemsSource = sortedTaskList;
                _window.lbTasks.UpdateLayout();
                //sortedTaskList.
                UpdateSummary(selectedTasksList);
            }
            catch (Exception ex)
            {
                ex.Handle("Error while sorting tasks");
            }

            // Set the menu item to Bold to easily identify if there is a filter in force
            _window.filterMenu.FontWeight = User.Default.FilterText.Length == 0 ? FontWeights.Normal : FontWeights.Bold;
        }

        protected void UpdateSummary(List<Task> selectedTasksList)
        {
            FilteredTasks = selectedTasksList.Count;

            int fTask = 0, incompTask = 0, dueTodayTask = 0, overdueTask = 0;
            foreach (Task t in selectedTasksList)
            {
                if (!t.Completed)
                {
                    incompTask++;


                    if (!String.IsNullOrEmpty(t.DueDate))
                    {
                        DateTime dueDt;

                        if (DateTime.TryParseExact(t.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dueDt))
                        {
                            if (dueDt.Date == DateTime.Today.Date)
                            {
                                dueTodayTask++;
                            }
                            else if (dueDt.Date < DateTime.Today)
                            {
                                overdueTask++;
                            }
                        }
                    }
                }
            }
            IncompleteTasks = incompTask;
            TasksOverdue = overdueTask;
            TasksDueToday = dueTodayTask;
        }

        /// <summary>
        /// Returns true if a single task is selected in the task list.
        /// </summary>
        /// <returns>True if a single task is selected in the task list. False if zero or more than one tasks are selected in the task list.</returns>
        private bool IsTaskSelected()
        {
            return (_window.lbTasks.SelectedItems.Count == 1);
        }

        /// <summary>
        /// Returns true if a one or more task is selected in the task list.
        /// </summary>
        /// <returns>True if one or more tasks are selected in the task list. False if zero or one task is selected.</returns>
        private bool AreTasksSelected()
        {
            return (_window.lbTasks.SelectedItems.Count > 0);
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

        /// <summary>
        /// Emulates a keydown event, as if "key" were pressed when the "target" control has focus.
        /// </summary>
        /// <param name="target">Control to send keyDown event to.</param>
        /// <param name="key">Key press to emulate in KeyDown event.</param>
        private void SendKeyDownEvent(Control target, Key key)
        {
            var routedEvent = Keyboard.KeyDownEvent; // Event to send

            target.RaiseEvent(
               new KeyEventArgs(
                 Keyboard.PrimaryDevice,
                 PresentationSource.FromVisual(target),
                 0,
                 key) { RoutedEvent = routedEvent }
             );
        }

        public void EmulateDownArrow()
        {
            SendKeyDownEvent(_window.lbTasks, Key.Down);
        }

        public void EmulateUpArrow()
        {
            SendKeyDownEvent(_window.lbTasks, Key.Up);
        }

        #endregion

        #region Filter Methods

        public void ShowFilterDialog()
        {
            var f = new FilterDialog(this);
            f.Owner = _window;

            f.FilterText = User.Default.FilterText;
            f.FilterTextPreset1 = User.Default.FilterTextPreset1;
            f.FilterTextPreset2 = User.Default.FilterTextPreset2;
            f.FilterTextPreset3 = User.Default.FilterTextPreset3;
            f.FilterTextPreset4 = User.Default.FilterTextPreset4;
            f.FilterTextPreset5 = User.Default.FilterTextPreset5;
            f.FilterTextPreset6 = User.Default.FilterTextPreset6;
            f.FilterTextPreset7 = User.Default.FilterTextPreset7;
            f.FilterTextPreset8 = User.Default.FilterTextPreset8;
            f.FilterTextPreset9 = User.Default.FilterTextPreset9;

            if (f.ShowDialog().Value)
            {
                User.Default.FilterText = f.FilterText.Trim();
                User.Default.FilterTextPreset1 = f.FilterTextPreset1.Trim();
                User.Default.FilterTextPreset2 = f.FilterTextPreset2.Trim();
                User.Default.FilterTextPreset3 = f.FilterTextPreset3.Trim();
                User.Default.FilterTextPreset4 = f.FilterTextPreset4.Trim();
                User.Default.FilterTextPreset5 = f.FilterTextPreset5.Trim();
                User.Default.FilterTextPreset6 = f.FilterTextPreset6.Trim();
                User.Default.FilterTextPreset7 = f.FilterTextPreset7.Trim();
                User.Default.FilterTextPreset8 = f.FilterTextPreset8.Trim();
                User.Default.FilterTextPreset9 = f.FilterTextPreset9.Trim();

                User.Default.Save();

                GetSelectedTasks();
				UpdateDisplayedTasks();
                SetSelectedTasks();
            }
        }

        public static IEnumerable<Task> FilterList(IEnumerable<Task> tasks)
        {
            var filters = User.Default.FilterText;
            var comparer = User.Default.FilterCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

            var filteredTasks = new List<Task>();

            foreach (var task in tasks)
            {
                bool include = true;
                if (!User.Default.ShowHidenTasks)
                    include = !task.Raw.Contains("h:1");

                if (include)
                    if (User.Default.FilterFutureTasks)
                        include = String.IsNullOrEmpty(task.ThresholdDate) || task.ThresholdDate.IsDateLessThan(DateTime.Now.AddDays(1));

                if (include)
                {
                    foreach (
                        var filter in
                        filters.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (filter.Equals("due:today", StringComparison.OrdinalIgnoreCase)
                            && task.DueDate == DateTime.Now.ToString("yyyy-MM-dd"))
                            continue;
                        if (filter.Equals("due:future", StringComparison.OrdinalIgnoreCase)
                            && task.DueDate.IsDateGreaterThan(DateTime.Now))
                            continue;
                        if (filter.Equals("due:past", StringComparison.OrdinalIgnoreCase)
                            && task.DueDate.IsDateLessThan(DateTime.Now))
                            continue;
                        if (filter.Equals("due:active", StringComparison.OrdinalIgnoreCase)
                            && !task.DueDate.IsNullOrEmpty()
                            && !task.DueDate.IsDateGreaterThan(DateTime.Now))
                            continue;
                        if (filter.Equals("-due:today", StringComparison.OrdinalIgnoreCase)
                            && task.DueDate == DateTime.Now.ToString("yyyy-MM-dd"))
                        {
                            include = false;
                            continue;
                        }
                        if (filter.Equals("-due:future", StringComparison.OrdinalIgnoreCase)
                            && task.DueDate.IsDateGreaterThan(DateTime.Now))
                        {
                            include = false;
                            continue;
                        }
                        if (filter.Equals("-due:past", StringComparison.OrdinalIgnoreCase)
                            && task.DueDate.IsDateLessThan(DateTime.Now))
                        {
                            include = false;
                            continue;
                        }
                        if (filter.Equals("-due:active", StringComparison.OrdinalIgnoreCase)
                            && !task.DueDate.IsNullOrEmpty()
                            && !task.DueDate.IsDateGreaterThan(DateTime.Now))
                        {
                            include = false;
                            continue;
                        }

                        // "DONE" filter is different from "due" filter in that
                        // "due" is part of todotxt syntax and would very unlikely occur in task text
                        // while the word "done" can occur in the text
                        if (filter.Equals("-DONE", StringComparison.Ordinal))
                        {
                            if (task.Completed) include = false;
                        }
                        else if (filter.Equals("DONE", StringComparison.Ordinal))
                        {
                            if (!task.Completed) include = false;
                        }
                        // so if the filter is "DONE" or "-DONE", pass the substring test
                        else
                        {
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

        public void ApplyFilterPreset4()
        {
            ApplyFilterPreset(4);
        }

        public void ApplyFilterPreset5()
        {
            ApplyFilterPreset(5);
        }

        public void ApplyFilterPreset6()
        {
            ApplyFilterPreset(6);
        }

        public void ApplyFilterPreset7()
        {
            ApplyFilterPreset(7);
        }

        public void ApplyFilterPreset8()
        {
            ApplyFilterPreset(8);
        }

        public void ApplyFilterPreset9()
        {
            ApplyFilterPreset(9);
        }

        public void ApplyHideFutureTasks()
        {
            User.Default.FilterFutureTasks = !User.Default.FilterFutureTasks;
            UpdateDisplayedTasks();
            SetSelectedTasks();

            User.Default.Save();
        }

        public void ApplyShowHiddenTasks()
        {
            User.Default.ShowHidenTasks = !User.Default.ShowHidenTasks;

            GetSelectedTasks();
            UpdateDisplayedTasks();
            SetSelectedTasks();

            User.Default.Save();
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
                case 4:
                    User.Default.FilterText = User.Default.FilterTextPreset4;
                    break;
                case 5:
                    User.Default.FilterText = User.Default.FilterTextPreset5;
                    break;
                case 6:
                    User.Default.FilterText = User.Default.FilterTextPreset6;
                    break;
                case 7:
                    User.Default.FilterText = User.Default.FilterTextPreset7;
                    break;
                case 8:
                    User.Default.FilterText = User.Default.FilterTextPreset8;
                    break;
                case 9:
                    User.Default.FilterText = User.Default.FilterTextPreset9;
                    break;
                default:
                    return;
            }
            
            GetSelectedTasks();
            UpdateDisplayedTasks();
            SetSelectedTasks();
            
            User.Default.Save();

            ActiveFilterNumber = filterPresetNumber;
        }
        #endregion

        #region Sort Methods

        public void SortList(SortType sortType)
        {
            GetSelectedTasks();
            this.SortType = sortType;
            UpdateDisplayedTasks();
            SetSelectedTasks();
        }

        public IEnumerable<Task> SortList(IEnumerable<Task> tasks)
        {
            Log.Debug("Sorting {0} tasks by {1}.", tasks.Count().ToString(), SortType.ToString());

            switch (SortType)
            {
                case SortType.Completed:
                    _window.SetSelectedMenuItem(_window.sortMenu, "Completed");
                    return tasks.OrderBy(t => t.Completed)
                        .ThenBy(t => (string.IsNullOrEmpty(t.Priority) ? "(zzz)" : t.Priority))
                        .ThenBy(t => (string.IsNullOrEmpty(t.DueDate) ? "9999-99-99" : t.DueDate))
                        .ThenBy(t => (string.IsNullOrEmpty(t.CreationDate) ? "0000-00-00" : t.CreationDate));
                
                case SortType.Context:
                    _window.SetSelectedMenuItem(_window.sortMenu, "Context");
                    return tasks.OrderBy(t =>
                        {
                            var s = "";
                            if (t.Contexts != null && t.Contexts.Count > 0)
                                s += t.PrimaryContext;
                            else
                                s += "zzz";
                            return s;
                        })
                        .ThenBy(t => t.Completed)
                        .ThenBy(t => (string.IsNullOrEmpty(t.Priority) ? "(zzz)" : t.Priority))
                        .ThenBy(t => (string.IsNullOrEmpty(t.DueDate) ? "9999-99-99" : t.DueDate))
                        .ThenBy(t => (string.IsNullOrEmpty(t.CreationDate) ? "0000-00-00" : t.CreationDate));

                case SortType.Alphabetical:
                    _window.SetSelectedMenuItem(_window.sortMenu, "Alphabetical");
                    return tasks.OrderBy(t => t.Raw);

                case SortType.DueDate:
                    _window.SetSelectedMenuItem(_window.sortMenu, "DueDate");
                    return tasks.OrderBy(t => (string.IsNullOrEmpty(t.DueDate) ? "9999-99-99" : t.DueDate))
                        .ThenBy(t => t.Completed)
                        .ThenBy(t => (string.IsNullOrEmpty(t.Priority) ? "(zzz)" : t.Priority))
                        .ThenBy(t => (string.IsNullOrEmpty(t.CreationDate) ? "0000-00-00" : t.CreationDate));

                case SortType.Priority:
                    _window.SetSelectedMenuItem(_window.sortMenu, "Priority");
                    return tasks.OrderBy(t => (string.IsNullOrEmpty(t.Priority) ? "(zzz)" : t.Priority))
                        .ThenBy(t => t.Completed)
                        .ThenBy(t => (string.IsNullOrEmpty(t.DueDate) ? "9999-99-99" : t.DueDate))
                        .ThenBy(t => (string.IsNullOrEmpty(t.CreationDate) ? "0000-00-00" : t.CreationDate));

                case SortType.Project:
                    _window.SetSelectedMenuItem(_window.sortMenu, "Project");
                    return tasks
                        .OrderBy(t =>
                        {
                            var s = "";
                            if (t.Projects != null && t.Projects.Count > 0)
                                s += t.PrimaryProject;
                            else
                                s += "zzz";
                            return s;
                        })
                        .ThenBy(t => t.Completed)
                        .ThenBy(t => (string.IsNullOrEmpty(t.Priority) ? "(zzz)" : t.Priority))
                        .ThenBy(t => (string.IsNullOrEmpty(t.DueDate) ? "9999-99-99" : t.DueDate))
                        .ThenBy(t => (string.IsNullOrEmpty(t.CreationDate) ? "0000-00-00" : t.CreationDate));
				
                case SortType.Created:
                    _window.SetSelectedMenuItem(_window.sortMenu, "CreatedDate");
                    return tasks.OrderBy(t => (string.IsNullOrEmpty(t.CreationDate) ? "0000-00-00" : t.CreationDate))
                        .ThenBy(t => t.Completed)
                        .ThenBy(t => (string.IsNullOrEmpty(t.Priority) ? "(zzz)" : t.Priority))
                        .ThenBy(t => (string.IsNullOrEmpty(t.DueDate) ? "9999-99-99" : t.DueDate));

                default:
                    _window.SetSelectedMenuItem(_window.sortMenu, "File");
                    return tasks;
            }
        }

        #endregion

        #region Edit Methods

        public void CopySelectedTaskToTextBox()
        {
            if (!IsTaskSelected())
            {
                return;
            }

            var currentTask = _window.lbTasks.SelectedItem as Task;
            if (currentTask != null)
            {
                _window.taskText.Text = currentTask.Raw;
                _window.taskText.Select(_window.taskText.Text.Length, 0); // puts cursor at the end
                _window.taskText.Focus();
            }
        }

        public void PasteTasksIntoTaskList()
        {
            // Abort if clipboard does not contain text.
            if (!Clipboard.ContainsText())
            {
                return;
            }

            // Split clipboard text into lines.
            string clipboardText = Clipboard.GetText();
            string[] clipboardLines = clipboardText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            DisableFileChangeObserver();

            // Add each line from the clipboard to the task list.
            foreach (string clipboardLine in clipboardLines)
            {
                TaskList.Add(new Task(clipboardLine));
            }
            
            UpdateDisplayedTasks();

            EnableFileChangeObserver();
        }

        #endregion

        #region Task Methods

        /// <summary>
        /// This is a generic method used to update one or more selected tasks in the task list.
        /// It is called by all the methods that update one or more tasks in the task list.
        /// It is not called by the task update method or the task deletion method.
        /// </summary>
        /// <param name="modificationFunction">A function that returns a Task object and takes as parameters a Task object and a dynamic variable.</param>
        /// <param name="parameter">The parameter (or null) to pass to modificationFunction.</param>
        public void ModifySelectedTasks(Func<Task, dynamic, Task> modificationFunction, dynamic parameter = null)
        {
            // Abort if no tasks are selected.
            if (!AreTasksSelected()) { 
                return; 
            }

            DisableFileChangeObserver();
            
            // Save which tasks were selected, because the selection will be lost later when UpdateDisplayedTasks() is called. 
            GetSelectedTasks();

            // Make sure we are working with the latest version of the file.
            TaskList.ReloadTasks();

            // For each selected task, perform the modification, update the task list, and update the copy of the task in _selectedTasks.
            foreach (var task in _selectedTasks)
        	{
                Task newTask = modificationFunction(task, parameter);
                TaskList.Update(task, newTask);
                task.Raw = newTask.Raw;
	        }

            // Reload the task list and re-sort it.
            UpdateDisplayedTasks();

            // Re-apply the selections captured above in the GetSelectedTasks() method call.
            SetSelectedTasks();

            EnableFileChangeObserver();
        }

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
                    else if (filter.Equals("DONE", StringComparison.Ordinal)
                        || filter.Equals("-DONE", StringComparison.Ordinal))
                        continue;
                    else
                        filters = filters + " " + filter;
                }
            }

            _window.taskText.Text = filters;
            _window.taskText.Focus();
        }

        public void AddNewTaskWithPriority()
        {
            AddNewTask();
            _window.taskText.Text = _window.taskText.Text.Length > 0 ? $"( {_window.taskText.Text}" : "(";
            _window.taskText.CaretIndex = 1;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>_window.taskText.CheckKeyAndShowPopup()));
        }

        public void UpdateTask()
        {
            // Abort if no task, or more than one task, is selected.
            if (!IsTaskSelected())
            {
                return;
            }
            _updating = (Task)_window.lbTasks.SelectedItem;
            _window.taskText.Text = _updating.ToString();
            _window.taskText.Select(_window.taskText.Text.Length, 0); // puts cursor at the end
            _window.taskText.Focus();
        }

        public void DeleteTasks()
        {
            if (!AreTasksSelected())
            {
                return;
            }

            bool isTaskListFocused = _window.lbTasks.IsKeyboardFocusWithin;

            var res = ShowDeleteConfirmationDialog();
            if (!res)
            {
                return;
            }

            if (_window.lbTasks.SelectedItems.Contains(_updating))
            {
                _updating = null;
            }

            GetSelectedTasks();
            
            DisableFileChangeObserver();

            try
            {
                // Make sure we are working with the latest version of the file.
                TaskList.ReloadTasks();

                foreach (var task in _window.lbTasks.SelectedItems)
                {
                    TaskList.Delete((Task)task);
                }
            }
            catch (Exception ex)
            {
                ex.Handle("Error deleting task");
            }

            UpdateDisplayedTasks();

            if (isTaskListFocused)
            {
                SelectTaskByIndex(0);
            }

            EnableFileChangeObserver();
        }

        private bool ShowDeleteConfirmationDialog()
        {
            if (!AreTasksSelected())
            {
                return false;
            }

            var dialog = new DeleteConfirmationDialog();
            dialog.Owner = _window;
            return dialog.ShowDialog().Value;
        }

        public void AppendText()
        {
            string textToAppend = ShowAppendTextDialog();
            if (textToAppend.IsNullOrEmpty())
            {
                return;
            }

            ModifySelectedTasks(AppendTaskText, textToAppend);
        }

        private Task AppendTaskText(Task task, dynamic text = null)
        {
            Task newTask = new Task(string.Concat(task.Raw, " ", text));
            return newTask;
        }

        private string ShowAppendTextDialog()
        {
            if (!AreTasksSelected())
            {
                return "";
            }

            var dialog = new AppendTextDialog(this);
            dialog.Owner = _window;
            if (dialog.ShowDialog().Value)
            {
                return dialog.TextToAppend.Trim();
            }

            return "";
        }

        public void ToggleCompletion()
        {
            ModifySelectedTasks(SetTaskCompletion, null);
            if (User.Default.AutoArchive)
            {
                ArchiveCompleted();
            }
        }

        private Task SetTaskCompletion(Task task, dynamic parameter = null)
        {
            if (!task.Completed && !string.IsNullOrEmpty(task.Recur))
            {
                var addTask = new Task(task.ToString());
                addTask.ApplyRecur();
                TaskList.Add(addTask);
            }

            task.Completed = !task.Completed;
            var newTask = new Task(task.ToString());
            return newTask;
        }

        public void SetPriority()
        {
            string newPriority = ShowPriorityDialog();
            if (String.IsNullOrEmpty(newPriority) || !Char.IsLetter((char)newPriority[0])) // reject bad input
            {
                return;
            }
            ModifySelectedTasks(SetTaskPriority, newPriority);
        }

        private Task SetTaskPriority(Task task, dynamic newPriority)
        {
            Regex rgx = new Regex(@"^\((?<priority>[A-Z])\)\s"); // matches priority strings such as "(A) " (including trailing space)

            string oldTaskRawText = task.ToString();
            string oldPriorityRaw = rgx.Match(oldTaskRawText).ToString(); // Priority letter plus parentheses and trailing space
            string oldPriority = rgx.Match(oldTaskRawText).Groups["priority"].Value.Trim(); // Priority letter 

            string newPriorityRaw = "(" + newPriority + ") ";
            string newTaskRawText = (String.IsNullOrEmpty(oldPriority)) ?
                newPriorityRaw + oldTaskRawText :            // prepend new priority
                rgx.Replace(oldTaskRawText, newPriorityRaw); // replace old priority (regex) with new priority (formatted)

            return new Task(newTaskRawText);
        }

        private string ShowPriorityDialog()
        {
            if (!AreTasksSelected())
            {
                return null;
            }

            // Get the default priority from the selected task to load into the Set Priority dialog
            Task selectedTask = (Task)_window.lbTasks.SelectedItem;
            string selectedTaskRawText = selectedTask.ToString();
            Regex rgx = new Regex(@"^\((?<priority>[A-Z])\)\s"); // matches priority strings such as "(A) " (including trailing space)
            string selectedPriorityRaw = rgx.Match(selectedTaskRawText).ToString(); // Priority letter plus parentheses and trailing space
            string selectedPriority = rgx.Match(selectedTaskRawText).Groups["priority"].Value.Trim(); // Priority letter 
            string defaultPriority = (String.IsNullOrEmpty(selectedPriority)) ? "A" : selectedPriority; // default for the priority dialog
            
            // Get priority from the Set Priority dialog,
            var dialog = new SetPriorityDialog(defaultPriority);
            dialog.Owner = _window;
            if (dialog.ShowDialog().Value)
            {
                return dialog.PriorityText;
            }
            return null;
        }

        public void IncreasePriority()
        {
            ModifySelectedTasks(IncreaseTaskPriority, null);
        }

        private Task IncreaseTaskPriority(Task task, dynamic newPriority = null)
        {
            Task newTask = new Task(task.Raw);
            newTask.IncPriority();
            return newTask;
        }

        public void DecreasePriority()
        {
            ModifySelectedTasks(DecreaseTaskPriority, null);
        }

        private Task DecreaseTaskPriority(Task task, dynamic newPriority = null)
        {
            Task newTask = new Task(task.Raw);
            newTask.DecPriority();
            return newTask;
        }

        public void RemovePriority()
        {
            ModifySelectedTasks(RemoveTaskPriority, null);
        }

        private Task RemoveTaskPriority(Task task, dynamic newPriority = null)
        {
            Task newTask = new Task(task.Raw);
            newTask.SetPriority(' ');
            return newTask;
        }

        public void IncrementDueDate()
        {
            ModifySelectedTasks(IncrementTaskDueDate, null);
        }

        private Task IncrementTaskDueDate(Task task, dynamic newDueDate = null)
        {
            return PostponeTask(task, new {Days = 1, DateType = "due" });
        }

        public void IncrementThresholdDate()
        {
            ModifySelectedTasks(IncrementTaskThresholdDate, null);
        }

        private Task IncrementTaskThresholdDate(Task task, dynamic newDueDate = null)
        {
            return PostponeTask(task, new { Days = 1, DateType = "t" });
        }

        public void DecrementDueDate()
        {
            ModifySelectedTasks(DecrementTaskDueDate, null);
        }

        private Task DecrementTaskDueDate(Task task, dynamic newDueDate = null)
        {
            return PostponeTask(task, new { Days = -1, DateType = "due" });
        }
        public void DecrementThresholdDate()
        {
            ModifySelectedTasks(DecrementTaskThresholdDate, null);
        }

        private Task DecrementTaskThresholdDate(Task task, dynamic newDueDate = null)
        {
            return PostponeTask(task, new { Days = -1, DateType = "t" });
        }

        public void RemoveDueDate()
        {
            ModifySelectedTasks(RemoveTaskDueDate, null);
        }

        private Task RemoveTaskDueDate(Task task, dynamic newDueDate = null)
        {
            Regex rgx = new Regex(@"(?i:(^|\s)due:(\d{4})-(\d{2})-(\d{2}))*");
            Task newTask = new Task(rgx.Replace(task.Raw, "").TrimStart(' '));
            return newTask;
        }

        public void RemoveThresholdDate()
        {
            ModifySelectedTasks(RemoveTaskThresholdDate, null);
        }

        private Task RemoveTaskThresholdDate(Task task, dynamic newDueDate = null)
        {
            Regex rgx = new Regex(@"(?i:(^|\s)t:(\d{4})-(\d{2})-(\d{2}))*");
            Task newTask = new Task(rgx.Replace(task.Raw, "").TrimStart(' '));
            return newTask;
        }

        public void SetDueDate()
        {
            DateTime? newDueDate = ShowSetDueDateDialog();
            if (newDueDate == null) // reject bad input
            {
                return;
            }
            ModifySelectedTasks(SetTaskDueDate, newDueDate);
        }

        public void SetThresholdDate()
        {
            DateTime? newDueDate = ShowSetThresholdDateDialog();
            if (newDueDate == null) // reject bad input
            {
                return;
            }
            ModifySelectedTasks(SetTaskThresholdDate, newDueDate);
        }

        private DateTime? ShowSetDueDateDialog()
        {
            Regex rgx = new Regex(@"(?<=\sdue:)(?<date>(\d{4})-(\d{2})-(\d{2}))");
            return ShowDateDialog(rgx);
        }

        private DateTime? ShowSetThresholdDateDialog()
        {
            Regex rgx = new Regex(@"(?<=\st:)(?<date>(\d{4})-(\d{2})-(\d{2}))");
            return ShowDateDialog(rgx);
        }

        private DateTime? ShowDateDialog(Regex rgx)
        {
            if (!AreTasksSelected())
            {
                return null;
            }

            // Get the default due date to show in the Set Due Date dialog.
            Task lastSelectedTask = (Task)_window.lbTasks.SelectedItem;
            string oldTaskRawText = lastSelectedTask.ToString();
            string oldDueDateText = rgx.Match(oldTaskRawText).Groups["date"].Value.Trim();
            DateTime defaultDate = (String.IsNullOrEmpty(oldDueDateText)) ? DateTime.Today : DateTime.Parse(oldDueDateText);

            // Get the new due date from the Set Due Date dialog.
            var dialog = new SetDueDateDialog(defaultDate);
            dialog.Owner = _window;
            if (dialog.ShowDialog().Value)
            {
                return dialog.DueDatePicker.SelectedDate;
            }
            return null;
        }

        private Task SetTaskDueDate(Task task, dynamic newDueDate)
        {
            return SetTaskDate(task, newDueDate, "due");
        }
        private Task SetTaskThresholdDate(Task task, dynamic newDueDate)
        {
            return SetTaskDate(task, newDueDate, "t");
        }

        private Task SetTaskDate(Task task, dynamic newDueDate, string dateType)
        {
            Regex rgx = dateType == "due" ? new Regex(@"(?<=(^|\s)due:)(?<date>(\d{4})-(\d{2})-(\d{2}))") : new Regex(@"(?<=(^|\s)t:)(?<date>(\d{4})-(\d{2})-(\d{2}))");
            string oldDateText = rgx.Match(task.Raw).Groups["date"].Value.Trim();

            string oldTaskRawText = task.Raw;
            oldDateText = rgx.Match(oldTaskRawText).Groups["date"].Value.Trim();
            string newTaskRawText = (String.IsNullOrEmpty(oldDateText)) ?
                oldTaskRawText + " " + dateType + ":" + ((DateTime)newDueDate).ToString("yyyy-MM-dd") :
                rgx.Replace(oldTaskRawText, ((DateTime)newDueDate).ToString("yyyy-MM-dd"));

            return new Task(newTaskRawText);
        }

        public void Postpone()
        {
            int daysToPostpone = ShowPostponeDialog();
            if (daysToPostpone == 0)
            {
                return;
            }
            ModifySelectedTasks(PostponeTask, new { Days = daysToPostpone, DateType = "due" });
        }

        public void Threshold()
        {
            int daysToPostpone = ShowPostponeDialog();
            if (daysToPostpone == 0)
            {
                return;
            }
            ModifySelectedTasks(PostponeTask, new { Days = daysToPostpone, DateType = "t"});
        }
        
        private Task PostponeTask(Task task, dynamic daysToPostpone)
        {
            if (daysToPostpone.Days == 0) // if user entered 0 or junk
            {
                return task;
            }

            // Get due date of the selected task. 
            // If current item doesn't have a due date, use today as the due date.
            var oldDateTime = DateTime.Today;
            if (daysToPostpone.DateType == "due" && !String.IsNullOrEmpty(task.DueDate)) 
                oldDateTime = Convert.ToDateTime(task.DueDate);
            else if (daysToPostpone.DateType == "t" && !String.IsNullOrEmpty(task.ThresholdDate))
                oldDateTime = Convert.ToDateTime(task.ThresholdDate);          

            // Add days to that date to create the new due date.
            DateTime newDueDate = oldDateTime.AddDays(daysToPostpone.Days);

            // If the item has a due date, exchange the current due date with the new.
            // Else if the item does not have a due date, append the new due date to the task.
            string updatedRaw = task.Raw;
            if (daysToPostpone.DateType == "due")
            {
                updatedRaw = (task.DueDate.Length > 0) ?
                task.Raw.Replace("due:" + task.DueDate, "due:" + newDueDate.ToString("yyyy-MM-dd")) :
                task.Raw.ToString() + " due:" + newDueDate.ToString("yyyy-MM-dd");
            }
            else if (daysToPostpone.DateType == "t")
            {
                 updatedRaw = (task.ThresholdDate.Length > 0) ?
                task.Raw.Replace("t:" + task.ThresholdDate, "t:" + newDueDate.ToString("yyyy-MM-dd")) :
                task.Raw.ToString() + " t:" + newDueDate.ToString("yyyy-MM-dd");
            }

            return new Task(updatedRaw);
        }

        private int ShowPostponeDialog()
        {
            const string relativePattern =
                @"^(?<dateRelative>today|tomorrow|(?<weekday>mon(?:day)?|tue(?:sday)?|wed(?:nesday)?|thu(?:rsday)?|fri(?:day)?|sat(?:urday)?|sun(?:day)?))$";

            if (!AreTasksSelected())
            {
                return 0;
            }
            
            var dialog = new PostponeDialog();
            dialog.Owner = _window;

            int iDays = 0;

            if (dialog.ShowDialog().Value)
            {
                string sPostpone = dialog.PostponeText.Trim();

                // Lower case for the comparison
                sPostpone = sPostpone.ToLower();

                var reg = new Regex(relativePattern, RegexOptions.IgnoreCase);
                var regMatch = reg.Match(sPostpone);

                // Postpone to a day, not a number of days from now
                if (regMatch.Success)
                {
                    DateTime due = DateTime.Now;
                    var count = 0;
                    bool isValid = false;

                    // Set the current due date as today, otherwise if the task is overdue or in the future, the following count won't work correctly
                    ModifySelectedTasks(SetTaskDueDate, DateTime.Today);
                    
                    //if day of week, add days to today until weekday matches input
                    if (sPostpone == "today")
                    {
                        return 0;
                    }
                    else if (sPostpone == "tomorrow")
                    {
                        return 1;
                    }
                    //if today is the specified weekday, due date will be in one week
                    var lookingForShortDay = sPostpone.Substring(0, 3);
                    do
                    {
                        count++;
                        due = due.AddDays(1);
                        isValid = string.Equals(due.ToString("ddd", new CultureInfo("en-US")),
                                                lookingForShortDay,
                                                StringComparison.CurrentCultureIgnoreCase);
                    } while (!isValid && (count < 7));
                    // The count check is to prevent an endless loop in case of other culture.

                    return count;
                }

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
            Clipboard.SetDataObject(clipboardText.ToString());
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
            if (!File.Exists(User.Default.SelectedArchiveFilePath))
            {
                ShowOptionsDialog();
            }

            if (!File.Exists(User.Default.SelectedArchiveFilePath))
            {
                return;
            }

            GetSelectedTasks();
            
            DisableFileChangeObserver();

            var archiveList = new TaskList(User.Default.SelectedArchiveFilePath, User.Default.PreserveWhiteSpace);
            var completed = TaskList.Tasks.Where(t => t.Completed);
            
            // Make sure we are working with the latest version of the file.
            TaskList.ReloadTasks();

            foreach (var task in completed)
            {
                archiveList.Add(task);
                TaskList.Delete(task);
            }

            UpdateDisplayedTasks();

            SetSelectedTasks();
            
            EnableFileChangeObserver();
        }

        public void ShowOptionsDialog()
        {
            var o = new Options(FontInfo.GetControlFont(_window.lbTasks));
            o.Owner = _window;

            var autoRefreshOriginalSetting = User.Default.AutoRefresh;
            bool updateTaskListRequired = false;

            var res = o.ShowDialog();
            if (!res.Value) // User cancelled Options dialog
            {
                return;
            }

            // Update the task list display only if auto-refresh, filter case-sensitivity, grouping,
            // or font attributes are changed in the Options dialog.
            updateTaskListRequired = (
                User.Default.AutoRefresh != o.cbAutoRefresh.IsChecked.Value ||
                User.Default.FilterCaseSensitive != o.cbCaseSensitiveFilter.IsChecked.Value ||
                User.Default.AllowGrouping != o.cbAllowGrouping.IsChecked.Value ||
                User.Default.TaskListFontFamily != o.TaskListFont.Family.ToString() ||
                User.Default.TaskListFontSize != o.TaskListFont.Size ||
                User.Default.TaskListFontStyle != o.TaskListFont.Style.ToString() ||
                User.Default.TaskListFontWeight != o.TaskListFont.Weight.ToString() ||
                User.Default.TaskListFontStretch != o.TaskListFont.Stretch.ToString() ||
                User.Default.TaskListFontBrushColor != o.TaskListFont.BrushColor.ToString()
                );

            User.Default.ArchiveFilePath = o.tbArchiveFile.Text;
            User.Default.AutoArchive = o.cbAutoArchive.IsChecked.Value;
            User.Default.AutoSelectArchivePath = o.cbAutoSelectArchivePath.IsChecked.Value;
            User.Default.MoveFocusToTaskListAfterAddingNewTask = o.cbMoveFocusToTaskListAfterAddingNewTask.IsChecked.Value;
            User.Default.AutoRefresh = o.cbAutoRefresh.IsChecked.Value;
            User.Default.FilterCaseSensitive = o.cbCaseSensitiveFilter.IsChecked.Value;
            User.Default.IntellisenseCaseSensitive = o.cbIntellisenseCaseSensitive.IsChecked.Value;
            User.Default.AddCreationDate = o.cbAddCreationDate.IsChecked.Value;
            User.Default.DebugLoggingOn = o.cbDebugOn.IsChecked.Value;
            User.Default.MinimiseToSystemTray = o.cbMinToSysTray.IsChecked.Value;
            User.Default.MinimiseOnClose = o.cbMinOnClose.IsChecked.Value;
            User.Default.RequireCtrlEnter = o.cbRequireCtrlEnter.IsChecked.Value;
            User.Default.AllowGrouping = o.cbAllowGrouping.IsChecked.Value;
            User.Default.PreserveWhiteSpace = o.cbPreserveWhiteSpace.IsChecked.Value;
            if (TaskList != null)
            {
                TaskList.PreserveWhiteSpace = User.Default.PreserveWhiteSpace;
            }
            User.Default.WordWrap = o.cbWordWrap.IsChecked.Value;

            // Unfortunately, font classes are not serializable, so all the pieces are tracked instead.
            User.Default.TaskListFontFamily = o.TaskListFont.Family.ToString();
            User.Default.TaskListFontSize = o.TaskListFont.Size;
            User.Default.TaskListFontStyle = o.TaskListFont.Style.ToString();
            User.Default.TaskListFontWeight = o.TaskListFont.Weight.ToString();
            User.Default.TaskListFontStretch = o.TaskListFont.Stretch.ToString();
            User.Default.TaskListFontBrushColor = o.TaskListFont.BrushColor.ToString();

            User.Default.DisplayStatusBar = o.cbDisplayStatusBar.IsChecked.Value;
            User.Default.CheckForUpdates = o.cbCheckForUpdates.IsChecked.Value;

            User.Default.Save();

            Log.LogLevel = User.Default.DebugLoggingOn ? LogLevel.Debug : LogLevel.Error;

            if (User.Default.AutoRefresh != autoRefreshOriginalSetting && User.Default.AutoRefresh)
            {
                EnableFileChangeObserver();
            }
            else
            {
                DisableFileChangeObserver();
            }

            if (updateTaskListRequired)
            {
                _window.SetFont();
                GetSelectedTasks();
                UpdateDisplayedTasks();
                SetSelectedTasks();
            }

            _window.sbTaskSummary.Visibility = User.Default.DisplayStatusBar ? Visibility.Visible : Visibility.Collapsed;
            _window.sbTaskSummary.UpdateLayout();
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

        /// <summary>
        /// Adds a new task to the task list with text from the textbox.
        /// Adds creation date to task if "Add created date to new tasks" option is on.
        /// If "Move focus to task list after adding new task" option is on, sets selection to the new task in the task list,
        /// and focus to the newly added task.
        /// If "Move focus to task list after adding new task" option is off, ensures that tasks selected prior to adding the
        /// new one are still selected after the new one is added.
        /// </summary>
        private Task AddTaskFromTextbox()
        {
            string taskString = _window.taskText.Text;

            if (!User.Default.PreserveWhiteSpace)
            {
                taskString = _window.taskText.Text.Trim();
            }
           
            var taskDetail = taskString;
                
            if (!(taskDetail.Length > 0))
            {
                return null;
            }

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

            try
            {
                Task newTask = new Task(taskDetail);
                TaskList.Add(newTask);

                if (User.Default.MoveFocusToTaskListAfterAddingNewTask)
                {
                    _window.lbTasks.Focus();
                    _selectedTasks.Clear();
                    _selectedTasks.Add(newTask);
                    UpdateDisplayedTasks();
                    SetSelectedTasks();
                }
                else
                {
                    GetSelectedTasks();
                    UpdateDisplayedTasks();
                    SetSelectedTasks();
                    _window.taskText.Focus();
                }
                return newTask;
            }
            catch (TaskException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        /// <summary>
        /// Updates a task selected in the task list (_updating) with new text from the textbox.
        /// Sets selection to the edited task in the task list.
        /// </summary>
        private void UpdateTaskFromTextbox()
        {
            string taskString = _window.taskText.Text;

            if (!User.Default.PreserveWhiteSpace)
            {
                taskString = _window.taskText.Text.Trim();
            }

            Task newTask = new Task(taskString);

            _selectedTasks.Clear();
            _selectedTasks.Add(newTask);
            TaskList.Update(_updating, newTask);
            _updating = null;
            UpdateDisplayedTasks();
            SetSelectedTasks();
        }

        internal void TaskTextPreviewKeyUp(KeyEventArgs e)
        {
            if (TaskList == null)
            {
                MessageBox.Show("You don't have a todo.txt file open - please use File\\New or File\\Open",
                    "Please open a file", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
                _window.lbTasks.Focus();
                return;
            }

            if (ShouldAddTask(e))
            {
                Task addedTask = null;
                if (_updating == null) // Adding new tasks
                {
                    addedTask = AddTaskFromTextbox();
                }
                else // Updating existing tasks
                {
                    UpdateTaskFromTextbox();
                }

                //If holding shift keep the PrimaryContext and PrimaryProject in the Input box for quick data entry.
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && addedTask != null)
                {
                    var text = "";
                    if (!string.IsNullOrWhiteSpace(addedTask.PrimaryContext))
                    {
                        text = addedTask.PrimaryContext + " ";
                    }
                    if (!string.IsNullOrWhiteSpace(addedTask.PrimaryProject))
                    {
                        text += addedTask.PrimaryProject + " ";
                    }
                    _window.taskText.Text = text;
                    _window.taskText.CaretIndex = text.Length == 0 ? 0 : text.Length; //Set the cursor position to the end 
                }
                else
                {
                    _window.taskText.Text = "";
                }

                return;
            }

            switch (e.Key)
            {
                // In the task text box, the Escape key: 
                // 1. cancels updates
                // 2. clears the text box if it is not empty
                // 3. returns focus to the task list if the text box is empty
                case Key.Escape:
                    _updating = null;
                    if (_window.taskText.Text == "")
                    {
                        GetSelectedTasks();
                        SetSelectedTasks();
                    }
                    else
                    {
                        _window.taskText.Text = "";
                    }
                    break;
            }
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

        #region Utility Methods
        private void RaiseProperyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}