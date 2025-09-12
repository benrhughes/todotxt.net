using System.Windows.Media;

namespace ColorFont
{
    public class FontColor
    {
        public string Name { get; set; }
        public SolidColorBrush Brush { get; set; }

        public FontColor(string name, SolidColorBrush brush)
        {
            Name = name;
            Brush = brush;
        }

        public override bool Equals(System.Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            FontColor p = obj as FontColor;
            if ((System.Object)p == null)
            {
                return false;
            }

            return (this.Name == p.Name) && (this.Brush.Equals(p.Brush));
        }

        public bool Equals(FontColor p)
        {
            if ((object)p == null)
            {
                return false;
            }

            return (this.Name == p.Name) && (this.Brush.Equals(p.Brush));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "FontColor [Color=" + this.Name + ", " + this.Brush.ToString() + "]";
        }
    }
}
