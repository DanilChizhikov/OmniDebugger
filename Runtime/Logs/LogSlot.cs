using System;

namespace DTech.OmniDebugger
{
	internal struct LogSlot
	{
		public readonly long Id;
		public readonly DateTime TimestampUtc;
		public readonly LogBody Body;

		public DateTime LastTimestampUtc;
		public int RepeatCount;

		public LogSlot(long id, DateTime timestampUtc, LogBody body)
		{
			Id = id;
			TimestampUtc = timestampUtc;
			Body = body;
			LastTimestampUtc = timestampUtc;
			RepeatCount = 1;
		}

		public LogRecord ToRecord() =>
			new LogRecord(
				Id,
				TimestampUtc,
				Body.Type,
				Body.Message,
				Body.StackTrace,
				Body.Tags,
				Body.Flags,
				RepeatCount,
				LastTimestampUtc);
	}
}
