using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ToDoLib;

namespace Client
{
    /// <summary>
    /// This class represents a text box with Intellisense popup behavior built in, for autocompletion 
    /// of projects and contextsfrom the MainWindowViewModel's task list.
    /// </summary>
    public class IntellisenseTextBox : TextBox
    {
        #region Properties

        private Popup IntellisensePopup { get; set; }
        private ListBox IntellisenseList { get; set; }
        private int IntelliPos { get; set; } // used to position the Intellisense popup

        public TaskList TaskList
        {
            get
            {
                return (TaskList)GetValue(TaskListProperty);
            }
            set
            {
                SetValue(TaskListProperty, value);
            }
        }
        public static readonly DependencyProperty TaskListProperty =
            DependencyProperty.Register("TaskList", typeof(TaskList), typeof(IntellisenseTextBox), new UIPropertyMetadata(null));

        public bool CaseSensitive
        {
            get
            {
                return (bool)GetValue(CaseSensitiveProperty);
            }
            set
            {
                SetValue(CaseSensitiveProperty, value);
            }
        }
        public static readonly DependencyProperty CaseSensitiveProperty =
            DependencyProperty.Register("CaseSensitive", typeof(bool), typeof(IntellisenseTextBox), new UIPropertyMetadata(false));

        #endregion

        #region Constructor

        public IntellisenseTextBox()
        {
            // Set up the Intellisense list, which will be contained within the Intellisense popup 
            // and handles most of the Intellisense behaviors.
            this.IntellisenseList = new ListBox();
            this.IntellisenseList.IsTextSearchEnabled = true;
            this.IntellisenseList.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            this.IntellisenseList.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            this.IntellisenseList.PreviewKeyUp += new KeyEventHandler(Intellisense_PreviewKeyUp);
            this.IntellisenseList.MouseUp += new MouseButtonEventHandler(Intellisense_MouseUp);

            // Set up the Intellisense popup.
            this.IntellisensePopup = new Popup();
            this.IntellisensePopup.IsOpen = false;
            this.IntellisensePopup.Height = Double.NaN; // auto
            this.IntellisensePopup.MinWidth = 150;
            this.IntellisensePopup.MaxWidth = 500;
            this.IntellisensePopup.StaysOpen = false;
            this.IntellisensePopup.Placement = PlacementMode.Bottom;
            this.IntellisensePopup.PlacementTarget = this;
            this.IntellisensePopup.Child = IntellisenseList;

            // Set up an event handler on the text box to trigger Intellisense.
            this.TextChanged += new TextChangedEventHandler(IntellisenseTextBox_TextChanged);
        }
		
	    #endregion

        #region Intellisense Popup-related Methods

        /// <summary>
        /// Show the Intellisense popup.
        /// </summary>
        /// <param name="s">Source for the Intellisense list.</param>
        /// <param name="placement">This value should be set to the cursor position in the text box.</param>
        public void ShowIntellisensePopup(IEnumerable<string> s, Rect placement)
        {
            if (s == null || s.Count() == 0)
            {
                return;
            }

            this.IntellisensePopup.PlacementRectangle = placement;
            this.IntellisenseList.ItemsSource = s;
            this.IntellisenseList.SelectedItem = null;
            this.IntellisensePopup.IsOpen = true;

            this.Focus();
        }

        /// <summary>
        /// Hide the Intellisense popup.
        /// </summary>
        public void HideIntellisensePopup()
        {
            this.IntellisensePopup.IsOpen = false;
        }

        /// <summary>
        /// Insert the selected Intellisense text into the textbox.
        /// Called in Intellisense_PreviewKeyUp and Intellisense_MouseUp methods.
        /// </summary>
        private void InsertIntellisenseText()
        {
            HideIntellisensePopup();

            if (this.IntellisenseList.SelectedItem == null)
            {
                this.Focus();
                return;
            }

            this.Text = this.Text.Remove(this.IntelliPos, this.CaretIndex - this.IntelliPos);

            var newText = this.IntellisenseList.SelectedItem.ToString();
            this.Text = this.Text.Insert(this.IntelliPos, newText);
            this.CaretIndex = this.IntelliPos + newText.Length;

            this.Focus();
        }

        /// <summary>
        /// Tab, Enter and Space keys will all added the selected text into the task string.
        /// Escape key cancels out.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">The key to trigger on.</param>
        public virtual void Intellisense_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Tab:
                case Key.Space:
                    InsertIntellisenseText();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    HideIntellisensePopup();
                    this.CaretIndex = this.Text.Length;
                    this.Focus();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Allow the user to click on an entry in the Intellisense list and insert that entry into the text box.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        public virtual void Intellisense_MouseUp(object sender, MouseButtonEventArgs e)
        {
            InsertIntellisenseText();
        }

        #endregion

        #region TextBox Event Handler Overrides

        /// <summary>
        /// Ensure that tabbing away from the text box hides the Intellisense popup before focus is lost.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Tab)
            {
                HideIntellisensePopup();
                e.Handled = false;
            }
        }

        /// <summary>
        /// Handle key events in the textbox that impact the Intellisense list and popup.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            if (this.IntellisensePopup.IsOpen && !this.IntellisenseList.IsFocused)
            {
                if (this.CaretIndex <= this.IntelliPos) // we've moved behind the symbol, drop out of intellisense
                {
                    HideIntellisensePopup();
                    e.Handled = false; // allow key event to be passed to TextBox
                    return;
                }

                switch (e.Key)
                {
                    case Key.Down:
                        if (this.IntellisenseList.Items.Count != 0)
                        {
                            this.IntellisenseList.SelectedIndex = 0; 
                            var listBoxItem = (ListBoxItem)this.IntellisenseList.ItemContainerGenerator.ContainerFromItem(
                                this.IntellisenseList.SelectedItem);
                            listBoxItem.Focus();
                        }
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        HideIntellisensePopup();
                        e.Handled = true;
                        break;
                    case Key.Space:
                    case Key.Enter:
                        HideIntellisensePopup();
                        e.Handled = false; // allow key event to be passed to TextBox
                        break;
                    default:
                        var word = FindIntelliWord();
                        if (this.CaseSensitive)
                        {
                            this.IntellisenseList.Items.Filter = (o) => o.ToString().Contains(word);
                        }
                        else
                        {
                            this.IntellisenseList.Items.Filter = (o) => (o.ToString().IndexOf(word, StringComparison.CurrentCultureIgnoreCase) >= 0);
                        }
                        e.Handled = true;
                        break;
                }
            }
        }

        private string FindIntelliWord()
        {
            return this.Text.Substring(this.IntelliPos + 1, this.CaretIndex - this.IntelliPos - 1);
        }

        /// <summary>
        /// Triggers the Intellisense popup to appear when "+" or "@" is pressed in the text box.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Event arguments</param>
        private void IntellisenseTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.Changes.Count != 1 || e.Changes.First().AddedLength < 1 || this.CaretIndex < 1)
            {
                return;
            }
			
            if (this.TaskList == null)
            {
                return;
            }

            var lastAddedCharacter = this.Text.Substring(this.CaretIndex - 1, 1);
            switch (lastAddedCharacter)
            {
                case "+":
                    this.IntelliPos = this.CaretIndex - 1;
                    ShowIntellisensePopup(this.TaskList.Projects, this.GetRectFromCharacterIndex(this.IntelliPos));
                    break;

                case "@":
                    this.IntelliPos = this.CaretIndex - 1;
                    ShowIntellisensePopup(this.TaskList.Contexts, this.GetRectFromCharacterIndex(this.IntelliPos));
                    break;
            }
        }

        #endregion
    }
}
