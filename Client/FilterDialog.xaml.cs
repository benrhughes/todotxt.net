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
    /// Interaction logic for FilterDialog.xaml
    /// </summary>
    public partial class FilterDialog : Window
    {
		private bool _prevKeyWasEnter;
        public string FilterText
        {
            get { return tbFilter.Text; }
			set { tbFilter.Text = value; tbFilter.CaretIndex = tbFilter.Text.Length; }
        }

        public FilterDialog()
        {
            InitializeComponent();
            tbFilter.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            tbFilter.Clear();
        }

        private void tbFilter_PreviewKeyUp(object sender, KeyEventArgs e)
        {
			if (e.Key == Key.Escape)
				this.DialogResult = true;
			else if (e.Key == Key.Enter && _prevKeyWasEnter)
				this.DialogResult = true;
			else if (e.Key == Key.Enter)
				_prevKeyWasEnter = true;
			else
				_prevKeyWasEnter = false;
        }
    }
}
