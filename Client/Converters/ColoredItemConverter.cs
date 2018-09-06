using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ToDoLib;

namespace Client.Converters
{
    public class ColoredItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var textBlock = new TextBlock();

            if (!(value is Task task) || task.Completed)
            {
                if (value != null) textBlock.Text = value.ToString();
                return textBlock;
            }

            if (!string.IsNullOrWhiteSpace(task.Priority))
            {
                if (task.Priority.Equals("(A)", StringComparison.CurrentCultureIgnoreCase))
                {
                    textBlock.Inlines.Add(CreateColoredTextBlock(task.Priority, Colors.Red));
                }
                else if (task.Priority.Equals("(B)", StringComparison.CurrentCultureIgnoreCase))
                {
                    textBlock.Inlines.Add(CreateColoredTextBlock(task.Priority, Colors.Chocolate));
                }
                else if (task.Priority.Equals("(C)", StringComparison.CurrentCultureIgnoreCase))
                {
                    textBlock.Inlines.Add(CreateColoredTextBlock(task.Priority, Colors.DodgerBlue));
                }
                else
                {
                    textBlock.Inlines.Add(task.Priority);
                }
            }

            if (!string.IsNullOrWhiteSpace(task.CreationDate))
            {
                if (textBlock.Inlines.Any())
                {
                    textBlock.Inlines.Add(" ");
                }
                textBlock.Inlines.Add(task.CreationDate);
            }

            if (textBlock.Inlines.Any())
            {
                textBlock.Inlines.Add(" ");
            }
            textBlock.Inlines.Add(task.Body);

            if (!string.IsNullOrWhiteSpace(task.DueDate))
            {
                textBlock.Inlines.Add(CreateColoredTextBlock($" due:{task.DueDate}", Colors.Chocolate));
            }

            if (task.Projects.Any())
            {
                textBlock.Inlines.Add(" ");
                textBlock.Inlines.Add(CreateColoredTextBlock(string.Join(" ",task.Projects), Colors.Brown));
            }

            if (task.Contexts.Any())
            {
                textBlock.Inlines.Add(" ");
                textBlock.Inlines.Add(CreateColoredTextBlock(string.Join(" ",task.Contexts), Colors.Green));
            }

            return textBlock;
        }

        private static TextBlock CreateColoredTextBlock(string text, Color color)
        {
            var wordTextBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color)
            };
            return wordTextBlock;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
