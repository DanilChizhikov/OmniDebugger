using System;
using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ThemeCatalog : IThemeRegistry
	{
		public event Action OnChanged;

		public const string DarkThemeId = "dark";
		public const string LightThemeId = "light";

		private const string DarkThemeName = "Dark";
		private const string LightThemeName = "Light";
		private const int BuiltInSortOrder = 0;

		private static readonly Comparison<OmniDebuggerTheme> _order = Compare;

		private readonly List<OmniDebuggerTheme> _registered = new ();
		private readonly List<OmniDebuggerTheme> _builtIn = new ();
		private readonly List<OmniDebuggerTheme> _themes = new ();
		private readonly Dictionary<string, OmniDebuggerTheme> _byId = new (StringComparer.Ordinal);
		private readonly ILogSink _log;

		public IReadOnlyList<OmniDebuggerTheme> All
		{
			get
			{
				EnsureLoaded();
				return _themes;
			}
		}

		public OmniDebuggerTheme Default
		{
			get
			{
				EnsureLoaded();

				if (_byId.TryGetValue(DarkThemeId, out OmniDebuggerTheme dark))
				{
					return dark;
				}

				return _themes[0];
			}
		}

		private bool _loaded;

		public ThemeCatalog(ILogSink log)
		{
			if (log == null)
			{
				throw new ArgumentNullException(nameof(log));
			}

			_log = log;
		}

		public bool TryGet(string id, out OmniDebuggerTheme theme)
		{
			EnsureLoaded();

			if (string.IsNullOrWhiteSpace(id))
			{
				theme = null;
				return false;
			}

			return _byId.TryGetValue(id, out theme);
		}

		public bool Register(OmniDebuggerTheme theme)
		{
			MainThreadGuard.Verify(nameof(Register));

			if (theme == null)
			{
				throw new ArgumentNullException(nameof(theme));
			}

			if (_registered.Contains(theme))
			{
				return false;
			}

			_registered.Add(theme);
			Rebuild();
			OnChanged?.Invoke();
			return true;
		}

		public bool Unregister(OmniDebuggerTheme theme)
		{
			MainThreadGuard.Verify(nameof(Unregister));

			if (theme == null)
			{
				throw new ArgumentNullException(nameof(theme));
			}

			if (!_registered.Remove(theme))
			{
				return false;
			}

			Rebuild();
			OnChanged?.Invoke();
			return true;
		}

		public void Refresh()
		{
			MainThreadGuard.Verify(nameof(Refresh));

			Rebuild();
			OnChanged?.Invoke();
		}

		public void Clear()
		{
			_registered.Clear();
			_themes.Clear();
			_byId.Clear();
			_loaded = false;

			for (int i = 0; i < _builtIn.Count; i++)
			{
				UnityEngine.Object.DestroyImmediate(_builtIn[i]);
			}

			_builtIn.Clear();
			OnChanged = null;
		}

		private static int Compare(OmniDebuggerTheme left, OmniDebuggerTheme right)
		{
			int byOrder = left.SortOrder.CompareTo(right.SortOrder);
			return byOrder != 0 ? byOrder : string.CompareOrdinal(left.DisplayName, right.DisplayName);
		}

		private void EnsureLoaded()
		{
			if (_loaded)
			{
				return;
			}

			Rebuild();
		}

		private void Rebuild()
		{
			_loaded = true;
			_themes.Clear();
			_byId.Clear();

			OmniDebuggerTheme[] discovered = Resources.LoadAll<OmniDebuggerTheme>(OmniDebuggerUiAssets.ResourcesFolder);

			for (int i = 0; i < discovered.Length; i++)
			{
				Collect(discovered[i], reportDuplicates: true);
			}

			for (int i = 0; i < _registered.Count; i++)
			{
				Collect(_registered[i], reportDuplicates: true);
			}

			EnsureBuiltIns();

			for (int i = 0; i < _builtIn.Count; i++)
			{
				Collect(_builtIn[i], reportDuplicates: false);
			}

			_themes.Sort(_order);
		}

		private void EnsureBuiltIns()
		{
			if (_builtIn.Count > 0)
			{
				return;
			}

			_builtIn.Add(OmniDebuggerTheme.CreateBuiltIn(DarkThemeId, DarkThemeName, BuiltInSortOrder, null));
			_builtIn.Add(OmniDebuggerTheme.CreateBuiltIn(
				LightThemeId,
				LightThemeName,
				BuiltInSortOrder,
				OmniDebuggerUiAssets.LightTokens));
		}

		private void Collect(OmniDebuggerTheme theme, bool reportDuplicates)
		{
			if (theme == null)
			{
				return;
			}

			string id = theme.Id;

			if (_byId.TryGetValue(id, out OmniDebuggerTheme existing))
			{
				if (reportDuplicates && !ReferenceEquals(existing, theme))
				{
					_log.Warning(
						"Two themes share an id, so only the first one is offered. " +
						$"Id: {id}; Kept: {existing.name}; Ignored: {theme.name}.");
				}

				return;
			}

			_byId.Add(id, theme);
			_themes.Add(theme);
		}
	}
}