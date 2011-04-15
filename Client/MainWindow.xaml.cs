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

        public MainWindow()
        {
            InitializeComponent();

            taskList.ItemsSource = _taskList.Tasks;
        }


        private void Sort_Priority(object sender, RoutedEventArgs e)
        {
            taskList.ItemsSource = _taskList.Tasks.OrderBy(x => x.Priority);
            SetSelected((MenuItem)sender);
        }

        private void Sort_None(object sender, RoutedEventArgs e)
        {
            taskList.ItemsSource = _taskList.Tasks;
            SetSelected((MenuItem)sender);
        }

        private void Sort_Context(object sender, RoutedEventArgs e)
        {
            taskList.ItemsSource = _taskList.Tasks.OrderBy(x => string.IsNullOrEmpty(x.Context) ? "zzz" : x.Context.Substring(1)); //ignore the @
            SetSelected((MenuItem)sender);
        }

        private void Sort_Project(object sender, RoutedEventArgs e)
        {
            taskList.ItemsSource = _taskList.Tasks.OrderBy(x => string.IsNullOrEmpty(x.Project) ? "zzz" : x.Project.Substring(1)); //ignore the +
            SetSelected((MenuItem)sender);
        }

        void SetSelected(MenuItem item)
        {
            var sortMenu = (MenuItem)item.Parent;
            foreach (MenuItem i in sortMenu.Items)
                i.IsChecked = false;
            
            item.IsChecked = true;

        }
    }
}
