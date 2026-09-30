using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// How the runtime panel is scaled, layered, skinned, opened and locked. Read through
	/// <see cref="OmniDebuggerOptions.Panel"/> and by <see cref="OmniDebuggerPanel.Options"/>.
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

		/// <summary>
		/// How the panel sits on a landscape screen: a floating window over the game, or edge to edge.
		/// <see cref="OmniDebuggerLandscapeLayout.Floating"/> by default. Read when the panel is built
		/// and whenever the project settings are edited in play mode.
		/// </summary>
		public OmniDebuggerLandscapeLayout LandscapeLayout
		{
			get => _landscapeLayout;
			set => _landscapeLayout = value;
		}

		/// <summary>
		/// Scale of the floating panel, on top of <see cref="Scale"/>: 1 is the compact default window, below 1
		/// makes it smaller, above 1 bigger, never past the screen. 1 by default; values at or below zero read
		/// as 1. Only the floating window scales — edge to edge, the panel ignores it. Read like
		/// <see cref="LandscapeLayout"/>.
		/// </summary>
		public float FloatingScale
		{
			get => _floatingScale > 0.0f ? _floatingScale : 1.0f;
			set => _floatingScale = value;
		}

		/// <summary>
		/// The edge of the screen the hotbar — the commands pinned from the Commands tab — runs along.
		/// <see cref="OmniDebuggerHotbarEdge.Bottom"/> by default.
		/// </summary>
		public OmniDebuggerHotbarEdge HotbarEdge
		{
			get => _hotbarEdge;
			set => _hotbarEdge = value;
		}

		/// <summary>Opens the panel as soon as it is built.</summary>
		public bool OpenOnStart
		{
			get => _openOnStart;
			set => _openOnStart = value;
		}

		/// <summary>
		/// Panel settings the runtime panel starts from. Always cloned, never edited: the clone gets
		/// <see cref="SortingOrder"/> and the scaling on top. Null uses the asset shipped with the package.
		/// </summary>
		public PanelSettings PanelSettings
		{
			get => _panelSettings;
			set => _panelSettings = value;
		}

		/// <summary>The floating button and keyboard shortcuts. Never null.</summary>
		public OmniDebuggerOpenOptions Open
		{
			get => _open ??= new OmniDebuggerOpenOptions();
			set => _open = value;
		}

		/// <summary>
		/// The PIN or password asked for before the panel shows. Never null. Read every time the panel
		/// opens, so changes apply on the next open.
		/// </summary>
		public OmniDebuggerLockOptions Lock
		{
			get => _lock ??= new OmniDebuggerLockOptions();
			set => _lock = value;
		}

		[Tooltip("Auto scales with the screen on phones and tablets and keeps a physical size on desktop.")]
		[SerializeField] private OmniDebuggerScaleMode _scaleMode = OmniDebuggerScaleMode.Auto;

		[Tooltip("Multiplier on top of the scale mode. Above 1 makes everything bigger.")]
		[SerializeField, Min(0.1f)] private float _scale = 1.0f;

		[Tooltip("Raise this above your own UI if the panel ends up behind it.")]
		[SerializeField, Min(0f)] private float _sortingOrder = 1000.0f;

		[Tooltip("How the panel sits on a landscape screen. Floating keeps the game visible and playable around a window you can drag by its top bar; Full Screen goes edge to edge. Portrait always goes edge to edge.")]
		[SerializeField] private OmniDebuggerLandscapeLayout _landscapeLayout = OmniDebuggerLandscapeLayout.Floating;

		[Tooltip("Scale of the floating window on top of Scale. 1 is the compact default window; below 1 makes it smaller, above 1 bigger; it never grows past the screen. Full Screen and portrait ignore it.")]
		[SerializeField, Range(0.5f, 2.0f)] private float _floatingScale = 1.0f;

		[Tooltip("The edge of the screen the hotbar of pinned commands runs along.")]
		[SerializeField] private OmniDebuggerHotbarEdge _hotbarEdge = OmniDebuggerHotbarEdge.Bottom;

		[Tooltip("Opens the panel as soon as it is built.")]
		[SerializeField] private bool _openOnStart;

		[Tooltip("Left empty, the settings shipped with the package are used. Always cloned, never edited.")]
		[SerializeField] private PanelSettings _panelSettings;

		[SerializeField] private OmniDebuggerOpenOptions _open = new ();

		[SerializeField] private OmniDebuggerLockOptions _lock = new ();

		internal OmniDebuggerPanelOptions Clone()
		{
			return new OmniDebuggerPanelOptions
			{
				_scaleMode = _scaleMode,
				_scale = _scale,
				_sortingOrder = _sortingOrder,
				_landscapeLayout = _landscapeLayout,
				_floatingScale = _floatingScale,
				_hotbarEdge = _hotbarEdge,
				_openOnStart = _openOnStart,
				_panelSettings = _panelSettings,
				_open = Open.Clone(),
				_lock = Lock.Clone(),
			};
		}
	}
}