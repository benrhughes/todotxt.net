using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for SetPriorityDialog.xaml
    /// </summary>
    public partial class AppendTextDialog : Window
    {
        private MainWindowViewModel _parentWindowViewModel;
        
        public string TextToAppend
        {
            get { return this.tbTextToAppend.Text.Trim(); }
            set { this.tbTextToAppend.Text = (String.IsNullOrEmpty(value)) ? "" : value; }
        }

        public AppendTextDialog(MainWindowViewModel parentWindowViewModel) : this()
        {
            _parentWindowViewModel = parentWindowViewModel;
            this.DataContext = _parentWindowViewModel;
        }

        public AppendTextDialog()
        {
            InitializeComponent();
            this.tbTextToAppend.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
