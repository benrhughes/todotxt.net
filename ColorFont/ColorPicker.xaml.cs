using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ColorFont
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        private ColorPickerViewModel viewModel;

        public static readonly RoutedEvent ColorChangedEvent = EventManager.RegisterRoutedEvent(
            "ColorChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ColorPicker));

        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            "SelectedColor", typeof(FontColor), typeof(ColorPicker), new UIPropertyMetadata(null));

        public ColorPicker()
        {
            InitializeComponent();
            this.viewModel = new ColorPickerViewModel();
            this.DataContext = this.viewModel;
        }

        public event RoutedEventHandler ColorChanged
        {
            add { AddHandler(ColorChangedEvent, value); }
            remove { RemoveHandler(ColorChangedEvent, value); }
        }

        public FontColor SelectedColor
        {
            get 
            {
                FontColor fc = (FontColor)this.GetValue(SelectedColorProperty);
                if (fc == null)
                {
                    fc = AvailableColors.GetFontColor("Black");
                }
                return fc;
            }

            set 
            {
                this.viewModel.SelectedFontColor = value;
                SetValue(SelectedColorProperty, value);
            }
        }

        private void RaiseColorChangedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(ColorPicker.ColorChangedEvent);
            RaiseEvent(newEventArgs);
        }

        private void superCombo_DropDownClosed(object sender, EventArgs e)
        {
            this.SetValue(SelectedColorProperty, this.viewModel.SelectedFontColor);
            this.RaiseColorChangedEvent();
        }

        private void superCombo_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetValue(SelectedColorProperty, this.viewModel.SelectedFontColor);
        }

    }
}
