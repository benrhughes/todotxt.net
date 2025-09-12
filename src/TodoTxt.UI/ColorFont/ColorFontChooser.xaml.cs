using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ColorFont
{
    public partial class ColorFontChooser : UserControl
    {
        public ColorFontChooser()
        {
            InitializeComponent();
            this.txtSampleText.IsReadOnly = true;
        }

        public FontInfo SelectedFont
        {
            get
            {
                return new FontInfo(this.txtSampleText.FontFamily,
                                    this.txtSampleText.FontSize,
                                    this.txtSampleText.FontStyle,
                                    this.txtSampleText.FontStretch,
                                    this.txtSampleText.FontWeight,
                                    this.colorPicker.SelectedColor.Brush);
            }

        }

        private void colorPicker_ColorChanged(object sender, RoutedEventArgs e)
        {
            this.txtSampleText.Foreground = this.colorPicker.SelectedColor.Brush;
        }
    }
}
