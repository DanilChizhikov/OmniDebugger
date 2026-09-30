using System;
using System.Collections.Generic;
using System.Text;

namespace DTech.OmniDebugger
{
	internal static class LogQuerySyntax
	{
		public const string TagKey = "tag";
		public const string TypeKey = "type";

		public static ParsedLogQuery Parse(string filter)
		{
			List<LogQueryToken> tokens = new List<LogQueryToken>();
			Tokenize(filter, tokens);

			StringBuilder text = null;
			HashSet<string> tags = null;
			HashSet<string> excluded = null;
			LogTypeMask included = LogTypeMask.None;
			LogTypeMask removed = LogTypeMask.None;

			foreach (LogQueryToken token in tokens)
			{
				switch (token.Kind)
				{
					case LogQueryTokenKind.Tag when token.Negated:
						(excluded ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(token.Value);
						break;

					case LogQueryTokenKind.Tag:
						(tags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(token.Value);
						break;

					case LogQueryTokenKind.Type when token.Negated:
						removed |= token.Type;
						break;

					case LogQueryTokenKind.Type:
						included |= token.Type;
						break;

					default:
						text ??= new StringBuilder();
						if (text.Length > 0)
						{
							text.Append(' ');
						}

						text.Append(filter, token.Start, token.Length);
						break;
				}
			}

			LogTypeMask types = (included == LogTypeMask.None ? LogTypeMask.All : included) & ~removed;

			return new ParsedLogQuery(text?.ToString(), tags, excluded, types);
		}

		public static void Tokenize(string filter, List<LogQueryToken> results)
		{
			if (string.IsNullOrEmpty(filter))
			{
				return;
			}

			int index = 0;
			while (index < filter.Length)
			{
				while (index < filter.Length && char.IsWhiteSpace(filter[index]))
				{
					index++;
				}

				if (index >= filter.Length)
				{
					break;
				}

				int start = index;
				bool quoted = false;

				while (index < filter.Length && (quoted || !char.IsWhiteSpace(filter[index])))
				{
					if (filter[index] == '"')
					{
						quoted = !quoted;
					}

					index++;
				}

				results.Add(Read(filter, start, index - start));
			}
		}

		public static void SplitTerms(string text, List<string> results)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}

			List<LogQueryToken> tokens = new List<LogQueryToken>();
			Tokenize(text, tokens);

			foreach (LogQueryToken token in tokens)
			{
				string term = Unquote(text.Substring(token.Start, token.Length));
				if (term.Length > 0)
				{
					results.Add(term);
				}
			}
		}

		public static string Format(string key, string value, bool negated)
		{
			string written = value.IndexOf(' ') >= 0 || value.IndexOf('"') >= 0
				? "\"" + value.Replace("\"", string.Empty) + "\""
				: value;

			return (negated ? "-" : string.Empty) + key + ":" + written;
		}

		public static bool TryReadType(string value, out LogTypeMask type)
		{
			switch (value.ToLowerInvariant())
			{
				case "log":
				case "info":
					type = LogTypeMask.Log;
					return true;

				case "warning":
				case "warn":
					type = LogTypeMask.Warning;
					return true;

				case "error":
				case "assert":
				case "exception":
					type = LogTypeMask.Error;
					return true;

				default:
					type = LogTypeMask.None;
					return false;
			}
		}

		private static LogQueryToken Read(string filter, int start, int length)
		{
			string raw = filter.Substring(start, length);
			bool negated = raw.Length > 1 && raw[0] == '-';
			string body = negated ? raw.Substring(1) : raw;
			int colon = body.IndexOf(':');

			if (colon > 0 && colon < body.Length - 1)
			{
				string key = body.Substring(0, colon);
				string value = Unquote(body.Substring(colon + 1));

				if (value.Length > 0)
				{
					if (string.Equals(key, TagKey, StringComparison.OrdinalIgnoreCase))
					{
						return new LogQueryToken(LogQueryTokenKind.Tag, value, negated, LogTypeMask.None, start, length);
					}

					if (string.Equals(key, TypeKey, StringComparison.OrdinalIgnoreCase) &&
						TryReadType(value, out LogTypeMask type))
					{
						return new LogQueryToken(LogQueryTokenKind.Type, value, negated, type, start, length);
					}
				}
			}

			return new LogQueryToken(LogQueryTokenKind.Text, Unquote(raw), false, LogTypeMask.None, start, length);
		}

		private static string Unquote(string value) => value.Replace("\"", string.Empty).Trim();
	}
}
