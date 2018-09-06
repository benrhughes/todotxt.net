using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace ColorFont
{
    class ColorPickerViewModel : INotifyPropertyChanged
    {
        private ReadOnlyCollection<FontColor> roFontColors;
        private FontColor selectedFontColor;

        public ColorPickerViewModel()
        {
            this.selectedFontColor = AvailableColors.GetFontColor(Colors.Black);
            this.roFontColors = new ReadOnlyCollection<FontColor>(new AvailableColors());
        }

        public ReadOnlyCollection<FontColor> FontColors
        {
            get { return this.roFontColors; }
        }

        public FontColor SelectedFontColor
        {
            get 
            { 
                return this.selectedFontColor; 
            }

            set
            {
                if (this.selectedFontColor == value) return;

                this.selectedFontColor = roFontColors.FirstOrDefault(c => c.Name == value.Name) ?? AvailableColors.GetFontColor(Colors.Black);
                OnPropertyChanged("SelectedFontColor");
            }
        }

        #region INotifyPropertyChanged Members

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
