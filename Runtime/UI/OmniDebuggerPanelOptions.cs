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
				_openOnStart = _openOnStart,
				_panelSettings = _panelSettings,
				_open = Open.Clone(),
				_lock = Lock.Clone(),
			};
		}
	}
}