using System;
using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger
{
	/// <summary>One message from Unity's console, and how many times in a row it arrived.</summary>
	public readonly struct LogRecord
	{
		/// <summary>Grows with every record, so a later record always has a bigger id.</summary>
		public long Id { get; }

		/// <summary>When the message first reached the debugger, in UTC.</summary>
		public DateTime TimestampUtc { get; }

		/// <summary>When its latest repeat reached the debugger, in UTC; <see cref="TimestampUtc"/> when there is none.</summary>
		public DateTime LastTimestampUtc { get; }

		/// <summary>
		/// How many times the message arrived back to back, word for word and stack trace included. A repeat
		/// is folded into the record before it rather than stored again, so a message logged every frame
		/// takes one record. Never below 1.
		/// </summary>
		public int RepeatCount { get; }

		/// <summary>The kind of message, as Unity reported it.</summary>
		public LogType Type { get; }

		/// <summary>The message, cut at the feed's limit. See <see cref="Flags"/>.</summary>
		public string Message { get; }

		/// <summary>The stack trace, cut at the feed's limit. Empty when Unity gave none.</summary>
		public string StackTrace { get; }

		/// <summary>
		/// Tags of the message: its leading <c>[Tag]</c> prefixes — <c>"[Net] [Auth] failed"</c> carries
		/// <c>Net</c> and <c>Auth</c> — or the tag of <c>Debug.unityLogger.Log("Net", "failed")</c>, which Unity
		/// writes as <c>"Net: failed"</c>. Never null.
		/// </summary>
		public IReadOnlyList<string> Tags { get; }

		/// <summary>Whether the message or the stack trace was cut to fit the feed's limit.</summary>
		public LogFlags Flags { get; }

		/// <summary>Errors, asserts and exceptions.</summary>
		public bool IsError => Type == LogType.Error || Type == LogType.Assert || Type == LogType.Exception;

		public LogRecord(
			long id,
			DateTime timestampUtc,
			LogType type,
			string message,
			string stackTrace,
			IReadOnlyList<string> tags,
			LogFlags flags = LogFlags.None,
			int repeatCount = 1,
			DateTime lastTimestampUtc = default)
		{
			Id = id;
			TimestampUtc = timestampUtc;
			LastTimestampUtc = lastTimestampUtc == default ? timestampUtc : lastTimestampUtc;
			RepeatCount = repeatCount < 1 ? 1 : repeatCount;
			Type = type;
			Message = message ?? string.Empty;
			StackTrace = stackTrace ?? string.Empty;
			Tags = tags ?? Array.Empty<string>();
			Flags = flags;
		}
	}
}
