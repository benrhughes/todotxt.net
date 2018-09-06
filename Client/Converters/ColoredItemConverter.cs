using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Client.Utilities;

namespace Client.Converters
{
    public class ColoredItemConverter : IValueConverter
    {
        private static readonly Dictionary<TaskTokenKind, SolidColorBrush> ColorTheme = new Dictionary<TaskTokenKind, SolidColorBrush>
        {
            { TaskTokenKind.Context, new SolidColorBrush(Colors.DarkGreen)},
            { TaskTokenKind.KeyValue, new SolidColorBrush(Colors.Chocolate)},
            { TaskTokenKind.PriorityA, new SolidColorBrush(Colors.Red)},
            { TaskTokenKind.PriorityB, new SolidColorBrush(Colors.Chocolate)},
            { TaskTokenKind.PriorityC, new SolidColorBrush(Colors.DodgerBlue)},
            { TaskTokenKind.Project, new SolidColorBrush(Colors.Brown)}
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var textBlock = new TextBlock();

            var raw = value.ToString();

            if (raw.StartsWith("x "))
            {
                textBlock.Text = raw;
                return textBlock;
            }

            var isNotFirst = false;
            foreach (var token in TaskParser.ParseIncompleteTask(raw))
            {
                var tokenTextBlock = new TextBlock
                {
                    Text = token.Value
                };
                if (ColorTheme.ContainsKey(token.Kind))
                {
                    tokenTextBlock.Foreground = ColorTheme[token.Kind];
                }
                if (isNotFirst)
                {
                    textBlock.Inlines.Add(" ");
                }
                else
                {
                    isNotFirst = true;
                }

                textBlock.Inlines.Add(tokenTextBlock);
            }

            return textBlock;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}