using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Client.Utilities;
using ColorFont;

namespace Client.Converters
{
    public class ColoredItemConverter : IValueConverter
    {
        private static readonly Dictionary<TaskTokenKind, SolidColorBrush> Theme = new Dictionary<TaskTokenKind, SolidColorBrush>();

        public static void ReloadTheme()
        {
            Theme.Clear();

            var colors = new AvailableColors();

            Theme.Add(TaskTokenKind.Project, colors.GetFontColorByName(User.Default.ProjectColor).Brush);
            Theme.Add(TaskTokenKind.Context, colors.GetFontColorByName(User.Default.ContextColor).Brush);
            Theme.Add(TaskTokenKind.KeyValue, colors.GetFontColorByName(User.Default.KeyValueColor).Brush);
            Theme.Add(TaskTokenKind.PriorityA, colors.GetFontColorByName(User.Default.PriorityAColor).Brush);
            Theme.Add(TaskTokenKind.PriorityB, colors.GetFontColorByName(User.Default.PriorityBColor).Brush);
            Theme.Add(TaskTokenKind.PriorityC, colors.GetFontColorByName(User.Default.PriorityCColor).Brush);
        }
        

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
                if (Theme.ContainsKey(token.Kind))
                {
                    tokenTextBlock.Foreground = Theme[token.Kind];
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