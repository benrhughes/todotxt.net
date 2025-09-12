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
        private MainWindowViewModel _parentWindowViewModel;

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

        public string FilterTextPreset4
        {
            get { return tbFilterPreset4.Text; }
            set { tbFilterPreset4.Text = value; tbFilterPreset4.CaretIndex = tbFilterPreset4.Text.Length; }
        }

        public string FilterTextPreset5
        {
            get { return tbFilterPreset5.Text; }
            set { tbFilterPreset5.Text = value; tbFilterPreset5.CaretIndex = tbFilterPreset5.Text.Length; }
        }

        public string FilterTextPreset6
        {
            get { return tbFilterPreset6.Text; }
            set { tbFilterPreset6.Text = value; tbFilterPreset6.CaretIndex = tbFilterPreset6.Text.Length; }
        }

        public string FilterTextPreset7
        {
            get { return tbFilterPreset7.Text; }
            set { tbFilterPreset7.Text = value; tbFilterPreset7.CaretIndex = tbFilterPreset7.Text.Length; }
        }

        public string FilterTextPreset8
        {
            get { return tbFilterPreset8.Text; }
            set { tbFilterPreset8.Text = value; tbFilterPreset8.CaretIndex = tbFilterPreset8.Text.Length; }
        }

        public string FilterTextPreset9
        {
            get { return tbFilterPreset9.Text; }
            set { tbFilterPreset9.Text = value; tbFilterPreset9.CaretIndex = tbFilterPreset9.Text.Length; }
        }

        public FilterDialog(MainWindowViewModel parentWindowViewModel) : this()
        {
            _parentWindowViewModel = parentWindowViewModel;
            this.DataContext = _parentWindowViewModel;
        }

        public FilterDialog()
        {
            InitializeComponent();
            tbFilter.Focus();
            _parentWindowViewModel = null;
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
            tbFilterPreset4.Clear();
            tbFilterPreset5.Clear();
            tbFilterPreset6.Clear();
            tbFilterPreset7.Clear();
            tbFilterPreset8.Clear();
            tbFilterPreset9.Clear();
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
