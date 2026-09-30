using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal sealed class InfoRegistry : IInfoRegistry
	{
		public event Action OnChanged;

		public event Action OnFloatingChanged;

		private static readonly Comparison<IInfoProvider> _order = Compare;

		private readonly List<IInfoProvider> _providers = new ();
		private readonly List<string> _floating = new ();
		private readonly ILogSink _log;

		public IReadOnlyList<IInfoProvider> All => _providers;

		internal IReadOnlyList<string> FloatingKeys => _floating;

		internal bool IsRestored { get; set; }

		public InfoRegistry(ILogSink log)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));

			_providers.Add(new PerformanceInfo());
			_providers.Add(new MemoryInfo());
			_providers.Add(new GraphicsInfo());
			_providers.Add(new QualityInfo());
			_providers.Add(new ScreenInfo());
			_providers.Add(new BuildInfo());
			_providers.Add(new DeviceInfo());
			_providers.Sort(_order);
		}

		public bool Register(IInfoProvider provider)
		{
			MainThreadGuard.Verify(nameof(Register));

			if (provider == null)
			{
				throw new ArgumentNullException(nameof(provider));
			}

			if (_providers.Contains(provider))
			{
				_log.Warning($"Info section is already registered and was ignored. Title: {provider.Title}.");
				return false;
			}

			_providers.Add(provider);
			_providers.Sort(_order);
			OnChanged?.Invoke();
			return true;
		}

		public bool Unregister(IInfoProvider provider)
		{
			MainThreadGuard.Verify(nameof(Unregister));

			if (provider == null || !_providers.Remove(provider))
			{
				return false;
			}

			OnChanged?.Invoke();
			return true;
		}

		public bool Float(IInfoProvider provider)
		{
			MainThreadGuard.Verify(nameof(Float));

			string key = KeyOf(provider);
			if (key == null || _floating.Contains(key))
			{
				return false;
			}

			_floating.Add(key);
			OnFloatingChanged?.Invoke();
			return true;
		}

		public bool Dock(IInfoProvider provider)
		{
			MainThreadGuard.Verify(nameof(Dock));

			string key = KeyOf(provider);
			if (key == null || !_floating.Remove(key))
			{
				return false;
			}

			OnFloatingChanged?.Invoke();
			return true;
		}

		public bool IsFloating(IInfoProvider provider)
		{
			string key = KeyOf(provider);
			return key != null && _floating.Contains(key);
		}

		internal static string KeyOf(IInfoProvider provider)
		{
			if (provider == null)
			{
				return null;
			}

			try
			{
				string title = provider.Title;
				return string.IsNullOrWhiteSpace(title) ? InfoSectionModel.Unknown : title;
			}
			catch (Exception)
			{
				return null;
			}
		}

		internal bool TryGetProvider(string key, out IInfoProvider provider)
		{
			for (int i = 0; i < _providers.Count; i++)
			{
				if (string.Equals(KeyOf(_providers[i]), key, StringComparison.Ordinal))
				{
					provider = _providers[i];
					return true;
				}
			}

			provider = null;
			return false;
		}

		internal void RestoreFloating(IReadOnlyList<string> keys)
		{
			bool changed = false;

			for (int i = 0; i < keys.Count; i++)
			{
				string key = keys[i];

				if (!string.IsNullOrWhiteSpace(key) && !_floating.Contains(key))
				{
					_floating.Add(key);
					changed = true;
				}
			}

			if (changed)
			{
				OnFloatingChanged?.Invoke();
			}
		}

		internal void Clear()
		{
			_providers.Clear();
			_floating.Clear();
			OnChanged = null;
			OnFloatingChanged = null;
		}

		private static int Compare(IInfoProvider left, IInfoProvider right)
		{
			int byOrder = left.Order.CompareTo(right.Order);
			return byOrder != 0 ? byOrder : string.CompareOrdinal(left.Title, right.Title);
		}
	}
}
