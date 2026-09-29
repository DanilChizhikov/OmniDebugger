using System;
using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using UnityEngine;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// How an <see cref="OmniDebugger"/> sets itself up. The project's own copy is edited in
	/// <c>Project Settings → DTech → OmniDebugger → Panel</c> and read through <see cref="Default"/>;
	/// a new instance holds the package's built-in defaults instead.
	/// </summary>
	[Serializable]
	public sealed class OmniDebuggerOptions
	{
		/// <summary>
		/// Builds <see cref="OmniDebugger.Shared"/> before the first scene loads, so the log is captured from
		/// the first frame and the panel is there without a line of code. Read from the project settings when
		/// the game starts; a debugger built in code ignores it. Off by default.
		/// </summary>
		public bool CreateOnStartup
		{
			get => _createOnStartup;
			set => _createOnStartup = value;
		}

		/// <summary>
		/// Builds the runtime panel together with the debugger and destroys it on
		/// <see cref="OmniDebugger.Dispose"/>. Turn it off when a scene already carries an
		/// <see cref="OmniDebuggerPanel"/>, or when the debugger is only meant for the editor window.
		/// Nothing is built outside play mode either way.
		/// </summary>
		public bool CreatePanel
		{
			get => _createPanel;
			set => _createPanel = value;
		}

		/// <summary>How the runtime panel looks and opens. Never null.</summary>
		public OmniDebuggerPanelOptions Panel
		{
			get => _panel ??= new OmniDebuggerPanelOptions();
			set => _panel = value;
		}

		/// <summary>
		/// The theme a panel starts with while nothing was picked yet. Alone, it replaces the built-in
		/// dark and light themes and the panel hides its switcher; with <see cref="Themes"/> listed, the
		/// user can still switch away from it. Null keeps the built-in dark theme.
		/// </summary>
		public OmniDebuggerTheme DefaultTheme
		{
			get => _defaultTheme;
			set => _defaultTheme = value;
		}

		/// <summary>
		/// Themes registered with <see cref="IOmniDebugger.Themes"/> when the debugger is built. Offered
		/// next to the built-in dark and light themes, so the panel's theme button becomes a dropdown.
		/// Never null; null entries are skipped.
		/// </summary>
		public List<OmniDebuggerTheme> Themes
		{
			get => _themes ??= new List<OmniDebuggerTheme>();
			set => _themes = value;
		}

		/// <summary>
		/// Icon catalogs added to <see cref="IOmniDebugger.Icons"/> when the debugger is built, for
		/// assets kept outside <c>Resources/OmniDebugger</c>. Never null; null entries are skipped.
		/// </summary>
		public List<OmniDebuggerIconCatalog> IconCatalogs
		{
			get => _iconCatalogs ??= new List<OmniDebuggerIconCatalog>();
			set => _iconCatalogs = value;
		}

		/// <summary>
		/// A copy of the project's options from <c>Project Settings → DTech → OmniDebugger → Panel</c>, so
		/// changing it touches nothing else. Play mode in the editor reads the settings live; a player reads
		/// the snapshot taken when it was built. Falls back to the built-in defaults when neither exists.
		/// Only a debugger built without options keeps following later edits in play mode.
		/// </summary>
		public static OmniDebuggerOptions Default => ProjectOptions.Load();

		[Tooltip("Builds OmniDebugger.Shared before the first scene loads: the log from the first frame and the panel without any code.")]
		[SerializeField] private bool _createOnStartup;

		[Tooltip("Builds the runtime panel together with the debugger. Ignored outside play mode.")]
		[SerializeField] private bool _createPanel = true;

		[SerializeField] private OmniDebuggerPanelOptions _panel = new ();

		[Tooltip("The theme a panel starts with. Alone, it replaces the built-in dark and light themes and hides the switcher. Left empty, the built-in dark theme.")]
		[SerializeField] private OmniDebuggerTheme _defaultTheme;

		[Tooltip("Themes offered next to the built-in dark and light ones, picked from a dropdown.")]
		[SerializeField] private List<OmniDebuggerTheme> _themes = new ();

		[Tooltip("Icon catalogs used in addition to those found in Resources/OmniDebugger folders.")]
		[SerializeField] private List<OmniDebuggerIconCatalog> _iconCatalogs = new ();

		internal OmniDebuggerOptions Clone()
		{
			return new OmniDebuggerOptions
			{
				_createOnStartup = _createOnStartup,
				_createPanel = _createPanel,
				_panel = Panel.Clone(),
				_defaultTheme = _defaultTheme,
				_themes = new List<OmniDebuggerTheme>(Themes),
				_iconCatalogs = new List<OmniDebuggerIconCatalog>(IconCatalogs),
			};
		}
	}
}
