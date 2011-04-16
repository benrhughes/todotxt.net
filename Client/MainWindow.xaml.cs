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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TaskList _taskList = new TaskList(@"..\..\..\ToDoTests\testtasks.txt");

        Dictionary<Key, Action> KeyboardShortcuts;

        Func<IEnumerable<Task>, IEnumerable<Task>> CurrentSort;

        public MainWindow()
        {
            InitializeComponent();

            CurrentSort = x => x.OrderBy(t => t.Completed);
            lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);

            KeyboardShortcuts = new Dictionary<Key, Action>()
            {
                {Key.N, () => taskText.Focus()},
                {Key.J, () => lbTasks.SelectedIndex = lbTasks.SelectedIndex < lbTasks.Items.Count ? lbTasks.SelectedIndex + 1 : lbTasks.SelectedIndex},
                {Key.K, () => lbTasks.SelectedIndex = lbTasks.SelectedIndex > 0 ? lbTasks.SelectedIndex - 1 : 0},
                {Key.X, () => 
                    {
                        _taskList.ToggleComplete((Task)lbTasks.SelectedItem);
                        lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);
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
                            lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);
                            lbTasks.SelectedItem = lbTasks.Items[0];
                        }
                    }}
            };

            taskText.Focus();
        }


        private void Sort_Priority(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x.OrderBy(t => t.Priority);
            lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);

            SetSelected((MenuItem)sender);
        }

        private void Sort_None(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x;
            lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);

            SetSelected((MenuItem)sender);
        }

        private void Sort_Context(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x.OrderBy(t => string.IsNullOrEmpty(t.Context) ? "zzz" : t.Context.Substring(1)); //ignore the @
            lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Completed(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x.OrderBy(t => t.Completed);
            lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);

            SetSelected((MenuItem)sender);
        }

        private void Sort_Project(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x.OrderBy(t => string.IsNullOrEmpty(t.Project) ? "zzz" : t.Project.Substring(1)); //ignore the +
            lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);
            SetSelected((MenuItem)sender);
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
                lbTasks.ItemsSource = CurrentSort(_taskList.Tasks);
            }
        }

        private void taskList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (KeyboardShortcuts.ContainsKey(e.Key))
                KeyboardShortcuts[e.Key]();
        }

        

    }
}
