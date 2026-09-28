using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ValuePulse : IDisposable
	{
		private const long DefaultIntervalMs = 250;
		private const long MinimumIntervalMs = 50;

		private readonly List<IValueRefresher> _refreshers = new ();
		private readonly IVisualElementScheduledItem _scheduled;

		private long _intervalMs = DefaultIntervalMs;
		private bool _running;
		private bool _disposed;

		public ValuePulse(VisualElement host)
		{
			if (host == null)
			{
				throw new ArgumentNullException(nameof(host));
			}

			_scheduled = host.schedule.Execute(Tick).Every(_intervalMs);
			_scheduled.Pause();
		}

		public void SetInterval(long milliseconds)
		{
			long clamped = Math.Max(MinimumIntervalMs, milliseconds);

			if (clamped == _intervalMs)
			{
				return;
			}

			_intervalMs = clamped;
			_scheduled.Every(_intervalMs);
		}

		public void Register(IValueRefresher refresher)
		{
			if (_disposed || refresher == null || _refreshers.Contains(refresher))
			{
				return;
			}

			_refreshers.Add(refresher);
			refresher.RefreshValue();
		}

		public void Unregister(IValueRefresher refresher)
		{
			if (refresher == null)
			{
				return;
			}

			_refreshers.Remove(refresher);
		}

		public void Resume()
		{
			if (_disposed || _running)
			{
				return;
			}

			_running = true;
			_scheduled.Resume();
		}

		public void Pause()
		{
			if (_disposed || !_running)
			{
				return;
			}

			_running = false;
			_scheduled.Pause();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_running = false;
			_scheduled.Pause();
			_refreshers.Clear();
		}

		private void Tick(TimerState state)
		{
			for (int i = _refreshers.Count - 1; i >= 0; i--)
			{
				if (i >= _refreshers.Count)
				{
					continue;
				}

				_refreshers[i].RefreshValue();
			}
		}
	}
}