using System;
using System.Collections.Generic;
using System.Globalization;

namespace DTech.OmniDebugger.UI
{
	internal static class ScaleListFormat
	{
		private const char Separator = '\t';

		public static string Format(IReadOnlyDictionary<string, float> scales)
		{
			if (scales == null || scales.Count == 0)
			{
				return string.Empty;
			}

			List<string> lines = new List<string>(scales.Count);

			foreach (KeyValuePair<string, float> pair in scales)
			{
				if (!string.IsNullOrWhiteSpace(pair.Key))
				{
					lines.Add(pair.Value.ToString("0.###", CultureInfo.InvariantCulture) + Separator + pair.Key);
				}
			}

			return KeyListFormat.Join(lines);
		}

		public static IReadOnlyDictionary<string, float> Parse(string stored)
		{
			Dictionary<string, float> scales = new Dictionary<string, float>(StringComparer.Ordinal);
			IReadOnlyList<string> lines = KeyListFormat.Split(stored);

			for (int i = 0; i < lines.Count; i++)
			{
				string line = lines[i];
				int split = line.IndexOf(Separator);

				if (split <= 0 || split == line.Length - 1)
				{
					continue;
				}

				if (float.TryParse(line.Substring(0, split), NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) &&
					scale > 0.0f)
				{
					scales[line.Substring(split + 1)] = scale;
				}
			}

			return scales;
		}
	}
}
