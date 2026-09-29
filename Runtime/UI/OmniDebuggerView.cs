using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// The panel itself, built into an element the caller owns. One class serves both mounts: the
	/// runtime <see cref="OmniDebuggerPanel"/> and the editor window pass the same settings with
	/// different values, and neither one knows how the other renders.
	/// </summary>
	public sealed class OmniDebuggerView : IDisposable
	{
		/// <summary>Raised when the user closed the panel from inside it.</summary>
		public event Action OnClosed;

		private const float OrientationHysteresis = 1.05f;

		private readonly IOmniDebugger _debugger;
		private readonly ICommandCatalog _catalog;
		private readonly IThemeRegistry _themeRegistry;
		private readonly ITabRegistry _tabRegistry;
		private readonly OmniDebuggerViewState _state;
		private readonly IViewPrefs _prefs;
		private readonly string _origin;
		private readonly VisualElement _host;
		private readonly VisualElement _root;
		private readonly VisualElement _panel;
		private readonly VisualElement _tabBar;
		private readonly VisualElement _tabBody;
		private readonly PanelChrome _chrome;
		private readonly ThemeApplier _themes;
		private readonly TabHost _tabHost;
		private readonly PopupLayer _popups;
		private readonly ViewServices _services;
		private readonly SafeArea _safeArea;
		private readonly WindowsHost _windows;

		/// <summary>Whether the panel is showing. A closed panel keeps its state but stops working.</summary>
		public bool IsOpen { get; private set; }

		/// <summary>
		/// The element the panel was built into — the one carrying the theme's style sheets. A gesture
		/// attaches its own controls here so they are themed too, and it stays in the tree while the
		/// panel is closed.
		/// </summary>
		public VisualElement Root => _root;

		/// <summary>The theme in use. Never null once the view is built.</summary>
		public OmniDebuggerTheme Theme => _themes.Theme;

		private ViewOrientation _orientation;
		private bool _landscape;
		private bool _orientationResolved;
		private bool _disposed;

		public OmniDebuggerView(in OmniDebuggerViewSettings settings)
		{
			MainThreadGuard.Verify(nameof(OmniDebuggerView));

			if (settings.Root == null)
			{
				throw new ArgumentNullException(nameof(settings), "A view needs a root element to build into.");
			}

			if (settings.Debugger == null)
			{
				throw new ArgumentNullException(nameof(settings), "A view needs a debugger to show.");
			}

			_host = settings.Root;
			_debugger = settings.Debugger;
			_state = settings.State ?? new OmniDebuggerViewState();
			_prefs = settings.Prefs;
			_origin = settings.Origin;
			_catalog = _debugger.Catalog;
			_themeRegistry = _debugger.Themes;
			_tabRegistry = _debugger.Tabs;

			_root = new VisualElement { name = OmniDebuggerUiClasses.Root, pickingMode = PickingMode.Ignore };
			_root.AddToClassList(OmniDebuggerUiClasses.Root);
			_themes = new ThemeApplier(_root);
			_popups = new PopupLayer();

			RestoreFavorites();

			_services = new ViewServices(
				_debugger,
				_origin,
				_state.Arguments,
				_state.Favorites,
				settings.HostWindows ? _state.Pins : null,
				_popups);

			if (settings.HostWindows && _debugger.Windows is WindowRegistry windowRegistry)
			{
				_windows = new WindowsHost(_root, _services, windowRegistry);
			}

			if (settings.ShowCloseButton)
			{
				VisualElement scrim = UiBuild.Element(OmniDebuggerUiClasses.Scrim);
				scrim.RegisterCallback<PointerUpEvent>(OnScrimTapped);
				_root.Add(scrim);
			}

			_panel = UiBuild.Element(OmniDebuggerUiClasses.Panel);
			_root.Add(_panel);

			_chrome = new PanelChrome(settings.ShowCloseButton, _popups);
			_chrome.OnThemeSelected += OnThemePicked;
			_chrome.OnCloseRequested += Close;
			_panel.Add(_chrome.Root);

			VisualElement content = UiBuild.Element(OmniDebuggerUiClasses.Content);
			_panel.Add(content);

			_tabBar = UiBuild.Element(OmniDebuggerUiClasses.TabBar);
			content.Add(_tabBar);

			_tabBody = UiBuild.Element(OmniDebuggerUiClasses.TabBody);
			content.Add(_tabBody);

			_root.Add(_popups);

			_tabHost = new TabHost(_tabBar, _tabBody, _debugger, _state, _origin, _services);
			_tabHost.OnSelectionChanged += OnTabSelectionChanged;
			_panel.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			_host.Add(_root);

			if (settings.UseScreenSafeArea)
			{
				_safeArea = new SafeArea(_root);
				_safeArea.OnChanged += ApplySafeArea;
				ApplySafeArea();
			}

			ApplyTheme(ResolveTheme());
			ApplyOrientation(_state.Landscape);

			_catalog.OnChanged += OnCatalogChanged;
			_themeRegistry.OnChanged += OnThemesChanged;
			_tabRegistry.OnChanged += OnTabsChanged;
			_state.Favorites.OnChanged += SaveFavorites;

			_tabHost.Rebuild();
			SetOpen(settings.StartOpen || _state.IsOpen, notify: false);
			RefreshThemePicker();
		}

		/// <summary>Shows the panel and resumes everything a closed panel stops doing.</summary>
		public void Open()
		{
			MainThreadGuard.Verify(nameof(Open));
			ThrowIfDisposed();
			SetOpen(true, notify: false);
		}

		/// <summary>Hides the panel, keeping its state. Raises <see cref="OnClosed"/>.</summary>
		public void Close()
		{
			MainThreadGuard.Verify(nameof(Close));
			ThrowIfDisposed();
			SetOpen(false, notify: true);
		}

		/// <summary>
		/// Switches the theme and saves the choice, so a runtime panel and an editor window end up with
		/// separate selections.
		/// </summary>
		public void SetTheme(OmniDebuggerTheme theme)
		{
			MainThreadGuard.Verify(nameof(SetTheme));
			ThrowIfDisposed();

			ApplyTheme(theme ?? _themeRegistry.Default);
			_prefs?.SetThemeId(Theme.Id);

			RefreshThemePicker();
		}

		/// <summary>
		/// Re-reads everything the panel shows. Called on its own whenever the catalog changes.
		/// </summary>
		public void Refresh()
		{
			MainThreadGuard.Verify(nameof(Refresh));
			ThrowIfDisposed();

			RefreshThemePicker();
			_tabHost.Refresh();
		}

		/// <summary>
		/// Tears the panel down and leaves the host element exactly as it was found. Safe to call
		/// more than once. Neither the debugger nor the state object is disposed — both belong to the
		/// caller.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			_catalog.OnChanged -= OnCatalogChanged;
			_themeRegistry.OnChanged -= OnThemesChanged;
			_tabRegistry.OnChanged -= OnTabsChanged;
			_state.Favorites.OnChanged -= SaveFavorites;

			_popups.Hide();
			_tabHost.OnSelectionChanged -= OnTabSelectionChanged;
			_tabHost.Dispose();
			_windows?.Dispose();
			_panel.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			if (_safeArea != null)
			{
				_safeArea.OnChanged -= ApplySafeArea;
				_safeArea.Dispose();
			}

			_chrome.OnThemeSelected -= OnThemePicked;
			_chrome.OnCloseRequested -= Close;
			_chrome.Dispose();

			_themes.Clear();
			_root.RemoveFromHierarchy();

			OnClosed = null;
		}

		internal void SetOrientation(ViewOrientation orientation)
		{
			MainThreadGuard.Verify(nameof(SetOrientation));
			ThrowIfDisposed();

			_orientation = orientation;

			if (orientation != ViewOrientation.Auto)
			{
				ApplyOrientation(orientation == ViewOrientation.Landscape);
				return;
			}

			_orientationResolved = false;
			ResolveOrientation(_panel.layout.width, _panel.layout.height);
		}

		private OmniDebuggerTheme ResolveTheme()
		{
			if (_themeRegistry.TryGet(_state.ThemeId, out OmniDebuggerTheme fromState))
			{
				return fromState;
			}

			if (_prefs != null &&
				_prefs.TryGetThemeId(out string storedId) &&
				_themeRegistry.TryGet(storedId, out OmniDebuggerTheme stored))
			{
				return stored;
			}

			if (!string.IsNullOrWhiteSpace(_state.ThemeId))
			{
				UnityLogSink.Default.Warning(
					$"Theme is unknown, falling back to the default. Id: {_state.ThemeId}.");
			}

			return _themeRegistry.Default;
		}

		private void ApplyTheme(OmniDebuggerTheme theme)
		{
			_themes.Apply(theme);
			_state.ThemeId = theme == null ? null : theme.Id;
		}

		private void RestoreFavorites()
		{
			if (_prefs == null || _state.Favorites.Count > 0)
			{
				return;
			}

			IReadOnlyList<string> keys = _prefs.GetFavorites();

			for (int i = 0; i < keys.Count; i++)
			{
				_state.Favorites.Add(keys[i]);
			}
		}

		private void SaveFavorites() => _prefs?.SetFavorites(_state.Favorites.Keys);

		private void RefreshThemePicker() => _chrome.SetThemes(_themeRegistry.All, Theme);

		private void SetOpen(bool open, bool notify)
		{
			IsOpen = open;
			_state.IsOpen = open;
			_root.EnableInClassList(OmniDebuggerUiClasses.RootClosed, !open);
			_tabHost.SetPanelOpen(open);
			_windows?.SetPanelOpen(open);

			if (open)
			{
				_tabHost.Refresh();
				return;
			}

			_popups.Hide();

			if (notify)
			{
				OnClosed?.Invoke();
			}
		}

		private void ApplyOrientation(bool landscape)
		{
			_landscape = landscape;
			_state.Landscape = landscape;
			_panel.EnableInClassList(OmniDebuggerUiClasses.PanelLandscape, landscape);
			_tabHost.SetVertical(landscape);
		}

		private void OnGeometryChanged(GeometryChangedEvent evt) =>
			ResolveOrientation(evt.newRect.width, evt.newRect.height);

		private void ResolveOrientation(float width, float height)
		{
			if (_orientation != ViewOrientation.Auto || !(width > 0.0f) || !(height > 0.0f))
			{
				return;
			}

			bool landscape;

			if (!_orientationResolved)
			{
				_orientationResolved = true;
				landscape = width > height;
			}
			else
			{
				landscape = _landscape
					? width * OrientationHysteresis > height
					: width > height * OrientationHysteresis;
			}

			if (landscape != _landscape)
			{
				ApplyOrientation(landscape);
			}
		}

		private void ApplySafeArea()
		{
			Vector4 insets = _safeArea.Insets;

			_panel.style.paddingLeft = insets.x;
			_panel.style.paddingRight = insets.y;
			_panel.style.paddingBottom = insets.w;
			_chrome.Root.style.paddingTop = insets.z;
			_windows?.SetInsets(insets);
		}

		private void OnScrimTapped(PointerUpEvent evt)
		{
			if (evt.target != evt.currentTarget)
			{
				return;
			}

			Close();
		}

		private void OnTabSelectionChanged() => _popups.Hide();

		private void OnThemePicked(OmniDebuggerTheme theme) => SetTheme(theme);

		private void OnThemesChanged()
		{
			bool follows = !_themeRegistry.TryGet(Theme.Id, out _) || !IsThemeChosen();

			if (follows && !ReferenceEquals(Theme, _themeRegistry.Default))
			{
				ApplyTheme(_themeRegistry.Default);
			}

			RefreshThemePicker();
		}

		private bool IsThemeChosen() =>
			_prefs == null || (_prefs.TryGetThemeId(out string id) && _themeRegistry.TryGet(id, out _));

		private void OnTabsChanged() => _tabHost.Rebuild();

		private void OnCatalogChanged() => Refresh();

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(OmniDebuggerView));
			}
		}
	}
}