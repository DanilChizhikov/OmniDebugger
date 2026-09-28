using System;
using DTech.OmniDebugger.UI;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The debugger core. The game constructs it, holds it, and disposes it. In play mode it also
	/// puts the panel on screen and takes it down again — see <see cref="OmniDebuggerOptions"/>.
	/// </summary>
	public sealed class OmniDebugger : IOmniDebugger
	{
		private readonly CommandCatalog _catalog;
		private readonly CommandInvoker _invoker;
		private readonly GroupOrder _groups;
		private readonly LogStore _logs;
		private readonly LogCapture _capture;
		private readonly WindowRegistry _windows;
		private readonly TabRegistry _tabs;
		private readonly ThemeCatalog _themes;
		private readonly IconRegistry _icons;
		private readonly ArgumentFieldRegistry _fields;
		private readonly OmniDebuggerPanel _panel;

		/// <inheritdoc/>
		public ICommandCatalog Catalog
		{
			get
			{
				ThrowIfDisposed();
				return _catalog;
			}
		}

		/// <inheritdoc/>
		public ICommandInvoker Commands
		{
			get
			{
				ThrowIfDisposed();
				return _invoker;
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
		public IWindowRegistry Windows
		{
			get
			{
				ThrowIfDisposed();
				return _windows;
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

		private bool _disposed;

		/// <summary>Builds the debugger with <see cref="OmniDebuggerOptions.Default"/>.</summary>
		public OmniDebugger() : this(UnityLogSink.Default, OmniDebuggerOptions.Default)
		{
		}

		/// <param name="options">Null reads as <see cref="OmniDebuggerOptions.Default"/>.</param>
		public OmniDebugger(OmniDebuggerOptions options) : this(UnityLogSink.Default, options)
		{
		}

		internal OmniDebugger(ILogSink log) : this(log, new OmniDebuggerOptions { CreatePanel = false })
		{
		}

		internal OmniDebugger(ILogSink log, OmniDebuggerOptions options)
		{
			if (log == null)
			{
				throw new ArgumentNullException(nameof(log));
			}

			MainThreadGuard.Verify(nameof(OmniDebugger));

			options ??= OmniDebuggerOptions.Default;

			_groups = new GroupOrder();
			_catalog = new CommandCatalog(log);
			_invoker = new CommandInvoker(_catalog, log);
			_logs = new LogStore();
			_capture = new LogCapture(_logs);
			_windows = new WindowRegistry();
			_tabs = new TabRegistry(log);
			_themes = new ThemeCatalog(log);
			_icons = new IconRegistry(log);
			_fields = new ArgumentFieldRegistry(log);

			OmniDebuggerViews.Register(this);

			if (options.CreatePanel)
			{
				_panel = PanelLauncher.Launch(this, options.Panel);
			}
		}

		/// <summary>
		/// Releases every registered source — MonoBehaviours included — empties every registry and
		/// clears their <c>OnChanged</c> subscriber lists. Every member throws
		/// <see cref="ObjectDisposedException"/> afterwards. Safe to call more than once.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			PanelLauncher.Release(_panel);
			OmniDebuggerViews.Unregister(this);
			_capture.Dispose();

			_catalog.Clear();
			_groups.Clear();
			_windows.Clear();
			_tabs.Clear();
			_themes.Clear();
			_icons.Clear();
			_fields.Clear();
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(OmniDebugger));
			}
		}
	}
}