using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal static class KeyListFormat
	{
		private const char Separator = '\n';

		public static string Join(IReadOnlyList<string> keys)
		{
			if (keys == null || keys.Count == 0)
			{
				return string.Empty;
			}

			return string.Join(Separator.ToString(), keys);
		}

		public static IReadOnlyList<string> Split(string stored)
		{
			if (string.IsNullOrEmpty(stored))
			{
				return Array.Empty<string>();
			}

			string[] parts = stored.Split(Separator);
			List<string> keys = new List<string>(parts.Length);
			HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

			for (int i = 0; i < parts.Length; i++)
			{
				string key = parts[i];

				if (!string.IsNullOrWhiteSpace(key) && seen.Add(key))
				{
					keys.Add(key);
				}
			}

			return keys;
		}
	}
}