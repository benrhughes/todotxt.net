using System.Windows;
using System.Windows.Input;

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

        public string FilterTextPreset1
        {
            get { return tbFilterPreset1.Text; }
            set { tbFilterPreset1.Text = value; tbFilterPreset1.CaretIndex = tbFilterPreset1.Text.Length; }
        }

        public string FilterTextPreset2
        {
            get { return tbFilterPreset2.Text; }
            set { tbFilterPreset2.Text = value; tbFilterPreset2.CaretIndex = tbFilterPreset2.Text.Length; }
        }

        public string FilterTextPreset3
        {
            get { return tbFilterPreset3.Text; }
            set { tbFilterPreset3.Text = value; tbFilterPreset3.CaretIndex = tbFilterPreset3.Text.Length; }
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

        private void Clear_All_Click(object sender, RoutedEventArgs e)
        {
            tbFilter.Clear();
            tbFilterPreset1.Clear();
            tbFilterPreset2.Clear();
            tbFilterPreset3.Clear();
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
