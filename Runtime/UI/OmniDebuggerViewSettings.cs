using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Everything an <see cref="OmniDebuggerView"/> needs to know about the place it is built into.
	/// The runtime panel and the editor window differ only in the values they pass here.
	/// </summary>
	public readonly struct OmniDebuggerViewSettings
	{
		/// <summary>Origin reported to <see cref="ICommandInvoker"/> when none was given.</summary>
		public const string DefaultOrigin = "Panel";

		private readonly string _origin;

		/// <summary>
		/// The element the panel is built into. The view adds exactly one child and touches nothing
		/// else, so an editor window's root can be shared with the window's own chrome.
		/// </summary>
		public VisualElement Root { get; }

		/// <summary>The debugger whose commands are shown. The view never disposes it.</summary>
		public IOmniDebuggerHost Debugger { get; }

		/// <summary>
		/// State that outlives the elements. Null starts from scratch; pass the same instance again
		/// to keep the selected tab, group, search, theme and typed arguments across a rebuild. With
		/// preferences to save to, the theme, the typed arguments and the hotbar survive a restart too.
		/// </summary>
		public OmniDebuggerViewState State { get; }

		/// <summary>
		/// Value put into <see cref="InvocationRequest.Origin"/>, so the log says which panel ran a
		/// command. Defaults to <see cref="DefaultOrigin"/>.
		/// </summary>
		public string Origin => string.IsNullOrWhiteSpace(_origin) ? DefaultOrigin : _origin;

		/// <summary>Pads the panel out of the notch and the home indicator. Runtime only.</summary>
		public bool UseScreenSafeArea { get; }

		/// <summary>
		/// Adds a close button and draws the panel as glass over the game: edge to edge, or a floating
		/// window the game stays live around. False in an editor window, where the window's own tab
		/// closes the panel and the panel fills the window, opaque.
		/// </summary>
		public bool ShowCloseButton { get; }

		/// <summary>
		/// Opens the panel as soon as it is built, whatever <see cref="State"/> remembers. True for an
		/// editor window, which has no other way in; false for a runtime panel behind a gesture.
		/// </summary>
		public bool StartOpen { get; }

		/// <summary>
		/// Shows the hotbar and the floating Info sections while the panel is closed or floating, and lets the
		/// Info tab float its sections. Runtime only: an editor window has no game view to float them over.
		/// </summary>
		public bool HostOverlays { get; }

		internal IViewPrefs Prefs { get; }

		public OmniDebuggerViewSettings(
			VisualElement root,
			IOmniDebuggerHost debugger,
			OmniDebuggerViewState state = null,
			string origin = null,
			bool useScreenSafeArea = false,
			bool showCloseButton = false,
			bool startOpen = false,
			bool hostOverlays = false)
			: this(root, debugger, state, null, origin, useScreenSafeArea, showCloseButton, startOpen, hostOverlays)
		{
		}

		internal OmniDebuggerViewSettings(
			VisualElement root,
			IOmniDebuggerHost debugger,
			OmniDebuggerViewState state,
			IViewPrefs prefs,
			string origin = null,
			bool useScreenSafeArea = false,
			bool showCloseButton = false,
			bool startOpen = false,
			bool hostOverlays = false)
		{
			Root = root;
			Debugger = debugger;
			State = state;
			Prefs = prefs;
			_origin = origin;
			UseScreenSafeArea = useScreenSafeArea;
			ShowCloseButton = showCloseButton;
			StartOpen = startOpen;
			HostOverlays = hostOverlays;
		}
	}
}