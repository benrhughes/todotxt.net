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
        TaskList _taskList = new TaskList();

        public MainWindow()
        {
            InitializeComponent();

            taskList.ItemsSource = _taskList.Tasks;
        }

        private void sortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IEnumerable<Task> tasks;

            var item = (ComboBoxItem)e.AddedItems[0];

            var text = item.Content.ToString();
            switch (text)
            {
                case "Priority":
                    tasks = _taskList.Tasks.OrderBy(x => x.Priority);
                    break;
                case "Project":
                    tasks = _taskList.Tasks.OrderBy(x => x.Project);
                    break;
                case "Context":
                    tasks = _taskList.Tasks.OrderBy(x => x.Context);
                    break;
                default:
                    tasks = _taskList.Tasks;
                    break;
            }

            taskList.ItemsSource = tasks;
        }
    }
}
