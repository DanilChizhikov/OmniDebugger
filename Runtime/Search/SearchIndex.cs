using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// A searchable set of items, built for sets large enough that touching every item per
	/// keystroke is not an option. Items are held behind an inverted index, so a query reads
	/// only the terms that were typed and the items those terms belong to.
	/// <para>
	/// The index is kept in two pieces: a large one built in bulk, and a small one holding
	/// whatever was added since. Additions stay cheap, and the two are folded together only
	/// once the small piece has grown enough to be worth it.
	/// </para>
	/// </summary>
	/// <typeparam name="T">What is being searched.</typeparam>
	public sealed class SearchIndex<T> : IDisposable
		where T : class, ISearchIndexable
	{
		private const int MergeFloor = 64;
		private const int MergeDivisor = 10;

		private readonly List<T> _items = new List<T>();
		private readonly HashSet<int> _removed = new HashSet<int>();
		private readonly List<string> _deltaPhrases = new List<string>();
		private readonly List<int> _deltaOwners = new List<int>();
		private readonly List<string> _scratchPhrases = new List<string>();
		private readonly List<int> _scratchOwners = new List<int>();
		private readonly List<T> _scratchItems = new List<T>();
		private readonly List<string> _collected = new List<string>();
		
		/// <summary>How many items can currently be found.</summary>
		public int Count => _items.Count - _removed.Count;

		/// <summary>
		/// The item behind a <see cref="SearchHit.Id"/>. Ids stay valid until the next
		/// <see cref="Rebuild"/>, which hands out fresh ones. Null for a removed id.
		/// </summary>
		public T this[int id] => _items[id];

		private IndexSegment _base = IndexSegment.Empty;
		private IndexSegment _delta = IndexSegment.Empty;
		private SearchSession<T> _session;

		private int _baseItemCount;
		private int _deltaItemCount;
		private bool _deltaStale;
		private bool _disposed;

		/// <summary>
		/// Replaces everything in the index. Indexing happens here, so call it when the set
		/// changes wholesale — not on every keystroke, and not for a single addition.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
		public void Rebuild(IReadOnlyList<T> items)
		{
			MainThreadGuard.Verify(nameof(Rebuild));
			ThrowIfDisposed();

			if (items == null)
			{
				throw new ArgumentNullException(nameof(items));
			}

			Collect(items, _scratchItems, _scratchPhrases, _scratchOwners);
			Commit(_scratchItems, IndexBuilder.Build(_scratchPhrases, _scratchOwners));
		}

		/// <summary>
		/// Same as <see cref="Rebuild"/>, but only the phrase collection runs on the main
		/// thread; the index itself is built on a worker and swapped in when it is ready.
		/// Queries keep answering from the previous index until then.
		/// </summary>
		public async Awaitable RebuildAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
		{
			MainThreadGuard.Verify(nameof(RebuildAsync));
			ThrowIfDisposed();

			if (items == null)
			{
				throw new ArgumentNullException(nameof(items));
			}

			List<T> collectedItems = new List<T>(items.Count);
			List<string> phrases = new List<string>(items.Count * 2);
			List<int> owners = new List<int>(items.Count * 2);

			Collect(items, collectedItems, phrases, owners);

			await Awaitable.BackgroundThreadAsync();
			IndexSegment built = IndexBuilder.Build(phrases, owners);
			await Awaitable.MainThreadAsync();

			if (_disposed || cancellationToken.IsCancellationRequested)
			{
				return;
			}

			Commit(collectedItems, built);
		}

		/// <summary>
		/// Adds one item and returns the id it can be found by. Cheap: the item lands in the
		/// small piece of the index, and only a later fold touches the large one.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
		public int Add(T item)
		{
			MainThreadGuard.Verify(nameof(Add));
			ThrowIfDisposed();

			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			int id = _items.Count;
			_items.Add(item);

			CollectInto(item, id, _deltaPhrases, _deltaOwners);

			_deltaItemCount++;
			_deltaStale = true;
			CancelSession();

			if (_deltaItemCount >= Math.Max(MergeFloor, _baseItemCount / MergeDivisor))
			{
				Fold();
			}

			return id;
		}

		/// <summary>
		/// Stops an item being found. The id is retired, not reused: outstanding hits stay
		/// meaningful, and nothing has to be rewritten.
		/// </summary>
		/// <returns>False when the id was never handed out or is already gone.</returns>
		public bool Remove(int id)
		{
			MainThreadGuard.Verify(nameof(Remove));
			ThrowIfDisposed();

			if (id < 0 || id >= _items.Count || !_removed.Add(id))
			{
				return false;
			}

			_items[id] = default;
			CancelSession();
			return true;
		}

		/// <summary>
		/// Starts a query. The returned session is owned by the index and is reused by the
		/// next call, so a new query replaces the previous one rather than racing it.
		/// </summary>
		public SearchSession<T> BeginQuery(string term, in QuerySettings settings)
		{
			MainThreadGuard.Verify(nameof(BeginQuery));
			ThrowIfDisposed();

			if (_deltaStale)
			{
				_delta = IndexBuilder.Build(_deltaPhrases, _deltaOwners);
				_deltaStale = false;
			}

			_session ??= new SearchSession<T>(this);
			_session.Restart(term, settings, _base, _delta, _items.Count);

			return _session;
		}

		/// <summary>Empties the index and stops any running query.</summary>
		public void Clear()
		{
			MainThreadGuard.Verify(nameof(Clear));
			ThrowIfDisposed();

			CancelSession();

			_items.Clear();
			_removed.Clear();
			_deltaPhrases.Clear();
			_deltaOwners.Clear();

			_base = IndexSegment.Empty;
			_delta = IndexSegment.Empty;
			_baseItemCount = 0;
			_deltaItemCount = 0;
			_deltaStale = false;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			_session?.Dispose();
			_session = null;

			_items.Clear();
			_removed.Clear();
			_deltaPhrases.Clear();
			_deltaOwners.Clear();

			_base = IndexSegment.Empty;
			_delta = IndexSegment.Empty;
		}

		internal bool IsRemoved(int id) => _removed.Count > 0 && _removed.Contains(id);

		internal bool MatchesLetterCase(int id, SearchSession<T> session)
		{
			T item = _items[id];
			if (item == null)
			{
				return false;
			}

			_collected.Clear();
			item.CollectIndexTerms(_collected);

			for (int tokenIndex = 0; tokenIndex < session.TokenCount; tokenIndex++)
			{
				if (!AnyPhraseContains(session.Token(tokenIndex)))
				{
					return false;
				}
			}

			return true;
		}

		private bool AnyPhraseContains(ReadOnlySpan<char> token)
		{
			for (int i = 0; i < _collected.Count; i++)
			{
				string phrase = _collected[i];
				if (!string.IsNullOrEmpty(phrase) && phrase.AsSpan().IndexOf(token) >= 0)
				{
					return true;
				}
			}

			return false;
		}
		
		private void Fold()
		{
			_scratchPhrases.Clear();
			_scratchOwners.Clear();

			for (int id = 0; id < _items.Count; id++)
			{
				T item = _items[id];
				if (item == null || _removed.Contains(id))
				{
					continue;
				}

				CollectInto(item, id, _scratchPhrases, _scratchOwners);
			}

			_base = IndexBuilder.Build(_scratchPhrases, _scratchOwners);
			_baseItemCount = Count;

			_delta = IndexSegment.Empty;
			_deltaPhrases.Clear();
			_deltaOwners.Clear();
			_deltaItemCount = 0;
			_deltaStale = false;
		}

		private void Collect(IReadOnlyList<T> items, List<T> destination, List<string> phrases, List<int> owners)
		{
			destination.Clear();
			phrases.Clear();
			owners.Clear();

			for (int i = 0; i < items.Count; i++)
			{
				T item = items[i];
				if (item == null)
				{
					continue;
				}

				int id = destination.Count;
				destination.Add(item);

				CollectInto(item, id, phrases, owners);
			}
		}

		private void CollectInto(T item, int id, List<string> phrases, List<int> owners)
		{
			_collected.Clear();
			item.CollectIndexTerms(_collected);

			for (int i = 0; i < _collected.Count; i++)
			{
				string phrase = _collected[i];
				if (string.IsNullOrWhiteSpace(phrase))
				{
					continue;
				}

				phrases.Add(phrase);
				owners.Add(id);
			}
		}

		private void Commit(List<T> items, IndexSegment segment)
		{
			CancelSession();

			_items.Clear();
			_items.AddRange(items);
			_removed.Clear();

			_base = segment;
			_baseItemCount = _items.Count;

			_delta = IndexSegment.Empty;
			_deltaPhrases.Clear();
			_deltaOwners.Clear();
			_deltaItemCount = 0;
			_deltaStale = false;
		}

		private void CancelSession() => _session?.Cancel();

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(SearchIndex<T>));
			}
		}
	}
}