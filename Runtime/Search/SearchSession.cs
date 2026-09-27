using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// One running query. Work is handed out in slices the caller pays for, so a search over
	/// a large index never owns a frame: <see cref="Advance"/> does as much as the given time
	/// budget allows and returns.
	/// <para>
	/// Stages run cheapest first — whole word, then prefix, then substring, then near-miss —
	/// and <see cref="Hits"/> is republished after each one. The first slice therefore
	/// already holds the results a person is most likely to want, while the expensive
	/// near-miss work is still outstanding.
	/// </para>
	/// </summary>
	/// <typeparam name="T">What is being searched.</typeparam>
	public sealed class SearchSession<T> : IDisposable
		where T : class, ISearchIndexable
	{
		/// <summary>Raised when <see cref="Hits"/> has been republished.</summary>
		public event Action OnUpdated;
		
		private const int MaxTokens = 32;

		private const int ExactScore = 1000;
		private const int PrefixScore = 800;
		private const int InfixScore = 600;
		private const int FuzzyBaseScore = 400;
		private const int FuzzyPenaltyPerEdit = 100;

		private readonly SearchIndex<T> _owner;
		private readonly List<SearchHit> _hits = new List<SearchHit>();
		private readonly List<int> _candidates = new List<int>();
		private readonly List<int> _touched = new List<int>();
		private readonly TopKHeap _heap = new TopKHeap();
		private readonly MyersDistance _myers = new MyersDistance();
		private readonly IndexSegment[] _segments = new IndexSegment[2];
		
		/// <summary>
		/// The best hits found so far, best first. Rebuilt after every stage, so hold the
		/// list rather than a copy of it and re-read it when <see cref="OnUpdated"/> fires.
		/// </summary>
		public IReadOnlyList<SearchHit> Hits => _hits;

		/// <summary>What the query is working on now, or <see cref="SearchStage.Done"/>.</summary>
		public SearchStage Stage { get; private set; } = SearchStage.Done;

		/// <summary>Whether there is no work left.</summary>
		public bool IsComplete => Stage == SearchStage.Done;

		private char[] _tokenChars = new char[64];
		private int[] _tokenStart = new int[MaxTokens];
		private int[] _tokenLength = new int[MaxTokens];
		private int _tokenCount;
		private int _allTokensMask;

		private int[] _mask = Array.Empty<int>();
		private int[] _score = Array.Empty<int>();
		private int[] _stamp = Array.Empty<int>();
		private int[] _emitted = Array.Empty<int>();
		private int[] _termStamp = Array.Empty<int>();

		private int _generation;
		private int _termGeneration;
		private int _segmentCount;
		private int _segmentIndex;
		private int _tokenIndex;
		private int _termCursor;
		private bool _unitPrepared;
		private bool _fuzzyUsable;
		private bool _disposed;
		private QuerySettings _settings;

		internal SearchSession(SearchIndex<T> owner)
		{
			_owner = owner;
		}

		/// <summary>
		/// Does up to <paramref name="budgetTicks"/> worth of work, measured in
		/// <see cref="Stopwatch"/> ticks. Returns true while work remains. A budget of zero
		/// still does one unit, so a caller cannot livelock by passing too little.
		/// </summary>
		public bool Advance(long budgetTicks)
		{
			if (_disposed || IsComplete)
			{
				return false;
			}

			long now = Stopwatch.GetTimestamp();
			long budget = Math.Max(0, budgetTicks);
			long deadline = budget > long.MaxValue - now ? long.MaxValue : now + budget;

			do
			{
				Step();
			}
			while (!IsComplete && Stopwatch.GetTimestamp() < deadline);

			return !IsComplete;
		}

		/// <summary>
		/// Runs the query to the end, spending at most <paramref name="budgetTicks"/> per
		/// frame. Cancelling leaves whatever was found in place.
		/// </summary>
		public async Awaitable RunAsync(long budgetTicks, CancellationToken cancellationToken = default)
		{
			while (Advance(budgetTicks))
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				await Awaitable.NextFrameAsync(cancellationToken);
			}
		}

		/// <summary>Stops the query where it is. <see cref="Hits"/> stays readable.</summary>
		public void Cancel() => Stage = SearchStage.Done;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			Stage = SearchStage.Done;
			OnUpdated = null;

			_hits.Clear();
			_candidates.Clear();
			_touched.Clear();
		}
		
		internal void Restart(string term, in QuerySettings settings, IndexSegment first, IndexSegment second, int itemCapacity)
		{
			_settings = settings;
			_hits.Clear();
			_candidates.Clear();
			_touched.Clear();

			_segmentCount = 0;
			if (!first.IsEmpty)
			{
				_segments[_segmentCount++] = first;
			}

			if (!second.IsEmpty)
			{
				_segments[_segmentCount++] = second;
			}

			EnsureItemCapacity(itemCapacity);
			EnsureTermCapacity();

			_generation++;
			_segmentIndex = 0;
			_tokenIndex = 0;
			_termCursor = 0;
			_unitPrepared = false;

			_heap.Reset(settings.MaxResults);
			SplitIntoTokens(term);

			Stage = _tokenCount == 0 || _segmentCount == 0
				? SearchStage.Done
				: SearchStage.Exact;
		}

		internal int TokenCount => _tokenCount;

		internal ReadOnlySpan<char> Token(int index) =>
			new ReadOnlySpan<char>(_tokenChars, _tokenStart[index], _tokenLength[index]);

		private void Step()
		{
			if (!_unitPrepared)
			{
				PrepareUnit();
				_unitPrepared = true;
				_termCursor = 0;
				return;
			}

			if (_termCursor < _candidates.Count)
			{
				ProcessTerm(_segments[_segmentIndex], _candidates[_termCursor]);
				_termCursor++;
				return;
			}

			_unitPrepared = false;
			_tokenIndex++;

			if (_tokenIndex < _tokenCount)
			{
				return;
			}

			_tokenIndex = 0;
			_segmentIndex++;

			if (_segmentIndex < _segmentCount)
			{
				return;
			}

			_segmentIndex = 0;
			PublishStage();
			MoveToNextStage();
		}

		private void PrepareUnit()
		{
			_candidates.Clear();

			IndexSegment segment = _segments[_segmentIndex];
			ReadOnlySpan<char> token = Token(_tokenIndex);

			switch (Stage)
			{
				case SearchStage.Exact:
					if (segment.Terms.TryFind(token, out int exact))
					{
						_candidates.Add(exact);
					}

					return;

				case SearchStage.Prefix:
					List<int> prefixed = segment.Terms.TermsWithPrefix(token);
					for (int i = 0; i < prefixed.Count; i++)
					{
						_candidates.Add(prefixed[i]);
					}

					return;

				case SearchStage.Infix:
					CollectTrigramCandidates(segment, token, pooled: false);
					return;

				default:
					_fuzzyUsable = _myers.TrySetPattern(token);
					if (_fuzzyUsable)
					{
						CollectTrigramCandidates(segment, token, pooled: true);
					}

					return;
			}
		}

		private void CollectTrigramCandidates(IndexSegment segment, ReadOnlySpan<char> token, bool pooled)
		{
			_termGeneration++;
			segment.Trigrams.CollectTerms(token, _termStamp, _termGeneration, _candidates, pooled);
		}

		private void ProcessTerm(IndexSegment segment, int termId)
		{
			ReadOnlySpan<char> token = Token(_tokenIndex);

			switch (Stage)
			{
				case SearchStage.Exact:
					Mark(segment, termId, ExactScore);
					return;

				case SearchStage.Prefix:
					Mark(segment, termId, PrefixScore);
					return;

				case SearchStage.Infix:
					if (segment.Terms.Contains(termId, token))
					{
						Mark(segment, termId, InfixScore);
					}

					return;

				default:
					MarkFuzzy(segment, termId, token);
					return;
			}
		}

		private void MarkFuzzy(IndexSegment segment, int termId, ReadOnlySpan<char> token)
		{
			int tolerance = MyersDistance.ToleranceFor(token.Length);
			if (tolerance == 0)
			{
				return;
			}

			int length = segment.Terms.Length(termId);
			if (Math.Abs(length - token.Length) > tolerance)
			{
				return;
			}

			int distance = _myers.Distance(segment.Terms.Text(termId));
			if (distance > tolerance)
			{
				return;
			}

			Mark(segment, termId, FuzzyBaseScore - distance * FuzzyPenaltyPerEdit);
		}

		private void Mark(IndexSegment segment, int termId, int score)
		{
			int bit = 1 << _tokenIndex;
			int end = segment.PostingEnd(termId);

			for (int position = segment.PostingStart(termId); position < end; position++)
			{
				int itemId = segment.Posting(position);

				if (_owner.IsRemoved(itemId))
				{
					continue;
				}

				if (_stamp[itemId] != _generation)
				{
					_stamp[itemId] = _generation;
					_mask[itemId] = 0;
					_score[itemId] = 0;
					_touched.Add(itemId);
				}

				if ((_mask[itemId] & bit) != 0)
				{
					continue;
				}

				_mask[itemId] |= bit;
				_score[itemId] += score;
			}
		}
		
		private void PublishStage()
		{
			bool changed = false;
			bool caseSensitive = (_settings.Options & SearchOptions.CaseSensitive) != 0;

			for (int i = 0; i < _touched.Count; i++)
			{
				int itemId = _touched[i];

				if (_mask[itemId] != _allTokensMask || _emitted[itemId] == _generation)
				{
					continue;
				}

				_emitted[itemId] = _generation;

				if (caseSensitive && !_owner.MatchesLetterCase(itemId, this))
				{
					continue;
				}

				changed |= _heap.Push(itemId, _score[itemId], Stage);
			}

			if (!changed)
			{
				return;
			}

			_heap.FillSorted(_hits);
			OnUpdated?.Invoke();
		}

		private void MoveToNextStage()
		{
			SearchOptions options = _settings.Options;
			if ((options & SearchOptions.ExactOnly) != 0)
			{
				Stage = SearchStage.Done;
				return;
			}

			switch (Stage)
			{
				case SearchStage.Exact:
				{
					Stage = SearchStage.Prefix;
				} return;

				case SearchStage.Prefix:
				{
					Stage = SearchStage.Infix;
				} return;

				case SearchStage.Infix:
				{
					Stage = (options & SearchOptions.NoFuzzy) != 0
						? SearchStage.Done
						: SearchStage.Fuzzy;
				} return;

				default:
				{
					Stage = SearchStage.Done;
				} return;
			}
		}
		
		private void SplitIntoTokens(string term)
		{
			_tokenCount = 0;
			_allTokensMask = 0;

			if (string.IsNullOrWhiteSpace(term))
			{
				return;
			}

			if (_tokenChars.Length < term.Length)
			{
				_tokenChars = new char[term.Length];
			}

			int written = 0;
			var scanner = new TermScanner(term);

			while (scanner.MoveNext() && _tokenCount < MaxTokens)
			{
				ReadOnlySpan<char> token = scanner.Current;
				token.CopyTo(new Span<char>(_tokenChars, written, token.Length));

				_tokenStart[_tokenCount] = written;
				_tokenLength[_tokenCount] = token.Length;
				_tokenCount++;

				written += token.Length;
			}

			_allTokensMask = _tokenCount == MaxTokens ? -1 : (1 << _tokenCount) - 1;
		}

		private void EnsureItemCapacity(int capacity)
		{
			if (_mask.Length >= capacity)
			{
				return;
			}

			int size = Math.Max(capacity, Math.Max(64, _mask.Length * 2));

			Array.Resize(ref _mask, size);
			Array.Resize(ref _score, size);
			Array.Resize(ref _stamp, size);
			Array.Resize(ref _emitted, size);
		}

		private void EnsureTermCapacity()
		{
			int required = 0;

			for (int i = 0; i < _segmentCount; i++)
			{
				required = Math.Max(required, _segments[i].Terms.Count);
			}

			if (_termStamp.Length >= required)
			{
				return;
			}

			_termStamp = new int[Math.Max(required, 64)];
			_termGeneration = 0;
		}
	}
}