using System;
using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal readonly struct LogFilter
	{
		private readonly List<string> _terms;
		private readonly IReadOnlyCollection<string> _tags;
		private readonly IReadOnlyCollection<string> _excludedTags;
		private readonly LogTypeMask _types;

		public LogFilter(in LogQuery query)
		{
			_terms = null;

			if (!string.IsNullOrWhiteSpace(query.Text))
			{
				_terms = new List<string>(2);
				LogQuerySyntax.SplitTerms(query.Text, _terms);

				if (_terms.Count == 0)
				{
					_terms = null;
				}
			}

			_tags = query.Tags != null && query.Tags.Count > 0 ? query.Tags : null;
			_excludedTags = query.ExcludedTags != null && query.ExcludedTags.Count > 0 ? query.ExcludedTags : null;
			_types = query.Types;
		}

		public static LogTypeMask MaskOf(LogType type)
		{
			switch (type)
			{
				case LogType.Warning:
					return LogTypeMask.Warning;
				case LogType.Error:
				case LogType.Assert:
				case LogType.Exception:
					return LogTypeMask.Error;
				default:
					return LogTypeMask.Log;
			}
		}

		public bool Matches(LogBody body)
		{
			if ((_types & MaskOf(body.Type)) == 0)
			{
				return false;
			}

			if (_tags != null && !CarriesAny(body.Tags, _tags))
			{
				return false;
			}

			if (_excludedTags != null && CarriesAny(body.Tags, _excludedTags))
			{
				return false;
			}

			if (_terms == null)
			{
				return true;
			}

			for (int i = 0; i < _terms.Count; i++)
			{
				string term = _terms[i];

				if (body.Message.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0 &&
					body.StackTrace.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
				{
					return false;
				}
			}

			return true;
		}

		private static bool CarriesAny(IReadOnlyList<string> recordTags, IReadOnlyCollection<string> wanted)
		{
			for (int i = 0; i < recordTags.Count; i++)
			{
				foreach (string tag in wanted)
				{
					if (string.Equals(recordTags[i], tag, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
