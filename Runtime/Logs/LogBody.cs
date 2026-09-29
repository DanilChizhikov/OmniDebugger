using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class LogBody
	{
		public LogType Type { get; }

		public string Message { get; }

		public string StackTrace { get; }

		public IReadOnlyList<string> Tags { get; }

		public bool IsMessageTruncated { get; }

		public bool IsStackTraceTruncated { get; }

		public int Hash { get; }

		public int TextLength => Message.Length + StackTrace.Length;

		public bool IsError => Type == LogType.Error || Type == LogType.Assert || Type == LogType.Exception;

		public int References { get; set; }

		public LogBody Next { get; set; }

		public int FilterStamp { get; set; }

		public bool FilterResult { get; set; }

		public LogBody(in LogBodyKey key, LogBody next)
		{
			Type = key.Type;
			Message = key.IsMessageTruncated ? key.Message.Substring(0, key.MessageLength) : key.Message;
			StackTrace = key.IsStackTraceTruncated ? key.StackTrace.Substring(0, key.StackTraceLength) : key.StackTrace;
			Tags = LogTagParser.Parse(Message);
			IsMessageTruncated = key.IsMessageTruncated;
			IsStackTraceTruncated = key.IsStackTraceTruncated;
			Hash = key.Hash;
			References = 1;
			Next = next;
		}
	}
}
