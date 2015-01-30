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
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for PostponeDialog.xaml
    /// </summary>
    public partial class PostponeDialog : Window
    {
        public string PostponeText
        {
            get { return tbPostpone.Text; }
			set { this.tbPostpone.Text = value; tbPostpone.CaretIndex = tbPostpone.Text.Length; }
        }
        
        public PostponeDialog()
        {
            InitializeComponent();
            this.tbPostpone.Focus();
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
