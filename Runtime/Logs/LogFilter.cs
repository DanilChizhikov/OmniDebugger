using System;
using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal readonly struct LogFilter
	{
		private readonly string _text;
		private readonly IReadOnlyCollection<string> _tags;
		private readonly LogTagMode _tagMode;
		private readonly LogTypeMask _types;

		public LogFilter(in LogQuery query)
		{
			_text = string.IsNullOrWhiteSpace(query.Text) ? null : query.Text.Trim();
			_tags = query.Tags != null && query.Tags.Count > 0 ? query.Tags : null;
			_tagMode = query.TagMode;
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

		public bool Matches(in LogRecord record)
		{
			if ((_types & MaskOf(record.Type)) == 0)
			{
				return false;
			}

			if (_tags != null && !MatchesTags(record.Tags))
			{
				return false;
			}

			return _text == null ||
				record.Message.IndexOf(_text, StringComparison.OrdinalIgnoreCase) >= 0 ||
				record.StackTrace.IndexOf(_text, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static bool Contains(IReadOnlyList<string> tags, string wanted)
		{
			for (int i = 0; i < tags.Count; i++)
			{
				if (string.Equals(tags[i], wanted, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private bool MatchesTags(IReadOnlyList<string> recordTags)
		{
			foreach (string wanted in _tags)
			{
				bool found = Contains(recordTags, wanted);

				if (_tagMode == LogTagMode.Any && found)
				{
					return true;
				}

				if (_tagMode == LogTagMode.All && !found)
				{
					return false;
				}
			}

			return _tagMode == LogTagMode.All;
		}
	}
}