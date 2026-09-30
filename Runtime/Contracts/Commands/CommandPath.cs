using System;
using System.Collections.Generic;
using System.Text;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Builds and reads the stable identifier of a command: a path such as <c>"Economy/Coins/Add"</c>.
	/// Every segment but the last names a group, nested as deep as you like; the last one is the command.
	/// Paths are human readable on purpose, so they can be typed into a console, stored in a config, or
	/// sent over a wire without a lookup table.
	/// </summary>
	public static class CommandPath
	{
		/// <summary>Separates the segments of a path.</summary>
		public const char Separator = '/';

		/// <summary>
		/// Trims every segment and drops empty ones, so <c>" Economy // Coins "</c> reads
		/// <c>"Economy/Coins"</c>. Returns an empty string for a null or blank path.
		/// </summary>
		public static string Normalize(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return string.Empty;
			}

			if (IsNormalized(path))
			{
				return path;
			}

			StringBuilder builder = new StringBuilder(path.Length);
			string[] segments = path.Split(Separator);

			for (int i = 0; i < segments.Length; i++)
			{
				string segment = segments[i].Trim();
				if (segment.Length == 0)
				{
					continue;
				}

				if (builder.Length > 0)
				{
					builder.Append(Separator);
				}

				builder.Append(segment);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Joins a group path and a command name into a path.
		/// </summary>
		public static string Combine(string groupPath, string name)
		{
			string group = Normalize(groupPath);
			if (group.Length == 0)
			{
				throw new ArgumentException("Group path cannot be null or whitespace.", nameof(groupPath));
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException("Command name cannot be null or whitespace.", nameof(name));
			}

			if (name.IndexOf(Separator) >= 0)
			{
				throw new ArgumentException($"Command name cannot contain '{Separator}': {name}.", nameof(name));
			}

			return string.Concat(group, Separator.ToString(), name.Trim());
		}

		/// <summary>Everything before the last segment; empty for a single segment.</summary>
		public static string GetParent(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			int index = path.LastIndexOf(Separator);
			return index <= 0 ? string.Empty : path.Substring(0, index);
		}

		/// <summary>The last segment.</summary>
		public static string GetName(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			int index = path.LastIndexOf(Separator);
			return index < 0 ? path : path.Substring(index + 1);
		}

		/// <summary>The first segment.</summary>
		public static string GetRoot(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			int index = path.IndexOf(Separator);
			return index < 0 ? path : path.Substring(0, index);
		}

		/// <summary>The segments of a normalized path, first to last.</summary>
		public static string[] Split(string path) =>
			string.IsNullOrEmpty(path) ? Array.Empty<string>() : path.Split(Separator);

		/// <summary>
		/// Every group on the way to <paramref name="groupPath"/>, outermost first:
		/// <c>"A/B/C"</c> yields <c>"A"</c>, <c>"A/B"</c>, <c>"A/B/C"</c>.
		/// </summary>
		public static void CollectAncestors(string groupPath, ICollection<string> results)
		{
			if (results == null)
			{
				throw new ArgumentNullException(nameof(results));
			}

			if (string.IsNullOrEmpty(groupPath))
			{
				return;
			}

			int index = groupPath.IndexOf(Separator);
			while (index > 0)
			{
				results.Add(groupPath.Substring(0, index));
				index = groupPath.IndexOf(Separator, index + 1);
			}

			results.Add(groupPath);
		}

		/// <summary>
		/// Whether <paramref name="path"/> is <paramref name="groupPath"/> itself or lies anywhere under it.
		/// An empty group holds everything.
		/// </summary>
		public static bool IsWithin(string path, string groupPath)
		{
			if (string.IsNullOrEmpty(groupPath))
			{
				return true;
			}

			if (path == null || path.Length < groupPath.Length ||
				!path.StartsWith(groupPath, StringComparison.Ordinal))
			{
				return false;
			}

			return path.Length == groupPath.Length || path[groupPath.Length] == Separator;
		}

		private static bool IsNormalized(string path)
		{
			if (path[0] == Separator || path[path.Length - 1] == Separator ||
				char.IsWhiteSpace(path[0]) || char.IsWhiteSpace(path[path.Length - 1]))
			{
				return false;
			}

			for (int i = 1; i < path.Length; i++)
			{
				char current = path[i];
				char previous = path[i - 1];

				if (current == Separator && (previous == Separator || char.IsWhiteSpace(previous)))
				{
					return false;
				}

				if (previous == Separator && char.IsWhiteSpace(current))
				{
					return false;
				}
			}

			return true;
		}
	}
}
