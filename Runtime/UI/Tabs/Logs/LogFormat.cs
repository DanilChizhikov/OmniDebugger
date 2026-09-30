using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal static class LogFormat
	{
		private const string TimeFormat = "HH:mm:ss.fff";
		private const string Truncated = "[truncated]";

		public static string Timestamp(in LogRecord record) =>
			record.TimestampUtc.ToLocalTime().ToString(TimeFormat, CultureInfo.InvariantCulture);

		public static string Repeats(int count) =>
			count > 9999 ? "×9999+" : "×" + count.ToString(CultureInfo.InvariantCulture);

		public static string TypeLabel(LogType type)
		{
			switch (type)
			{
				case LogType.Warning:
					return "WARNING";
				case LogType.Error:
					return "ERROR";
				case LogType.Assert:
					return "ASSERT";
				case LogType.Exception:
					return "EXCEPTION";
				default:
					return "LOG";
			}
		}

		public static string TagLine(in LogRecord record)
		{
			bool truncated = (record.Flags & LogFlags.Truncated) != 0;

			if (record.Tags.Count == 0 && !truncated)
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder(32);

			for (int i = 0; i < record.Tags.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(' ');
				}

				builder.Append('[').Append(record.Tags[i]).Append(']');
			}

			if (truncated)
			{
				if (builder.Length > 0)
				{
					builder.Append(' ');
				}

				builder.Append(Truncated);
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
			builder.Append('[').Append(Timestamp(record)).Append("] ");
			builder.Append('[').Append(record.Type).Append("] ");

			if ((record.Flags & LogFlags.Truncated) != 0)
			{
				builder.Append(Truncated).Append(' ');
			}

			builder.Append(record.Message);

			if (record.RepeatCount > 1)
			{
				builder.Append(" (").Append(Repeats(record.RepeatCount)).Append(')');
			}

			if (!string.IsNullOrEmpty(record.StackTrace))
			{
				builder.Append('\n').Append(record.StackTrace.TrimEnd());
			}
		}
	}
}
