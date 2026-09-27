using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal sealed class TermPool
	{
		private const int MinimumBuckets = 64;

		private readonly List<int> _prefixScratch = new List<int>();
		
		public int Count => _count;

		private char[] _chars;
		private int _charCount;
		private int[] _termStart;
		private int[] _termLength;
		private int _count;
		private int[] _buckets;
		private int _bucketMask;
		private int[] _ordered;
		private bool _sealed;

		public TermPool(int expectedTerms = 0)
		{
			int terms = Math.Max(16, expectedTerms);

			_chars = new char[Math.Max(256, terms * 8)];
			_termStart = new int[terms];
			_termLength = new int[terms];
			_ordered = Array.Empty<int>();

			int buckets = MinimumBuckets;
			while (buckets < terms * 2)
			{
				buckets <<= 1;
			}

			_buckets = new int[buckets];
			_bucketMask = buckets - 1;
		}
		
		public ReadOnlySpan<char> Text(int termId) => new (_chars, _termStart[termId], _termLength[termId]);

		public int Length(int termId) => _termLength[termId];
		
		public int Intern(ReadOnlySpan<char> text)
		{
			if (_sealed)
			{
				throw new InvalidOperationException("Cannot add terms to a sealed pool.");
			}

			if (TryFind(text, out int existing))
			{
				return existing;
			}

			if (_count == _termStart.Length)
			{
				Array.Resize(ref _termStart, _count * 2);
				Array.Resize(ref _termLength, _count * 2);
			}
			
			if ((_count + 1) * 4 > _buckets.Length * 3)
			{
				Rehash();
			}

			if (_charCount + text.Length > _chars.Length)
			{
				int capacity = _chars.Length;
				while (capacity < _charCount + text.Length)
				{
					capacity *= 2;
				}

				Array.Resize(ref _chars, capacity);
			}

			for (int i = 0; i < text.Length; i++)
			{
				_chars[_charCount + i] = char.ToLowerInvariant(text[i]);
			}

			int id = _count++;
			_termStart[id] = _charCount;
			_termLength[id] = text.Length;
			_charCount += text.Length;

			Insert(id);
			return id;
		}
		
		public bool TryFind(ReadOnlySpan<char> text, out int termId)
		{
			if (text.Length == 0)
			{
				termId = -1;
				return false;
			}

			int slot = Hash(text) & _bucketMask;

			while (true)
			{
				int occupant = _buckets[slot];
				if (occupant == 0)
				{
					termId = -1;
					return false;
				}

				int candidate = occupant - 1;
				if (_termLength[candidate] == text.Length && Matches(candidate, text))
				{
					termId = candidate;
					return true;
				}

				slot = (slot + 1) & _bucketMask;
			}
		}
		
		public void Seal()
		{
			if (_sealed)
			{
				return;
			}

			_sealed = true;
			_ordered = new int[_count];

			for (int i = 0; i < _count; i++)
			{
				_ordered[i] = i;
			}

			Array.Sort(_ordered, new OrdinalOrder(this));
		}
		
		public List<int> TermsWithPrefix(ReadOnlySpan<char> prefix)
		{
			_prefixScratch.Clear();

			if (!_sealed || prefix.Length == 0)
			{
				return _prefixScratch;
			}

			int low = 0;
			int high = _count;

			while (low < high)
			{
				int middle = (int)(((uint)low + (uint)high) >> 1);
				if (Compare(_ordered[middle], prefix) < 0)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			for (int i = low; i < _count; i++)
			{
				int termId = _ordered[i];
				if (!StartsWith(termId, prefix))
				{
					break;
				}

				_prefixScratch.Add(termId);
			}

			return _prefixScratch;
		}

		public bool Contains(int termId, ReadOnlySpan<char> text)
		{
			int length = _termLength[termId];
			if (text.Length > length)
			{
				return false;
			}

			int start = _termStart[termId];
			int lastOffset = length - text.Length;

			for (int offset = 0; offset <= lastOffset; offset++)
			{
				int i = 0;
				while (i < text.Length && _chars[start + offset + i] == char.ToLowerInvariant(text[i]))
				{
					i++;
				}

				if (i == text.Length)
				{
					return true;
				}
			}

			return false;
		}
		
		private static int Hash(ReadOnlySpan<char> text)
		{
			unchecked
			{
				uint hash = 2166136261u;

				for (int i = 0; i < text.Length; i++)
				{
					hash = (hash ^ char.ToLowerInvariant(text[i])) * 16777619u;
				}

				return (int)(hash & 0x7FFFFFFF);
			}
		}

		private bool StartsWith(int termId, ReadOnlySpan<char> prefix)
		{
			if (_termLength[termId] < prefix.Length)
			{
				return false;
			}

			int start = _termStart[termId];
			for (int i = 0; i < prefix.Length; i++)
			{
				if (_chars[start + i] != char.ToLowerInvariant(prefix[i]))
				{
					return false;
				}
			}

			return true;
		}

		private bool Matches(int termId, ReadOnlySpan<char> text)
		{
			int start = _termStart[termId];

			for (int i = 0; i < text.Length; i++)
			{
				if (_chars[start + i] != char.ToLowerInvariant(text[i]))
				{
					return false;
				}
			}

			return true;
		}

		private int Compare(int termId, ReadOnlySpan<char> text)
		{
			int start = _termStart[termId];
			int length = _termLength[termId];
			int shared = Math.Min(length, text.Length);

			for (int i = 0; i < shared; i++)
			{
				int difference = _chars[start + i] - char.ToLowerInvariant(text[i]);
				if (difference != 0)
				{
					return difference;
				}
			}

			return length - text.Length;
		}

		private int CompareTerms(int left, int right)
		{
			int leftStart = _termStart[left];
			int rightStart = _termStart[right];
			int shared = Math.Min(_termLength[left], _termLength[right]);

			for (int i = 0; i < shared; i++)
			{
				int difference = _chars[leftStart + i] - _chars[rightStart + i];
				if (difference != 0)
				{
					return difference;
				}
			}

			return _termLength[left] - _termLength[right];
		}

		private void Insert(int termId)
		{
			int slot = Hash(Text(termId)) & _bucketMask;
			while (_buckets[slot] != 0)
			{
				slot = (slot + 1) & _bucketMask;
			}

			_buckets[slot] = termId + 1;
		}

		private void Rehash()
		{
			_buckets = new int[_buckets.Length * 2];
			_bucketMask = _buckets.Length - 1;

			for (int termId = 0; termId < _count; termId++)
			{
				int slot = Hash(Text(termId)) & _bucketMask;
				while (_buckets[slot] != 0)
				{
					slot = (slot + 1) & _bucketMask;
				}

				_buckets[slot] = termId + 1;
			}
		}

		private sealed class OrdinalOrder : IComparer<int>
		{
			private readonly TermPool _pool;

			public OrdinalOrder(TermPool pool)
			{
				_pool = pool;
			}

			public int Compare(int left, int right) => _pool.CompareTerms(left, right);
		}
	}
}