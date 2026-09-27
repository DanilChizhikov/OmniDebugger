using System;

namespace DTech.OmniDebugger
{
	internal sealed class IndexSegment
	{
		public static readonly IndexSegment Empty = new IndexSegment(new TermPool(), TrigramTable.Empty, new int[1], Array.Empty<int>());

		private readonly int[] _postingStart;
		private readonly int[] _postings;
		
		public TermPool Terms { get; }
		public TrigramTable Trigrams { get; }
		public bool IsEmpty => Terms.Count == 0;

		public IndexSegment(TermPool terms, TrigramTable trigrams, int[] postingStart, int[] postings)
		{
			Terms = terms;
			Trigrams = trigrams;
			_postingStart = postingStart;
			_postings = postings;
		}
		
		public int PostingStart(int termId) => _postingStart[termId];
		public int PostingEnd(int termId) => _postingStart[termId + 1];
		public int Posting(int position) => _postings[position];
		public int Frequency(int termId) => _postingStart[termId + 1] - _postingStart[termId];
	}
}