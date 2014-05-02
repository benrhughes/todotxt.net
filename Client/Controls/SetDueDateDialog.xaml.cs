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
    /// Interaction logic for SetDueDateDialog.xaml
    /// </summary>
    public partial class SetDueDateDialog : Window
    {
		private bool _prevKeyWasEnter;

        public DateTime DueDateText
        {
            get { return (DateTime)DueDatePicker.SelectedDate; }
			set { DueDatePicker.SelectedDate = value; }
        }

        public SetDueDateDialog(DateTime? defaultDate)
        {
            InitializeComponent();
            DueDatePicker.SelectedDate = (defaultDate == null) ? DateTime.Today : defaultDate;
            DueDatePicker.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void DueDatePicker_Keydown(object sender, KeyEventArgs e)
        {
            // This event handler is necessary because the XAML KeyBindings don't work for 
            // Enter, Up, Down, and Esc don't work for the DueDatePicker control.
            
            var dp = sender as DatePicker;
            if (dp == null) return;

            // Keyboard shortcut "T" for todays date.
            if (e.Key == Key.T)
            {
                e.Handled = true;
                dp.SetValue(DatePicker.SelectedDateProperty, DateTime.Today);
                return;
            }

            if (!dp.SelectedDate.HasValue) return;

            // "Up" key increases date by 1 day.
            var date = dp.SelectedDate.Value;
            if (e.Key == Key.Up)
            {
                e.Handled = true;
                dp.SetValue(DatePicker.SelectedDateProperty, date.AddDays(1));
                return;
            }

            // "Down" key increases date by 1 day.
            if (e.Key == Key.Down)
            {
                e.Handled = true;
                dp.SetValue(DatePicker.SelectedDateProperty, date.AddDays(-1));
                return;
            }
            
            // "Enter" key accepts value.
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                this.OK_Click(sender, e);
                return;
            }

            // "Escape" key cancels the change.            
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.Cancel_Click(sender, e);
                return;
            }
        }
    }
}
