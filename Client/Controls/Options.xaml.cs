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
            cbAutoSelectArchivePath.IsChecked = User.Default.AutoSelectArchivePath;
            cbAutoRefresh.IsChecked = User.Default.AutoRefresh;
            cbCaseSensitiveFilter.IsChecked = User.Default.FilterCaseSensitive;
            cbIntellisenseCaseSensitive.IsChecked = User.Default.IntellisenseCaseSensitive;
            cbAddCreationDate.IsChecked = User.Default.AddCreationDate;
            cbDebugOn.IsChecked = User.Default.DebugLoggingOn;
            cbMinToSysTray.IsChecked = User.Default.MinimiseToSystemTray;
            cbMinOnClose.IsChecked = User.Default.MinimiseOnClose;
            cbRequireCtrlEnter.IsChecked = User.Default.RequireCtrlEnter;
            cbAllowGrouping.IsChecked = User.Default.AllowGrouping;
            cbMoveFocusToTaskListAfterAddingNewTask.IsChecked = User.Default.MoveFocusToTaskListAfterAddingNewTask;
            cbPreserveWhiteSpace.IsChecked = User.Default.PreserveWhiteSpace;
            cbWordWrap.IsChecked = User.Default.WordWrap;
            this.TaskListFont = taskFont;
            this.cbDisplayStatusBar.IsChecked = User.Default.DisplayStatusBar;
            this.cbCheckForUpdates.IsChecked = User.Default.CheckForUpdates;

            var colors = new AvailableColors();
            cpProject.SelectedColor = colors.GetFontColorByName(User.Default.ProjectColor);
            cpProject.SelectedColor = colors.GetFontColorByName(User.Default.ProjectColor);
            cpContext.SelectedColor = colors.GetFontColorByName(User.Default.ContextColor);
            cpKeyValue.SelectedColor = colors.GetFontColorByName(User.Default.KeyValueColor);
            cpPriorityA.SelectedColor = colors.GetFontColorByName(User.Default.PriorityAColor);
            cpPriorityB.SelectedColor = colors.GetFontColorByName(User.Default.PriorityBColor);
            cpPriorityC.SelectedColor = colors.GetFontColorByName(User.Default.PriorityCColor);
            
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
                    this.currentFontDisplay.Text = string.Format("{0}, {1}-{2}-{3}, {4}",
                            this.taskListFont.Family,
                            this.taskListFont.Style,
                            this.taskListFont.Weight,
                            this.taskListFont.Stretch,
                            this.taskListFont.Size
                        );
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
