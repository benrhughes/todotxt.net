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
		private bool _prevKeyWasEnter;
        public string PostponeText
        {
            get { return tbPostpone.Text; }
			set { tbPostpone.Text = value; tbPostpone.CaretIndex = tbPostpone.Text.Length; }
        }

        
        public PostponeDialog()
        {
            InitializeComponent();
            tbPostpone.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }


        private void tbPostpone_PreviewKeyUp(object sender, KeyEventArgs e)
        {
			if (e.Key == Key.Escape)
				this.DialogResult = false;
			else if (e.Key == Key.Enter && _prevKeyWasEnter)
				this.DialogResult = true;
			else if (e.Key == Key.Enter)
				_prevKeyWasEnter = true;
			else
				_prevKeyWasEnter = false;
        }

       /* private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }*/

    }
}
