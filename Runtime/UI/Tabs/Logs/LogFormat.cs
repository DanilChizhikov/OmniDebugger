using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DTech.OmniDebugger.UI
{
	internal static class LogFormat
	{
		private const string TimeFormat = "HH:mm:ss.fff";
		private const string Truncated = "[truncated]";

		public static string Meta(in LogRecord record)
		{
			StringBuilder builder = new StringBuilder(48);
			builder.Append(record.TimestampUtc.ToLocalTime().ToString(TimeFormat, CultureInfo.InvariantCulture));
			builder.Append("  ");
			builder.Append(record.Type);

			for (int i = 0; i < record.Tags.Count; i++)
			{
				builder.Append("  [").Append(record.Tags[i]).Append(']');
			}

			if (record.IsMessageTruncated || record.IsStackTraceTruncated)
			{
				builder.Append("  ").Append(Truncated);
			}

			return builder.ToString();
		}

		public static string ForClipboard(in LogRecord record)
		{
			StringBuilder builder = new StringBuilder(record.Message.Length + record.StackTrace.Length + 48);
			Append(builder, record);
			return builder.ToString();
		}

		public static string ForClipboard(IReadOnlyList<LogRecord> records)
		{
			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < records.Count; i++)
			{
				if (i > 0)
				{
					builder.Append('\n');
				}

				Append(builder, records[i]);
			}

			return builder.ToString();
		}

		private static void Append(StringBuilder builder, in LogRecord record)
		{
			builder.Append('[').Append(record.TimestampUtc.ToLocalTime().ToString(TimeFormat, CultureInfo.InvariantCulture)).Append("] ");
			builder.Append('[').Append(record.Type).Append("] ");

			if (record.IsMessageTruncated || record.IsStackTraceTruncated)
			{
				builder.Append(Truncated).Append(' ');
			}

			builder.Append(record.Message);

			if (!string.IsNullOrEmpty(record.StackTrace))
			{
				builder.Append('\n').Append(record.StackTrace.TrimEnd());
			}
		}
	}
}