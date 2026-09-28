using System;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// How the runtime panel is scaled, layered and opened. Shared by
	/// <see cref="OmniDebuggerOptions.Panel"/> and the <see cref="OmniDebuggerPanel"/> inspector.
	/// </summary>
	[Serializable]
	public sealed class OmniDebuggerPanelOptions
	{
		/// <summary>
		/// How panel units map to screen pixels. <see cref="OmniDebuggerScaleMode.Auto"/> by default.
		/// </summary>
		public OmniDebuggerScaleMode ScaleMode
		{
			get => _scaleMode;
			set => _scaleMode = value;
		}

		/// <summary>Multiplier on top of <see cref="ScaleMode"/>. Values at or below zero read as 1.</summary>
		public float Scale
		{
			get => _scale > 0.0f ? _scale : 1.0f;
			set => _scale = value;
		}

		/// <summary>Sorting order of the panel's UI Toolkit panel. 1000 by default.</summary>
		public float SortingOrder
		{
			get => _sortingOrder;
			set => _sortingOrder = value;
		}

		/// <summary>Opens the panel as soon as it is built.</summary>
		public bool OpenOnStart
		{
			get => _openOnStart;
			set => _openOnStart = value;
		}

		/// <summary>
		/// Fixes the panel to this theme: the theme switcher is hidden and no choice is saved. Null lets
		/// the user switch themes, restoring the last choice and then <see cref="IThemeRegistry.Default"/>.
		/// </summary>
		public OmniDebuggerTheme Theme
		{
			get => _theme;
			set => _theme = value;
		}

		/// <summary>The floating button and keyboard shortcuts. Never null.</summary>
		public OmniDebuggerOpenOptions Open
		{
			get => _open ??= new OmniDebuggerOpenOptions();
			set => _open = value;
		}

		[Tooltip("Auto scales with the screen on phones and tablets and keeps a physical size on desktop.")]
		[SerializeField] private OmniDebuggerScaleMode _scaleMode = OmniDebuggerScaleMode.Auto;

		[Tooltip("Multiplier on top of the scale mode. Above 1 makes everything bigger.")]
		[SerializeField] private float _scale = 1.0f;

		[Tooltip("Raise this above your own UI if the panel ends up behind it.")]
		[SerializeField] private float _sortingOrder = 1000.0f;

		[Tooltip("Opens the panel as soon as it is built.")]
		[SerializeField] private bool _openOnStart;

		[Tooltip("Set, the panel always uses this theme and hides the switcher. Left empty, the user picks a theme, restored from PlayerPrefs, then the default.")]
		[SerializeField] private OmniDebuggerTheme _theme;

		[SerializeField] private OmniDebuggerOpenOptions _open = new ();
	}
}