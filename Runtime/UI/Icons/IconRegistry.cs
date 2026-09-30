using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class IconRegistry : IIconRegistry
	{
		public event Action OnChanged;

		private readonly Dictionary<string, Background> _cache = new (StringComparer.Ordinal);
		private readonly HashSet<string> _missing = new (StringComparer.Ordinal);
		private readonly ILogSink _log;

		public IOmniDebuggerIconProvider Provider
		{
			get => _provider;
			set
			{
				MainThreadGuard.Verify(nameof(Provider));

				IOmniDebuggerIconProvider next = value ?? ResourcesIconProvider.Instance;
				if (ReferenceEquals(next, _provider))
				{
					return;
				}

				_provider = next;
				ClearCache();
				OnChanged?.Invoke();
			}
		}

		private IOmniDebuggerIconProvider _provider = ResourcesIconProvider.Instance;

		public IconRegistry(ILogSink log)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));
		}

		public bool TryGetImage(string key, out Background background)
		{
			background = default;

			if (string.IsNullOrWhiteSpace(key) || OmniGlyphs.Contains(key))
			{
				return false;
			}

			if (_cache.TryGetValue(key, out background))
			{
				return true;
			}

			if (_missing.Contains(key))
			{
				return false;
			}

			bool found;
			try
			{
				found = _provider.TryGetIcon(key, out background);
			}
			catch (Exception exception)
			{
				_log.Exception($"The icon provider failed to load '{key}'.", exception);
				found = false;
			}

			if (found)
			{
				_cache[key] = background;
				return true;
			}

			_missing.Add(key);
			_log.Warning($"Icon was not found and is neither a built-in glyph nor known to the icon provider. Key: {key}.");
			return false;
		}

		internal void Clear()
		{
			_provider = ResourcesIconProvider.Instance;
			OnChanged = null;
			ClearCache();
		}

		private void ClearCache()
		{
			_cache.Clear();
			_missing.Clear();
		}
	}
}
