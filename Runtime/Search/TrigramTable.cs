using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal sealed class TrigramTable
	{
		private const int WindowSize = 3;
		
		public static readonly TrigramTable Empty = new TrigramTable();

		private int[] _keys = Array.Empty<int>();
		private int[] _start = Array.Empty<int>();
		private int[] _terms = Array.Empty<int>();
		private int _keyCount;
		private int _termCount;
		
		public void Build(TermPool pool)
		{
			_termCount = pool.Count;

			List<long> pairs = new List<long>(pool.Count * 4);

			for (int termId = 0; termId < pool.Count; termId++)
			{
				ReadOnlySpan<char> text = pool.Text(termId);

				for (int i = 0; i + WindowSize <= text.Length; i++)
				{
					long key = Key(text[i], text[i + 1], text[i + 2]);
					pairs.Add((key << 32) | (uint)termId);
				}
			}

			long[] sorted = pairs.ToArray();
			Array.Sort(sorted);

			_keys = new int[sorted.Length];
			_start = new int[sorted.Length + 1];
			_terms = new int[sorted.Length];
			_keyCount = 0;

			int written = 0;
			int index = 0;

			while (index < sorted.Length)
			{
				int key = (int)(sorted[index] >> 32);

				_keys[_keyCount] = key;
				_start[_keyCount] = written;

				int previousTerm = -1;
				while (index < sorted.Length && (int)(sorted[index] >> 32) == key)
				{
					int termId = (int)(sorted[index] & 0xFFFFFFFF);
					if (termId != previousTerm)
					{
						_terms[written++] = termId;
						previousTerm = termId;
					}

					index++;
				}

				_keyCount++;
			}

			_start[_keyCount] = written;
			
			Array.Resize(ref _keys, _keyCount);
			Array.Resize(ref _start, _keyCount + 1);
			Array.Resize(ref _terms, written);
		}
		
		public void CollectTerms(ReadOnlySpan<char> token, int[] termStamp, int generation, List<int> result, bool pooled)
		{
			if (token.Length < WindowSize || _keyCount == 0)
			{
				return;
			}

			if (!pooled)
			{
				int rarest = RarestSlot(token, requireEveryWindow: true);
				if (rarest >= 0)
				{
					AddSlot(rarest, termStamp, generation, result);
				}

				return;
			}

			int cap = Math.Max(64, _termCount / 8);
			bool added = false;

			for (int i = 0; i + WindowSize <= token.Length; i++)
			{
				if (!TryFindSlot(token, i, out int slot) || _start[slot + 1] - _start[slot] > cap)
				{
					continue;
				}

				AddSlot(slot, termStamp, generation, result);
				added = true;
			}

			if (added)
			{
				return;
			}

			int fallback = RarestSlot(token, requireEveryWindow: false);
			if (fallback >= 0)
			{
				AddSlot(fallback, termStamp, generation, result);
			}
		}
		
		private int RarestSlot(ReadOnlySpan<char> token, bool requireEveryWindow)
		{
			int best = -1;
			int bestSize = int.MaxValue;

			for (int i = 0; i + WindowSize <= token.Length; i++)
			{
				if (!TryFindSlot(token, i, out int slot))
				{
					if (requireEveryWindow)
					{
						return -1;
					}

					continue;
				}

				int size = _start[slot + 1] - _start[slot];
				if (size >= bestSize)
				{
					continue;
				}

				best = slot;
				bestSize = size;
			}

			return best;
		}

		private bool TryFindSlot(ReadOnlySpan<char> token, int offset, out int slot)
		{
			int key = Key(
				char.ToLowerInvariant(token[offset]),
				char.ToLowerInvariant(token[offset + 1]),
				char.ToLowerInvariant(token[offset + 2]));

			return TryFindKey(key, out slot);
		}

		private void AddSlot(int slot, int[] termStamp, int generation, List<int> result)
		{
			int end = _start[slot + 1];

			for (int p = _start[slot]; p < end; p++)
			{
				int termId = _terms[p];
				if (termStamp[termId] == generation)
				{
					continue;
				}

				termStamp[termId] = generation;
				result.Add(termId);
			}
		}

		private bool TryFindKey(int key, out int slot)
		{
			int low = 0;
			int high = _keyCount - 1;

			while (low <= high)
			{
				int middle = (int)(((uint)low + (uint)high) >> 1);
				int candidate = _keys[middle];

				if (candidate == key)
				{
					slot = middle;
					return true;
				}

				if (candidate < key)
				{
					low = middle + 1;
				}
				else
				{
					high = middle - 1;
				}
			}

			slot = -1;
			return false;
		}

		private static int Key(char first, char second, char third) =>
			((first & 0x3FF) << 20) | ((second & 0x3FF) << 10) | (third & 0x3FF);
	}
}