using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class LogStore : ILogFeed
	{
		public const int DefaultCapacity = 10000;
		public const int MaxMessageLength = 1024;
		public const int MaxStackTraceLength = 2048;

		private readonly object _gate = new ();
		private readonly LogRecord[] _records;
		private readonly Dictionary<string, int> _tagCounts = new (StringComparer.Ordinal);

		public long Version => Interlocked.Read(ref _version);

		public long ErrorCount => Interlocked.Read(ref _errorCount);

		public int Count
		{
			get
			{
				lock (_gate)
				{
					return _count;
				}
			}
		}

		private int _head;
		private int _count;
		private int _logs;
		private int _warnings;
		private int _errors;
		private long _nextId = 1;
		private long _version;
		private long _errorCount;

		public LogStore(int capacity = DefaultCapacity)
		{
			if (capacity <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be above zero.");
			}

			_records = new LogRecord[capacity];
		}

		public void Add(string message, string stackTrace, LogType type, DateTime timestampUtc)
		{
			string trimmedMessage = Cut(message, MaxMessageLength, out bool messageTruncated);
			string trimmedStack = Cut(stackTrace, MaxStackTraceLength, out bool stackTruncated);
			IReadOnlyList<string> tags = LogTagParser.Parse(trimmedMessage);

			lock (_gate)
			{
				if (_count == _records.Length)
				{
					Forget(_records[_head]);
					_records[_head] = default;
					_head = (_head + 1) % _records.Length;
					_count--;
				}

				LogRecord record = new LogRecord(
					_nextId++,
					timestampUtc,
					type,
					trimmedMessage,
					trimmedStack,
					tags,
					messageTruncated,
					stackTruncated);

				_records[(_head + _count) % _records.Length] = record;
				_count++;
				Remember(record);

				if (record.IsError)
				{
					Interlocked.Increment(ref _errorCount);
				}

				Interlocked.Increment(ref _version);
			}
		}

		public bool Query(in LogQuery query, List<LogRecord> results)
		{
			if (results == null)
			{
				throw new ArgumentNullException(nameof(results));
			}

			LogFilter filter = new LogFilter(query);
			int limit = query.Limit;

			lock (_gate)
			{
				return query.AfterId > 0
					? QueryForward(filter, query.AfterId, limit, results)
					: QueryBackward(filter, query.BeforeId, limit, results);
			}
		}

		public void CountByType(out int logs, out int warnings, out int errors)
		{
			lock (_gate)
			{
				logs = _logs;
				warnings = _warnings;
				errors = _errors;
			}
		}

		public void GetKnownTags(List<string> results)
		{
			if (results == null)
			{
				throw new ArgumentNullException(nameof(results));
			}

			int start = results.Count;

			lock (_gate)
			{
				foreach (KeyValuePair<string, int> pair in _tagCounts)
				{
					results.Add(pair.Key);
				}
			}

			results.Sort(start, results.Count - start, StringComparer.OrdinalIgnoreCase);
		}

		public void Clear()
		{
			lock (_gate)
			{
				Array.Clear(_records, 0, _records.Length);
				_tagCounts.Clear();
				_head = 0;
				_count = 0;
				_logs = 0;
				_warnings = 0;
				_errors = 0;
				Interlocked.Increment(ref _version);
			}
		}

		private static string Cut(string value, int max, out bool truncated)
		{
			if (string.IsNullOrEmpty(value))
			{
				truncated = false;
				return string.Empty;
			}

			truncated = value.Length > max;
			return truncated ? value.Substring(0, max) : value;
		}

		private bool QueryForward(in LogFilter filter, long afterId, int limit, List<LogRecord> results)
		{
			int added = 0;

			for (int i = FirstIndexAfter(afterId); i < _count; i++)
			{
				LogRecord record = At(i);

				if (!filter.Matches(record))
				{
					continue;
				}

				if (added == limit)
				{
					return true;
				}

				results.Add(record);
				added++;
			}

			return false;
		}

		private bool QueryBackward(in LogFilter filter, long beforeId, int limit, List<LogRecord> results)
		{
			int end = beforeId > 0 ? FirstIndexAtOrAfter(beforeId) : _count;
			int start = results.Count;
			int added = 0;
			bool hasMore = false;

			for (int i = end - 1; i >= 0; i--)
			{
				LogRecord record = At(i);

				if (!filter.Matches(record))
				{
					continue;
				}

				if (added == limit)
				{
					hasMore = true;
					break;
				}

				results.Add(record);
				added++;
			}

			results.Reverse(start, added);
			return hasMore;
		}

		private int FirstIndexAfter(long id) => FirstIndexAtOrAfter(id + 1);

		private int FirstIndexAtOrAfter(long id)
		{
			int low = 0;
			int high = _count;

			while (low < high)
			{
				int middle = (low + high) >> 1;

				if (At(middle).Id < id)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			return low;
		}

		private LogRecord At(int index) => _records[(_head + index) % _records.Length];

		private void Remember(in LogRecord record)
		{
			Tally(record.Type, 1);

			for (int i = 0; i < record.Tags.Count; i++)
			{
				string tag = record.Tags[i];
				_tagCounts[tag] = _tagCounts.TryGetValue(tag, out int count) ? count + 1 : 1;
			}
		}

		private void Forget(in LogRecord record)
		{
			Tally(record.Type, -1);

			for (int i = 0; i < record.Tags.Count; i++)
			{
				string tag = record.Tags[i];

				if (!_tagCounts.TryGetValue(tag, out int count))
				{
					continue;
				}

				if (count <= 1)
				{
					_tagCounts.Remove(tag);
				}
				else
				{
					_tagCounts[tag] = count - 1;
				}
			}
		}

		private void Tally(LogType type, int delta)
		{
			switch (LogFilter.MaskOf(type))
			{
				case LogTypeMask.Warning:
					_warnings += delta;
					break;
				case LogTypeMask.Error:
					_errors += delta;
					break;
				default:
					_logs += delta;
					break;
			}
		}
	}
}