using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal static class LogTagParser
	{
		public const int MaxTagLength = 64;

		public static IReadOnlyList<string> Parse(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return Array.Empty<string>();
			}

			List<string> tags = null;
			int index = SkipSpaces(message, 0);

			while (index < message.Length && message[index] == '[')
			{
				int close = message.IndexOf(']', index + 1);

				if (close < 0)
				{
					break;
				}

				int length = close - index - 1;
				string tag = length > 0 && length <= MaxTagLength
					? message.Substring(index + 1, length).Trim()
					: null;

				if (string.IsNullOrEmpty(tag) || tag.IndexOf('[') >= 0)
				{
					break;
				}

				tags ??= new List<string>();

				if (!tags.Contains(tag))
				{
					tags.Add(tag);
				}

				index = SkipSpaces(message, close + 1);
			}

			return tags == null ? Array.Empty<string>() : tags;
		}

		private static int SkipSpaces(string text, int index)
		{
			while (index < text.Length && char.IsWhiteSpace(text[index]))
			{
				index++;
			}

			return index;
		}
	}
}