using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for DeleteConfirmationDialog.xaml
    /// </summary>
    public partial class DeleteConfirmationDialog : Window
    {
        public DeleteConfirmationDialog()
        {
            InitializeComponent();
            this.img.Source = Imaging.CreateBitmapSourceFromHIcon(
                                SystemIcons.Warning.Handle,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());

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
