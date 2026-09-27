using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal static class IndexBuilder
	{
		public static IndexSegment Build(List<string> phrases, List<int> owners)
		{
			if (phrases.Count == 0)
			{
				return IndexSegment.Empty;
			}

			TermPool pool = new TermPool(phrases.Count * 4);
			List<long> pairs = new List<long>(phrases.Count * 4);

			char[] compact = new char[64];
			char[] initials = new char[64];

			for (int i = 0; i < phrases.Count; i++)
			{
				string phrase = phrases[i];
				if (string.IsNullOrWhiteSpace(phrase))
				{
					continue;
				}

				if (compact.Length < phrase.Length)
				{
					compact = new char[phrase.Length];
					initials = new char[phrase.Length];
				}

				AddPhrase(phrase, owners[i], pool, pairs, compact, initials);
			}

			pool.Seal();

			TrigramTable trigrams = new TrigramTable();
			trigrams.Build(pool);

			int[] postingStart = BuildPostings(pool.Count, pairs, out int[] postings);

			return new IndexSegment(pool, trigrams, postingStart, postings);
		}

		private static void AddPhrase(string phrase, int itemId, TermPool pool, List<long> pairs, char[] compact, char[] initials)
		{
			int compactLength = 0;
			int initialsLength = 0;

			TermScanner scanner = new TermScanner(phrase);
			while (scanner.MoveNext())
			{
				ReadOnlySpan<char> term = scanner.Current;

				pairs.Add(Pair(pool.Intern(term), itemId));

				term.CopyTo(new Span<char>(compact, compactLength, term.Length));
				compactLength += term.Length;

				initials[initialsLength++] = term[0];
			}

			if (initialsLength < 2)
			{
				return;
			}

			pairs.Add(Pair(pool.Intern(new ReadOnlySpan<char>(compact, 0, compactLength)), itemId));
			pairs.Add(Pair(pool.Intern(new ReadOnlySpan<char>(initials, 0, initialsLength)), itemId));
		}

		private static int[] BuildPostings(int termCount, List<long> pairs, out int[] postings)
		{
			long[] sorted = pairs.ToArray();
			Array.Sort(sorted);

			int[] start = new int[termCount + 1];
			postings = new int[sorted.Length];

			int written = 0;
			long previous = -1;

			for (int i = 0; i < sorted.Length; i++)
			{
				long pair = sorted[i];
				if (pair == previous)
				{
					continue;
				}

				previous = pair;

				int termId = (int)(pair >> 32);
				postings[written++] = (int)(pair & 0xFFFFFFFF);
				start[termId + 1]++;
			}

			for (int termId = 0; termId < termCount; termId++)
			{
				start[termId + 1] += start[termId];
			}

			if (written != postings.Length)
			{
				Array.Resize(ref postings, written);
			}

			return start;
		}

		private static long Pair(int termId, int itemId) => ((long)termId << 32) | (uint)itemId;
	}
}