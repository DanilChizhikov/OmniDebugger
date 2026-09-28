using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class IconRegistry : IIconRegistry
	{
		private readonly List<IOmniDebuggerIconProvider> _providers = new ();
		private readonly List<OmniDebuggerIconCatalog> _catalogs = new ();
		private readonly Dictionary<string, Background> _cache = new (StringComparer.Ordinal);
		private readonly HashSet<string> _missing = new (StringComparer.Ordinal);
		private readonly ILogSink _log;

		private bool _catalogsLoaded;

		public IconRegistry(ILogSink log)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));
		}

		public void Register(IOmniDebuggerIconProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException(nameof(provider));
			}

			if (_providers.Contains(provider))
			{
				return;
			}

			_providers.Add(provider);
			ClearCache();
		}

		public bool Unregister(IOmniDebuggerIconProvider provider)
		{
			if (provider == null || !_providers.Remove(provider))
			{
				return false;
			}

			ClearCache();
			return true;
		}

		public void AddCatalog(OmniDebuggerIconCatalog catalog)
		{
			if (catalog == null)
			{
				throw new ArgumentNullException(nameof(catalog));
			}

			if (_catalogs.Contains(catalog))
			{
				return;
			}

			_catalogs.Add(catalog);
			ClearCache();
		}

		public bool TryGet(in CommandIcon icon, out Background background)
		{
			background = default;

			if (icon.IsEmpty)
			{
				return false;
			}

			string cacheKey = icon.ToString();

			if (_cache.TryGetValue(cacheKey, out background))
			{
				return true;
			}

			if (_missing.Contains(cacheKey))
			{
				return false;
			}

			if (TryResolve(icon, out background))
			{
				_cache[cacheKey] = background;
				return true;
			}

			_missing.Add(cacheKey);
			_log.Warning($"Command icon was not found. Source: {icon.Source}; Key: {icon.Key}.");
			return false;
		}

		internal void Clear()
		{
			_providers.Clear();
			_catalogs.Clear();
			_catalogsLoaded = false;
			ClearCache();
		}

		private static bool TryLoadResource(string path, out Background background)
		{
			Sprite sprite = Resources.Load<Sprite>(path);

			if (sprite != null)
			{
				background = Background.FromSprite(sprite);
				return true;
			}

			Texture2D texture = Resources.Load<Texture2D>(path);
			background = texture != null ? Background.FromTexture2D(texture) : default;
			return texture != null;
		}

		private bool TryResolve(in CommandIcon icon, out Background background)
		{
			for (int i = _providers.Count - 1; i >= 0; i--)
			{
				if (_providers[i].TryGetIcon(icon, out background))
				{
					return true;
				}
			}

			return icon.Source == DebugIconSource.Resources
				? TryLoadResource(icon.Key, out background)
				: TryFindInCatalogs(icon.Key, out background);
		}

		private bool TryFindInCatalogs(string key, out Background background)
		{
			EnsureCatalogs();

			for (int i = 0; i < _catalogs.Count; i++)
			{
				if (_catalogs[i] == null || !_catalogs[i].TryGet(key, out IconEntry entry))
				{
					continue;
				}

				if (entry.Sprite != null)
				{
					background = Background.FromSprite(entry.Sprite);
					return true;
				}

				if (entry.Texture != null)
				{
					background = Background.FromTexture2D(entry.Texture);
					return true;
				}
			}

			background = default;
			return false;
		}

		private void EnsureCatalogs()
		{
			if (_catalogsLoaded)
			{
				return;
			}

			_catalogsLoaded = true;
			OmniDebuggerIconCatalog[] found = Resources.LoadAll<OmniDebuggerIconCatalog>(OmniDebuggerUiAssets.ResourcesFolder);

			for (int i = 0; i < found.Length; i++)
			{
				if (!_catalogs.Contains(found[i]))
				{
					_catalogs.Add(found[i]);
				}
			}
		}

		private void ClearCache()
		{
			_cache.Clear();
			_missing.Clear();
		}
	}
}