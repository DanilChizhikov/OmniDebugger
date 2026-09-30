using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class SearchController : IDisposable
	{
		public event Action OnResultsChanged;

		private const int MaxResults = 100;
		private const long PumpIntervalMs = 16;
		private const long BudgetDivisor = 2000;

		private readonly SearchIndex<CommandDefinition> _index = new ();
		private readonly List<CommandDefinition> _results = new ();
		private readonly IVisualElementScheduledItem _pump;
		private readonly long _budgetTicks;

		public bool HasQuery => !string.IsNullOrWhiteSpace(_query);
		public IReadOnlyList<CommandDefinition> Results => _results;

		private SearchSession<CommandDefinition> _session;
		private string _query = string.Empty;
		private SearchOptions _options = SearchOptions.None;
		private bool _pumping;
		private bool _disposed;

		public SearchController(VisualElement host)
		{
			if (host == null)
			{
				throw new ArgumentNullException(nameof(host));
			}

			_budgetTicks = Math.Max(1, Stopwatch.Frequency / BudgetDivisor);
			_pump = host.schedule.Execute(Pump).Every(PumpIntervalMs);
			_pump.Pause();
		}

		public void SetSource(IReadOnlyList<CommandDefinition> commands)
		{
			if (_disposed)
			{
				return;
			}

			_index.Rebuild(commands ?? Array.Empty<CommandDefinition>());
			_session = null;

			if (HasQuery)
			{
				StartQuery();
				return;
			}

			_results.Clear();
			OnResultsChanged?.Invoke();
		}

		public void SetQuery(string query)
		{
			if (_disposed)
			{
				return;
			}

			_query = query ?? string.Empty;

			if (!HasQuery)
			{
				_session?.Cancel();
				_results.Clear();
				Stop();
				OnResultsChanged?.Invoke();
				return;
			}

			StartQuery();
		}

		public void SetOptions(SearchOptions options)
		{
			if (_disposed || options == _options)
			{
				return;
			}

			_options = options;

			if (HasQuery)
			{
				StartQuery();
			}
		}

		public void Pause() => Stop();

		public void Resume()
		{
			if (_disposed || _session == null || _session.IsComplete)
			{
				return;
			}

			Start();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			Stop();

			if (_session != null)
			{
				_session.OnUpdated -= OnSessionUpdated;
				_session.Cancel();
				_session = null;
			}

			_index.Dispose();
			_results.Clear();
			OnResultsChanged = null;
		}

		private void StartQuery()
		{
			SearchSession<CommandDefinition> session = _index.BeginQuery(_query, new QuerySettings(MaxResults, _options));

			if (!ReferenceEquals(session, _session))
			{
				if (_session != null)
				{
					_session.OnUpdated -= OnSessionUpdated;
				}

				_session = session;
				_session.OnUpdated += OnSessionUpdated;
			}

			CollectResults();
			Start();
		}

		private void Pump(TimerState state)
		{
			if (_session == null || _session.IsComplete)
			{
				Stop();
				return;
			}

			_session.Advance(_budgetTicks);

			if (_session.IsComplete)
			{
				Stop();
			}
		}

		private void OnSessionUpdated() => CollectResults();

		private void CollectResults()
		{
			_results.Clear();

			if (_session == null)
			{
				OnResultsChanged?.Invoke();
				return;
			}

			IReadOnlyList<SearchHit> hits = _session.Hits;

			for (int i = 0; i < hits.Count; i++)
			{
				if (!TryResolve(hits[i].Id, out CommandDefinition definition))
				{
					continue;
				}

				_results.Add(definition);
			}

			OnResultsChanged?.Invoke();
		}

		private bool TryResolve(int id, out CommandDefinition definition)
		{
			try
			{
				definition = _index[id];
			}
			catch (ArgumentOutOfRangeException)
			{
				definition = null;
			}

			return definition != null;
		}

		private void Start()
		{
			if (_pumping)
			{
				return;
			}

			_pumping = true;
			_pump.Resume();
		}

		private void Stop()
		{
			if (!_pumping)
			{
				return;
			}

			_pumping = false;
			_pump.Pause();
		}
	}
}