using System;
using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger
{
	/// <summary>One message from Unity's console.</summary>
	public readonly struct LogRecord
	{
		/// <summary>Grows with every record, so a later record always has a bigger id.</summary>
		public long Id { get; }

		public DateTime TimestampUtc { get; }

		public LogType Type { get; }

		/// <summary>The message, cut at the feed's limit. See <see cref="IsMessageTruncated"/>.</summary>
		public string Message { get; }

		/// <summary>The stack trace, cut at the feed's limit. Empty when Unity gave none.</summary>
		public string StackTrace { get; }

		/// <summary>
		/// Leading <c>[Tag]</c> prefixes of the message: <c>"[Net] [Auth] failed"</c> carries
		/// <c>Net</c> and <c>Auth</c>. Never null.
		/// </summary>
		public IReadOnlyList<string> Tags { get; }

		public bool IsMessageTruncated { get; }

		public bool IsStackTraceTruncated { get; }

		/// <summary>Errors, asserts and exceptions.</summary>
		public bool IsError => Type == LogType.Error || Type == LogType.Assert || Type == LogType.Exception;

		public LogRecord(
			long id,
			DateTime timestampUtc,
			LogType type,
			string message,
			string stackTrace,
			IReadOnlyList<string> tags,
			bool isMessageTruncated,
			bool isStackTraceTruncated)
		{
			Id = id;
			TimestampUtc = timestampUtc;
			Type = type;
			Message = message ?? string.Empty;
			StackTrace = stackTrace ?? string.Empty;
			Tags = tags ?? Array.Empty<string>();
			IsMessageTruncated = isMessageTruncated;
			IsStackTraceTruncated = isStackTraceTruncated;
		}
	}
}