using System.Text;
using System.Windows;
using System.Windows.Media;
using ToDoLib;

namespace ColorFont
{
    /// <summary>
    /// Interaction logic for ColorFontDialog.xaml
    /// </summary>
public partial class ColorFontDialog : Window
{
    private FontInfo selectedFont;

    public ColorFontDialog()
    {
        Log.Debug("Loading ColorFontDialog");
        this.selectedFont = null; // Default
        InitializeComponent();
    }

    public FontInfo Font
    {
        get
        {
            return this.selectedFont;
        }

        set
        {
            FontInfo fi = value;
            this.selectedFont = fi;
        }
    }

    private void SyncFontName()
    {
        Log.Debug("In SyncFontName");

        string fontFamilyName = this.selectedFont.Family.Source;
        bool isFontFound = false;

        int idx = 0;
        foreach (var item in this.colorFontChooser.lstFamily.Items)
        {
            string itemName = item.ToString();
            if (fontFamilyName == itemName)
            {
                isFontFound = true;                
                break;
            }
            idx++;
        }
        
        if (!isFontFound)
        {
            idx = 0;
        }

        this.colorFontChooser.lstFamily.SelectedIndex = idx;
        this.colorFontChooser.lstFamily.ScrollIntoView(this.colorFontChooser.lstFamily.Items[idx]);
    }

    private void SyncFontSize()
    {
        Log.Debug("In SyncFontSize");
        double fontSize = this.selectedFont.Size;
        this.colorFontChooser.fontSizeSlider.Value = fontSize;
    }

    private void SyncFontColor()
    {
        Log.Debug("In SyncFontColor");
        //int colorIdx = AvailableColors.GetFontColorIndex(this.Font.Color);
        //this.colorFontChooser.colorPicker.superCombo.SelectedIndex = colorIdx;
        // The following does not work. Why??? Now it does
        this.colorFontChooser.colorPicker.SelectedColor = this.Font.Color;
        this.colorFontChooser.colorPicker.superCombo.BringIntoView();
    }

    private void SyncFontTypeface()
    {
        Log.Debug("In SyncFontTypeface");
        string fontTypeFaceSb = FontInfo.TypefaceToString(this.selectedFont.Typeface);
        int idx = 0;
        foreach (var item in this.colorFontChooser.lstTypefaces.Items)
        {
            FamilyTypeface face = item as FamilyTypeface;
            if (fontTypeFaceSb == FontInfo.TypefaceToString(face))
            {
                break;
            }
            idx++;
        }
        this.colorFontChooser.lstTypefaces.SelectedIndex = idx;
    }

    private void btnOk_Click(object sender, RoutedEventArgs e)
    {
        this.Font = this.colorFontChooser.SelectedFont;
        this.DialogResult = true;
    }

    private void Window_Loaded_1(object sender, RoutedEventArgs e)
    {
        Log.Debug("In Window_Loaded_1" );
        this.SyncFontColor();
        this.SyncFontName();
        this.SyncFontSize();
        this.SyncFontTypeface();
        Log.Debug("Leaving Window_Loaded_1");
    }
}
}
