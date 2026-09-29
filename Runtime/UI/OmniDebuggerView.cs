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

		private const float OrientationHysteresis = 1.1f;
		private const long WindowScaleSaveDelayMs = 400;

		private readonly IOmniDebuggerHost _debugger;
		private readonly ICommandCatalog _catalog;
		private readonly IThemeRegistry _themeRegistry;
		private readonly ITabRegistry _tabRegistry;
		private readonly OmniDebuggerViewState _state;
		private readonly IViewPrefs _prefs;
		private readonly string _origin;
		private readonly bool _overlay;
		private readonly VisualElement _host;
		private readonly VisualElement _root;
		private readonly VisualElement _desk;
		private readonly VisualElement _stage;
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
		private readonly WindowRegistry _windowRegistry;
		private readonly IVisualElementScheduledItem _saveWindowScale;
		private readonly IVisualElementScheduledItem _pendingRefresh;
		private readonly FloatingPanel _floating;

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
		private bool _fullScreen;
		private bool _orientationResolved;
		private bool _windowScaleDirty;
		private bool _disposed;

		private bool IsFloating => _overlay && _landscape && !_fullScreen;

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
			_overlay = settings.ShowCloseButton;
			_catalog = _debugger.Catalog;
			_themeRegistry = _debugger.Themes;
			_tabRegistry = _debugger.Tabs;

			_root = new VisualElement { name = OmniDebuggerUiClasses.Root, pickingMode = PickingMode.Ignore };
			_root.AddToClassList(OmniDebuggerUiClasses.Root);
			_root.EnableInClassList(OmniDebuggerUiClasses.RootOverlay, _overlay);
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

			_desk = UiBuild.Element(OmniDebuggerUiClasses.Desk);
			_desk.pickingMode = PickingMode.Ignore;
			_root.Add(_desk);

			_stage = UiBuild.Element(OmniDebuggerUiClasses.Stage);
			_stage.pickingMode = PickingMode.Ignore;
			_desk.Add(_stage);

			_panel = UiBuild.Element(OmniDebuggerUiClasses.Panel);
			_panel.AddManipulator(new Halo());
			_panel.RegisterCallback<PointerDownEvent>(OnPanelPointerDown, TrickleDown.TrickleDown);
			_stage.Add(_panel);

			if (settings.HostWindows && _debugger.Windows is WindowRegistry windowRegistry)
			{
				_windowRegistry = windowRegistry;
				RestoreWindowScale();
				_windows = new WindowsHost(_desk, _services, windowRegistry);
				_saveWindowScale = _root.schedule.Execute(SaveWindowScale);
				_saveWindowScale.Pause();
				_windowRegistry.OnScaleChanged += OnWindowScaleChanged;
			}

			string version = _debugger is OmniDebuggerHost debuggerHost ? debuggerHost.Version.ToString() : null;
			_chrome = new PanelChrome(_overlay, _popups, _debugger.Icons, version);
			_chrome.OnThemeSelected += OnThemePicked;
			_chrome.OnCloseRequested += Close;

			VisualElement rail = UiBuild.Element(OmniDebuggerUiClasses.Rail);
			_panel.Add(rail);
			rail.Add(_chrome.Brand);

			_tabBar = UiBuild.Element(OmniDebuggerUiClasses.TabBar);
			rail.Add(_tabBar);
			rail.Add(_chrome.Footer);

			VisualElement main = UiBuild.Element(OmniDebuggerUiClasses.Main);
			_panel.Add(main);
			main.Add(_chrome.PageBar);

			_tabBody = UiBuild.Element(OmniDebuggerUiClasses.TabBody);
			main.Add(_tabBody);

			_floating = new FloatingPanel(_stage, _panel, _state);
			_floating.AddHandle(_chrome.Brand);
			_floating.AddHandle(_chrome.PageBar);

			_root.Add(_popups);

			_tabHost = new TabHost(_tabBar, _tabBody, _debugger, _state, _origin, _services);
			_tabHost.OnSelectionChanged += OnTabSelectionChanged;
			_stage.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			_host.Add(_root);

			if (settings.UseScreenSafeArea)
			{
				_safeArea = new SafeArea(_root);
				_safeArea.OnChanged += ApplySafeArea;
				ApplySafeArea();
			}

			ApplyTheme(ResolveTheme());
			ApplyOrientation(_state.Landscape);

			_pendingRefresh = _root.schedule.Execute(Refresh);
			_pendingRefresh.Pause();

			_debugger.OnRefreshRequested += OnRefreshRequested;
			_catalog.OnChanged += OnCatalogChanged;
			_themeRegistry.OnChanged += OnThemesChanged;
			_tabRegistry.OnChanged += OnTabsChanged;
			_state.Favorites.OnChanged += SaveFavorites;

			_tabHost.Rebuild();
			RefreshPage();
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
		/// Re-reads and redraws everything the view shows: the selected tab and the floating windows.
		/// Called on its own, once per frame at most, whenever <see cref="IOmniDebuggerHost.Refresh"/> is.
		/// </summary>
		public void Refresh()
		{
			MainThreadGuard.Verify(nameof(Refresh));
			ThrowIfDisposed();

			RefreshTabs();
			_windows?.Refresh();
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

			_pendingRefresh.Pause();
			_debugger.OnRefreshRequested -= OnRefreshRequested;
			_catalog.OnChanged -= OnCatalogChanged;
			_themeRegistry.OnChanged -= OnThemesChanged;
			_tabRegistry.OnChanged -= OnTabsChanged;
			_state.Favorites.OnChanged -= SaveFavorites;

			_popups.Hide();
			_tabHost.OnSelectionChanged -= OnTabSelectionChanged;
			_tabHost.Dispose();

			if (_windowRegistry != null)
			{
				_windowRegistry.OnScaleChanged -= OnWindowScaleChanged;
				_saveWindowScale.Pause();

				if (_windowScaleDirty)
				{
					SaveWindowScale();
				}
			}

			_windows?.Dispose();
			_floating.Dispose();
			_panel.UnregisterCallback<PointerDownEvent>(OnPanelPointerDown, TrickleDown.TrickleDown);
			_stage.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

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
			ResolveOrientation(_stage.layout.width, _stage.layout.height);
		}

		internal void SetLandscapeLayout(OmniDebuggerLandscapeLayout layout)
		{
			MainThreadGuard.Verify(nameof(SetLandscapeLayout));
			ThrowIfDisposed();

			_fullScreen = layout == OmniDebuggerLandscapeLayout.FullScreen;
			ApplyFrame();
		}

		internal void SetFloatingScale(float scale)
		{
			MainThreadGuard.Verify(nameof(SetFloatingScale));
			ThrowIfDisposed();

			_floating.SetScale(scale);
		}

		internal void SetShortcutHint(string hint)
		{
			MainThreadGuard.Verify(nameof(SetShortcutHint));
			ThrowIfDisposed();

			_chrome.SetShortcutHint(hint);
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

		private void RestoreWindowScale()
		{
			if (_prefs != null && _prefs.TryGetWindowScale(out float scale))
			{
				_windowRegistry.SetScale(scale);
			}
		}

		private void OnWindowScaleChanged()
		{
			_windowScaleDirty = true;
			_saveWindowScale.ExecuteLater(WindowScaleSaveDelayMs);
		}

		private void SaveWindowScale()
		{
			_windowScaleDirty = false;
			_prefs?.SetWindowScale(_windowRegistry.Scale);
		}

		private void RefreshTabs()
		{
			RefreshThemePicker();
			_tabHost.Refresh();
		}

		private void RefreshThemePicker() => _chrome.SetThemes(_themeRegistry.All, Theme);

		private void RefreshPage() =>
			_chrome.SetPage(_tabHost.TryGetSelectedFactory(out IOmniDebuggerTabFactory factory) ? factory : null);

		private void SetOpen(bool open, bool notify)
		{
			IsOpen = open;
			_state.IsOpen = open;
			_root.EnableInClassList(OmniDebuggerUiClasses.RootClosed, !open);
			_tabHost.SetPanelOpen(open);
			RefreshWindows();

			if (open)
			{
				_stage.SendToBack();
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
			_root.EnableInClassList(OmniDebuggerUiClasses.RootLandscape, landscape);
			_tabHost.SetVertical(landscape);
			_chrome.SetLandscape(landscape);
			ApplyFrame();
		}

		private void ApplyFrame()
		{
			bool floating = IsFloating;
			_root.EnableInClassList(OmniDebuggerUiClasses.RootFloating, floating);
			_floating.SetActive(floating);
			ApplySafeArea();
			RefreshWindows();
		}

		private void RefreshWindows() => _windows?.SetVisible(!IsOpen || IsFloating);

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
			if (_safeArea == null)
			{
				return;
			}

			Vector4 insets = _safeArea.Insets;
			bool edgeToEdge = !IsFloating;
			Vector4 stage = edgeToEdge ? Vector4.zero : insets;
			Vector4 panel = edgeToEdge ? insets : Vector4.zero;

			_stage.style.left = stage.x;
			_stage.style.right = stage.y;
			_stage.style.top = stage.z;
			_stage.style.bottom = stage.w;

			_panel.style.paddingLeft = panel.x;
			_panel.style.paddingRight = panel.y;
			_panel.style.paddingTop = panel.z;
			_panel.style.paddingBottom = panel.w;

			_windows?.SetInsets(insets);
		}

		private void OnPanelPointerDown(PointerDownEvent evt)
		{
			if (_windows != null && IsFloating)
			{
				_stage.BringToFront();
			}
		}

		private void OnTabSelectionChanged()
		{
			_popups.Hide();
			RefreshPage();
		}

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

		private void OnTabsChanged()
		{
			_tabHost.Rebuild();
			RefreshPage();
		}

		private void OnCatalogChanged() => RefreshTabs();

		private void OnRefreshRequested() => _pendingRefresh.ExecuteLater(0);

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(OmniDebuggerView));
			}
		}
	}
}