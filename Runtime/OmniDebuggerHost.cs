using System;
using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using UnityEngine;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The debugger core. Either the game constructs it, holds it and disposes it, or it reads
	/// <see cref="Shared"/> and leaves the lifetime to the package. In play mode it also puts the panel on
	/// screen and takes it down again, set up as <c>Project Settings → DTech → OmniDebugger → Panel</c>
	/// says — see <see cref="OmniDebuggerOptions"/>.
	/// </summary>
	public sealed class OmniDebuggerHost : IOmniDebuggerHost
	{
		/// <inheritdoc/>
		public event Action OnRefreshRequested;

		private static readonly Version _version = new (2, 1, 0);
		private static readonly List<OmniDebuggerHost> _alive = new ();

		private readonly CommandRegistry _commands;
		private readonly Hotbar _hotbar;
		private readonly GroupOrder _groups;
		private readonly LogStore _logs;
		private readonly InfoRegistry _info;
		private readonly TabRegistry _tabs;
		private readonly ThemeCatalog _themes;
		private readonly IconRegistry _icons;
		private readonly ArgumentFieldRegistry _fields;
		private readonly bool _followsProject;

		/// <summary>
		/// The app's debugger: the first one built and not disposed yet — by the game's code or by
		/// <see cref="OmniDebuggerOptions.CreateOnStartup"/> — and, while there is none, a new one built right
		/// here from the project settings, the way the parameterless constructor builds it. Main thread only.
		/// </summary>
		public static OmniDebuggerHost Shared
		{
			get
			{
				MainThreadGuard.Verify(nameof(Shared));

				if (_shared == null)
				{
					OmniDebuggerHost created = new OmniDebuggerHost();
					_ownsShared = ReferenceEquals(_shared, created);
				}

				return _shared;
			}
		}

		/// <inheritdoc/>
		public ICommandRegistry Commands
		{
			get
			{
				ThrowIfDisposed();
				return _commands;
			}
		}

		/// <inheritdoc/>
		public IHotbar Hotbar
		{
			get
			{
				ThrowIfDisposed();
				return _hotbar;
			}
		}

		/// <inheritdoc/>
		public IGroupOrder Groups
		{
			get
			{
				ThrowIfDisposed();
				return _groups;
			}
		}

		/// <inheritdoc/>
		public ILogFeed Logs
		{
			get
			{
				ThrowIfDisposed();
				return _logs;
			}
		}

		/// <inheritdoc/>
		public IInfoRegistry Info
		{
			get
			{
				ThrowIfDisposed();
				return _info;
			}
		}

		/// <inheritdoc/>
		public ITabRegistry Tabs
		{
			get
			{
				ThrowIfDisposed();
				return _tabs;
			}
		}

		/// <inheritdoc/>
		public IThemeRegistry Themes
		{
			get
			{
				ThrowIfDisposed();
				return _themes;
			}
		}

		/// <inheritdoc/>
		public IIconRegistry Icons
		{
			get
			{
				ThrowIfDisposed();
				return _icons;
			}
		}

		/// <inheritdoc/>
		public IArgumentFieldRegistry Fields
		{
			get
			{
				ThrowIfDisposed();
				return _fields;
			}
		}

		/// <summary>
		/// The runtime panel built together with the debugger, or null when
		/// <see cref="OmniDebuggerOptions.CreatePanel"/> was off or the debugger was built outside
		/// play mode.
		/// </summary>
		public OmniDebuggerPanel Panel => _disposed || _panel == null ? null : _panel;

		internal Version Version => _version;

		private static OmniDebuggerHost _shared;
		private static bool _ownsShared;

		private OmniDebuggerOptions _options;
		private OmniDebuggerPanel _panel;
		private bool _disposed;

		/// <summary>
		/// Builds the debugger with <see cref="OmniDebuggerOptions.Default"/> — the project's options from
		/// <c>Project Settings → DTech → OmniDebugger → Panel</c>. In play mode it keeps following them:
		/// edits made there apply to the running debugger and its panel at once.
		/// </summary>
		public OmniDebuggerHost() : this(UnityLogSink.Default, null)
		{
		}

		/// <param name="options">
		/// Used instead of the project settings, as a whole. Start from
		/// <see cref="OmniDebuggerOptions.Default"/> to change only a few of them. Null reads as
		/// <see cref="OmniDebuggerOptions.Default"/> and keeps following the project settings, as the
		/// parameterless constructor does; options given here are never touched by them.
		/// </param>
		public OmniDebuggerHost(OmniDebuggerOptions options) : this(UnityLogSink.Default, options)
		{
		}

		internal OmniDebuggerHost(ILogSink log) : this(log, new OmniDebuggerOptions { CreatePanel = false })
		{
		}

		internal OmniDebuggerHost(ILogSink log, OmniDebuggerOptions options)
		{
			if (log == null)
			{
				throw new ArgumentNullException(nameof(log));
			}

			MainThreadGuard.Verify(nameof(OmniDebuggerHost));

			_followsProject = options == null;
			_options = options ?? OmniDebuggerOptions.Default;

			_groups = new GroupOrder();
			_commands = new CommandRegistry(log);
			_hotbar = new Hotbar();
			_logs = new LogStore();
			_logs.StartCapture();
			_info = new InfoRegistry(log);
			_tabs = new TabRegistry(log);
			_themes = new ThemeCatalog(log);
			_icons = new IconRegistry(log);
			_fields = new ArgumentFieldRegistry(log);

			RegisterAssets(_options);
			OmniDebuggerViews.Register(this);

			if (_options.CreatePanel)
			{
				_panel = PanelLauncher.Launch(this, _followsProject ? null : _options.Panel);
			}

			if (_followsProject)
			{
				ProjectOptions.OnEditorChanged += OnProjectOptionsChanged;
			}

			_alive.Add(this);

			if (_shared == null)
			{
				_shared = this;
			}
			else if (_panel != null && _shared._panel != null)
			{
				log.Warning(
					"Another debugger already shows its panel, so two panels are on screen now. Read " +
					$"{nameof(OmniDebuggerHost)}.{nameof(Shared)} instead of building a second debugger, or turn off " +
					"'Create On Startup' in Project Settings → DTech → OmniDebugger → Panel.");
			}
		}

		/// <summary>
		/// Reads <see cref="Shared"/> without building it: false while no debugger is alive. Main thread only.
		/// </summary>
		public static bool TryGetShared(out OmniDebuggerHost debugger)
		{
			MainThreadGuard.Verify(nameof(TryGetShared));

			debugger = _shared;
			return debugger != null;
		}

		/// <inheritdoc/>
		public void Refresh()
		{
			MainThreadGuard.Verify(nameof(Refresh));
			ThrowIfDisposed();

			OnRefreshRequested?.Invoke();
		}

		/// <summary>
		/// Releases every registered object — MonoBehaviours included — empties every registry and
		/// clears their <c>OnChanged</c> subscriber lists, and the one of <see cref="OnRefreshRequested"/>.
		/// Every member throws <see cref="ObjectDisposedException"/> afterwards. Disposing
		/// <see cref="Shared"/> hands its slot to the oldest debugger still alive, or empties it. Safe to call
		/// more than once.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_alive.Remove(this);

			if (ReferenceEquals(_shared, this))
			{
				_shared = _alive.Count > 0 ? _alive[0] : null;
				_ownsShared = false;
			}

			if (_followsProject)
			{
				ProjectOptions.OnEditorChanged -= OnProjectOptionsChanged;
			}

			PanelLauncher.Release(_panel);
			_panel = null;
			OmniDebuggerViews.Unregister(this);
			_logs.StopCapture();

			_commands.Clear();
			_hotbar.Release();
			_info.Clear();
			_groups.Clear();
			_tabs.Clear();
			_themes.Clear();
			_icons.Clear();
			_fields.Clear();

			OnRefreshRequested = null;
		}

		internal static void ReleaseShared()
		{
			if (_shared != null && _ownsShared)
			{
				_shared.Dispose();
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetShared()
		{
			ReleaseShared();
			_alive.Clear();
			_shared = null;
			_ownsShared = false;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void CreateSharedOnStartup()
		{
			if (_shared == null && ProjectOptions.Load().CreateOnStartup)
			{
				_ = Shared;
			}
		}

		private static bool Contains<T>(List<T> items, T item) where T : class
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (ReferenceEquals(items[i], item))
				{
					return true;
				}
			}

			return false;
		}

		private void RegisterAssets(OmniDebuggerOptions options)
		{
			for (int i = 0; i < options.Themes.Count; i++)
			{
				OmniDebuggerTheme theme = options.Themes[i];

				if (theme != null)
				{
					_themes.Register(theme);
				}
			}

			_themes.SetDefault(options.DefaultTheme);
		}

		private void UnregisterDroppedAssets(OmniDebuggerOptions previous, OmniDebuggerOptions next)
		{
			for (int i = 0; i < previous.Themes.Count; i++)
			{
				OmniDebuggerTheme theme = previous.Themes[i];

				if (theme != null && !Contains(next.Themes, theme))
				{
					_themes.Unregister(theme);
				}
			}
		}

		private void OnProjectOptionsChanged()
		{
			if (_disposed)
			{
				return;
			}

			OmniDebuggerOptions next = ProjectOptions.Load();

			UnregisterDroppedAssets(_options, next);
			RegisterAssets(next);

			_options = next;

			if (next.CreatePanel && _panel == null)
			{
				_panel = PanelLauncher.Launch(this, null);
			}
			else if (!next.CreatePanel && _panel != null)
			{
				PanelLauncher.Release(_panel);
				_panel = null;
			}
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(OmniDebuggerHost));
			}
		}
	}
}