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

        Func<IEnumerable<Task>, IEnumerable<Task>> CurrentSort;

        public MainWindow()
        {
            InitializeComponent();

            CurrentSort = x => x;
            taskList.ItemsSource = CurrentSort(_taskList.Tasks);

            taskText.Focus();
        }


        private void Sort_Priority(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x.OrderBy(t => t.Priority);
            taskList.ItemsSource = CurrentSort(_taskList.Tasks);

            SetSelected((MenuItem)sender);
        }

        private void Sort_None(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x;
            taskList.ItemsSource = CurrentSort(_taskList.Tasks);

            SetSelected((MenuItem)sender);
        }

        private void Sort_Context(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x.OrderBy(t => string.IsNullOrEmpty(t.Context) ? "zzz" : t.Context.Substring(1)); //ignore the @
            taskList.ItemsSource = CurrentSort(_taskList.Tasks);
            SetSelected((MenuItem)sender);
        }

        private void Sort_Project(object sender, RoutedEventArgs e)
        {
            CurrentSort = x => x.OrderBy(t => string.IsNullOrEmpty(t.Project) ? "zzz" : t.Project.Substring(1)); //ignore the +
            taskList.ItemsSource = CurrentSort(_taskList.Tasks);
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
                taskList.ItemsSource = CurrentSort(_taskList.Tasks);
            }
        }

    }
}
