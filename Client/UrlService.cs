using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Client
{
	/// <summary>
	/// An attached property for TextBlocks to allow URLs to be clickable.
	/// </summary>
	/// <remarks>From http://stackoverflow.com/questions/861409/wpf-making-hyperlinks-clickable </remarks>
	public static class UrlService
	{
		// Copied from http://flanders.co.nz/2009/11/08/a-good-url-regular-expression-repost/
		private static readonly Regex ReUrl = new Regex(@"(?:(())(www\.([^/?#\s]*))|((http(s)?|ftp):)(//([^/?#\s]*)))([^?#\s]*)(\?([^#\s]*))?(#([^\s]*))?");

		public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
			"Text",
			typeof(string),
			typeof(UrlService),
			new PropertyMetadata(null, OnTextChanged)
		);

		public static string GetText(DependencyObject d)
		{ return d.GetValue(TextProperty) as string; }

		public static void SetText(DependencyObject d, string value)
		{ d.SetValue(TextProperty, value); }

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textBlock = d as TextBlock;
			if (textBlock == null)
				return;

			textBlock.Inlines.Clear();

			var newText = (string)e.NewValue;
			if (string.IsNullOrEmpty(newText))
				return;

			// Find all URLs using a regular expression
			int lastPos = 0;
			foreach (Match match in ReUrl.Matches(newText))
			{
				// Copy raw string from the last position up to the match
				if (match.Index != lastPos)
				{
					var rawText = newText.Substring(lastPos, match.Index - lastPos);
					textBlock.Inlines.Add(new Run(rawText));
				}

				// Create a hyperlink for the match
                var uri = match.Value.StartsWith("www.") ? string.Format("http://{0}", match.Value) : match.Value;

                // in case the regex fails
			    if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute)) 
                    continue;

				var link = new Hyperlink(new Run(match.Value))
				{
					// If it starts with "www." add "http://" to make it a valid Uri
					NavigateUri = new Uri(uri),
					ToolTip = uri
				};

				link.Click += OnUrlClick;

				textBlock.Inlines.Add(link);

				// Update the last matched position
				lastPos = match.Index + match.Length;
			}

			// Finally, copy the remainder of the string
			if (lastPos < newText.Length)
				textBlock.Inlines.Add(new Run(newText.Substring(lastPos)));
		}

		private static void OnUrlClick(object sender, RoutedEventArgs e)
		{
			var link = (Hyperlink)sender;
			// Do something with link.NavigateUri like:
			Process.Start(link.NavigateUri.ToString());
		}
	}
}