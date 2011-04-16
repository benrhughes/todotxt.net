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

        Dictionary<Sort, Func<IEnumerable<Task>, IEnumerable<Task>> > SortActions;

        Sort CurrentSort;

        public MainWindow()
        {
            InitializeComponent();

            this.Height = User.Default.WindowHeight;
            this.Width = User.Default.WindowWidth;

            SortActions = new Dictionary<Sort, Func<IEnumerable<Task>, IEnumerable<Task>>>()
            {
                {Sort.Completed, x => x.OrderBy(t => t.Completed)} ,
                {Sort.Context, x => x.OrderBy(t => string.IsNullOrEmpty(t.Context) ? "zzz" : t.Context.Substring(1))}, //ignore the @
                {Sort.Priority, x => x.OrderBy(t => t.Priority)},
                {Sort.Project, x => x.OrderBy(t => string.IsNullOrEmpty(t.Project) ? "zzz" : t.Project.Substring(1))}, //ignore the +
                {Sort.None, x => x}
            };

            if (!string.IsNullOrEmpty(User.Default.FilePath))
                TryOpen(User.Default.FilePath);


            KeyboardShortcuts = new Dictionary<Key, Action>()
            {
                {Key.O, () => File_Open(null, null)},
                {Key.N, () => taskText.Focus()},
                {Key.J, () => lbTasks.SelectedIndex = lbTasks.SelectedIndex < lbTasks.Items.Count ? lbTasks.SelectedIndex + 1 : lbTasks.SelectedIndex},
                {Key.K, () => lbTasks.SelectedIndex = lbTasks.SelectedIndex > 0 ? lbTasks.SelectedIndex - 1 : 0},
                {Key.OemPeriod, () => 
                    {
                        _taskList.ReloadTasks(); 
                        lbTasks.ItemsSource = SortActions[CurrentSort](_taskList.Tasks);
                    }},
                {Key.X, () => 
                    {
                        _taskList.ToggleComplete((Task)lbTasks.SelectedItem);
                        lbTasks.ItemsSource = SortActions[CurrentSort](_taskList.Tasks);
                        lbTasks.SelectedItem = lbTasks.Items[0];
                    }},
                {Key.D, () => 
                    {
                        var res = MessageBox.Show("Permanently delete the selected task?", 
                                    "Confirm Delete",
                                    MessageBoxButton.YesNo);

                        if (res == MessageBoxResult.Yes)
                        {
                            _taskList.Delete((Task)lbTasks.SelectedItem);
                            lbTasks.ItemsSource = SortActions[CurrentSort](_taskList.Tasks);
                            lbTasks.SelectedItem = lbTasks.Items[0];
                        }
                    }}
            };

            SetSort((Sort)User.Default.CurrentSort);
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

        void SetSort(Sort sort)
        {
            User.Default.CurrentSort = (int)sort;
            User.Default.Save();

            CurrentSort = sort;

            if (_taskList != null)
            {
                lbTasks.ItemsSource = SortActions[CurrentSort](_taskList.Tasks);
                lbTasks.SelectedItem = lbTasks.Items[0];
            }
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
                _taskList.Add(new Task(tb.Text.Trim()));
                tb.Text = "";
                lbTasks.ItemsSource = SortActions[CurrentSort](_taskList.Tasks);
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

    }
}

