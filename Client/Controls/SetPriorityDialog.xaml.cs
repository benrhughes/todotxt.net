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
    public partial class SetPriorityDialog : Window
    {
        public string PriorityText
        {
            get { return PriorityTextBox.Text.ToUpper(); }
            set { PriorityTextBox.Text = (String.IsNullOrEmpty(value)) ? "" : value; }
        }

        public SetPriorityDialog(string defaultPriority)
        {
            InitializeComponent();
            PriorityText = defaultPriority;
            PriorityTextBox.Focus();
            PriorityTextBox.SelectAll();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void PriorityTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            // "Up" key increases priority.
            if (e.Key == Key.Up)
            {
                e.Handled = true;
                IncreasePriorityExecuted(sender, e);
                return;
            }

            // "Down" key decreases priority.
            if (e.Key == Key.Down)
            {
                e.Handled = true;
                DecreasePriorityExecuted(sender, e);
                return;
            }

            // "Enter" key accepts value.
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                OK_Click(sender, e);
                return;
            }

            // "Escape" key cancels the change.            
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Cancel_Click(sender, e);
                return;
            }
        }

        private void IncreasePriorityExecuted(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(this.PriorityText))
            {
                this.PriorityText = "A";
                this.PriorityTextBox.SelectAll();
                return;
            }

            Regex rgx = new Regex("[A-Z]");
            if (rgx.IsMatch(this.PriorityText) && this.PriorityText[0] != 'A')
            {
                char newPriority = (char)((int)this.PriorityText[0] - 1);
                this.PriorityText = newPriority.ToString();
                this.PriorityTextBox.SelectAll();
            }

        }

        private void DecreasePriorityExecuted(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(this.PriorityText))
            {
                this.PriorityText = "A";
                this.PriorityTextBox.SelectAll();
                return;
            }

            Regex rgx = new Regex("[A-Z]");
            if (rgx.IsMatch(this.PriorityText) && this.PriorityText[0] != 'Z')
            {
                char newPriority = (char)((int)this.PriorityText[0] + 1);
                this.PriorityText = newPriority.ToString();
                this.PriorityTextBox.SelectAll();
            }
        }
    }
}
