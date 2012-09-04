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
using ColorFont;

namespace Client
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        public Options(FontInfo taskFont)
        {
            InitializeComponent();

            tbArchiveFile.Text = User.Default.ArchiveFilePath;
            cbAutoArchive.IsChecked = User.Default.AutoArchive;
            cbAutoRefresh.IsChecked = User.Default.AutoRefresh;
            cbCaseSensitiveFilter.IsChecked = User.Default.FilterCaseSensitive;
            cbAddCreationDate.IsChecked = User.Default.AddCreationDate;
            cbDebugOn.IsChecked = User.Default.DebugLoggingOn;
            cbMinToSysTray.IsChecked = User.Default.MinimiseToSystemTray;
            cbRequireCtrlEnter.IsChecked = User.Default.RequireCtrlEnter;
            this.TaskListFont = taskFont;
        }

        private FontInfo taskListFont;
        
        /// <summary>
        /// The font specifically used by the task list.
        /// </summary>
        public FontInfo TaskListFont 
        {
            get
            {
                return this.taskListFont;
            }

            private set
            {
                this.taskListFont = value;
                if (this.taskListFont != null)
                {
                    this.currentFontDisplay.Text = this.taskListFont.Family.ToString() + ", " + this.taskListFont.Style.ToString() + ", " + this.taskListFont.Size.ToString();
                }
            }
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

        /// <summary>
        /// Opens the Font dialog.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private void selectFonts_Click(object sender, RoutedEventArgs e)
        {
            var taskFontDialog = new ColorFontDialog();

            taskFontDialog.Font = this.TaskListFont;

            var fontResult = taskFontDialog.ShowDialog();

            if (fontResult == true)
            {
                this.TaskListFont = taskFontDialog.Font;
            }

        }
    }
}
