using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LogsFilterState
	{
		private static readonly LogTypeMask[] _types = { LogTypeMask.Log, LogTypeMask.Warning, LogTypeMask.Error };
		private static readonly string[] _typeNames = { "log", "warning", "error" };

		private readonly List<LogQueryToken> _tokens = new ();

		public string Query { get; set; } = string.Empty;

		public LogTypeMask Types => LogQuerySyntax.Parse(Query).Types;

		public LogQuery ToQuery(int limit) => LogQuery.Parse(Query, limit);

		public IReadOnlyList<LogQueryToken> GetTerms()
		{
			_tokens.Clear();
			LogQuerySyntax.Tokenize(Query, _tokens);
			_tokens.RemoveAll(token => token.Kind == LogQueryTokenKind.Text);
			return _tokens;
		}

		public bool HasTag(string tag)
		{
			foreach (LogQueryToken token in GetTerms())
			{
				if (token.Kind == LogQueryTokenKind.Tag &&
					string.Equals(token.Value, tag, System.StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		public void AddTag(string tag, bool excluded) =>
			Append(LogQuerySyntax.Format(LogQuerySyntax.TagKey, tag, excluded));

		public void Remove(in LogQueryToken token)
		{
			if (token.Start < 0 || token.Start + token.Length > Query.Length)
			{
				return;
			}

			Query = Tidy(Query.Remove(token.Start, token.Length));
		}

		public void ToggleType(LogTypeMask type)
		{
			LogTypeMask types = Types ^ type;
			if (types == LogTypeMask.None)
			{
				types = LogTypeMask.All;
			}

			List<LogQueryToken> tokens = new List<LogQueryToken>();
			LogQuerySyntax.Tokenize(Query, tokens);

			string query = Query;
			for (int i = tokens.Count - 1; i >= 0; i--)
			{
				if (tokens[i].Kind == LogQueryTokenKind.Type)
				{
					query = query.Remove(tokens[i].Start, tokens[i].Length);
				}
			}

			Query = Tidy(query);

			if (types == LogTypeMask.All)
			{
				return;
			}

			for (int i = 0; i < _types.Length; i++)
			{
				if ((types & _types[i]) != 0)
				{
					Append(LogQuerySyntax.TypeKey + ":" + _typeNames[i]);
				}
			}
		}

		private static string Tidy(string query)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return string.Empty;
			}

			string[] parts = query.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
			return string.Join(" ", parts);
		}

		private void Append(string term) => Query = Tidy(Query + " " + term);
	}
}
