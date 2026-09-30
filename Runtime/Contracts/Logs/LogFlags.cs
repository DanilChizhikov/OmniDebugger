using System;

namespace DTech.OmniDebugger
{
	/// <summary>What happened to a <see cref="LogRecord"/> on its way into the feed.</summary>
	[Flags]
	public enum LogFlags : byte
	{
		None = 0,

		/// <summary><see cref="LogRecord.Message"/> was cut to fit the feed's limit.</summary>
		MessageTruncated = 1 << 0,

		/// <summary><see cref="LogRecord.StackTrace"/> was cut to fit the feed's limit.</summary>
		StackTraceTruncated = 1 << 1,

		/// <summary>Either part was cut.</summary>
		Truncated = MessageTruncated | StackTraceTruncated,
	}
}
