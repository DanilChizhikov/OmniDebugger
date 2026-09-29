using System;

namespace DTech.OmniDebugger
{
	internal readonly struct LogSlot
	{
		public readonly long Id;
		public readonly DateTime TimestampUtc;
		public readonly LogBody Body;

		public LogSlot(long id, DateTime timestampUtc, LogBody body)
		{
			Id = id;
			TimestampUtc = timestampUtc;
			Body = body;
		}

		public LogRecord ToRecord() =>
			new LogRecord(
				Id,
				TimestampUtc,
				Body.Type,
				Body.Message,
				Body.StackTrace,
				Body.Tags,
				Body.IsMessageTruncated,
				Body.IsStackTraceTruncated);
	}
}
