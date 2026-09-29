using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class LogStore : ILogFeed
	{
		public const int DefaultCapacity = 16384;
		public const int DefaultTextBudget = 4 * 1024 * 1024;
		public const int MaxMessageLength = 1500;
		public const int MaxStackTraceLength = 4000;

		private readonly object _gate = new ();
		private readonly LogSlot[] _slots;
		private readonly LogBodyPool _bodies = new ();
		private readonly Dictionary<string, int> _tagCounts = new (StringComparer.Ordinal);
		private readonly int _textBudget;

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
		private int _filterStamp;
		private long _nextId = 1;
		private long _version;
		private long _errorCount;

		internal int BodyCount
		{
			get
			{
				lock (_gate)
				{
					return _bodies.Count;
				}
			}
		}

		public LogStore(int capacity = DefaultCapacity, int textBudget = DefaultTextBudget)
		{
			if (capacity <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be above zero.");
			}

			if (textBudget <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(textBudget), textBudget, "The text budget must be above zero.");
			}

			_slots = new LogSlot[capacity];
			_textBudget = textBudget;
		}

		public void Add(string message, string stackTrace, LogType type, DateTime timestampUtc)
		{
			LogBodyKey key = new LogBodyKey(message, stackTrace, type, MaxMessageLength, MaxStackTraceLength);

			lock (_gate)
			{
				LogBody body = _bodies.Rent(key);

				if (_count == _slots.Length)
				{
					DropOldest();
				}

				_slots[(_head + _count) % _slots.Length] = new LogSlot(_nextId++, timestampUtc, body);
				_count++;
				Remember(body);

				while (_bodies.TextLength > _textBudget && _count > 1)
				{
					DropOldest();
				}

				if (body.IsError)
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
				_filterStamp = _filterStamp == int.MaxValue ? 1 : _filterStamp + 1;

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
				Array.Clear(_slots, 0, _slots.Length);
				_bodies.Clear();
				_tagCounts.Clear();
				_head = 0;
				_count = 0;
				_logs = 0;
				_warnings = 0;
				_errors = 0;
				Interlocked.Increment(ref _version);
			}
		}

		private bool QueryForward(in LogFilter filter, long afterId, int limit, List<LogRecord> results)
		{
			int added = 0;

			for (int i = FirstIndexAfter(afterId); i < _count; i++)
			{
				LogSlot slot = At(i);

				if (!Accepts(filter, slot.Body))
				{
					continue;
				}

				if (added == limit)
				{
					return true;
				}

				results.Add(slot.ToRecord());
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
				LogSlot slot = At(i);

				if (!Accepts(filter, slot.Body))
				{
					continue;
				}

				if (added == limit)
				{
					hasMore = true;
					break;
				}

				results.Add(slot.ToRecord());
				added++;
			}

			results.Reverse(start, added);
			return hasMore;
		}

		private bool Accepts(in LogFilter filter, LogBody body)
		{
			if (body.FilterStamp != _filterStamp)
			{
				body.FilterStamp = _filterStamp;
				body.FilterResult = filter.Matches(body);
			}

			return body.FilterResult;
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

		private LogSlot At(int index) => _slots[(_head + index) % _slots.Length];

		private void DropOldest()
		{
			LogBody body = _slots[_head].Body;

			_slots[_head] = default;
			_head = (_head + 1) % _slots.Length;
			_count--;

			Forget(body);
			_bodies.Return(body);
		}

		private void Remember(LogBody body)
		{
			Tally(body.Type, 1);

			for (int i = 0; i < body.Tags.Count; i++)
			{
				string tag = body.Tags[i];
				_tagCounts[tag] = _tagCounts.TryGetValue(tag, out int count) ? count + 1 : 1;
			}
		}

		private void Forget(LogBody body)
		{
			Tally(body.Type, -1);

			for (int i = 0; i < body.Tags.Count; i++)
			{
				string tag = body.Tags[i];

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
