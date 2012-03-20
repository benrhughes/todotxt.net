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
using Microsoft.Win32;

namespace Client
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        public Options()
        {
            InitializeComponent();

            tbArchiveFile.Text = User.Default.ArchiveFilePath;
            cbAutoArchive.IsChecked = User.Default.AutoArchive;
            cbAutoRefresh.IsChecked = User.Default.AutoRefresh;
            cbCaseSensitiveFilter.IsChecked = User.Default.FilterCaseSensitive;
            cbAddCreationDate.IsChecked = User.Default.AddCreationDate;
            cbDebugOn.IsChecked = User.Default.DebugLoggingOn;
			cbMinToSysTray.IsChecked = User.Default.MinimiseToSystemTray;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";

            var res = dialog.ShowDialog();

            if (res.Value)
                tbArchiveFile.Text = dialog.FileName;
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
