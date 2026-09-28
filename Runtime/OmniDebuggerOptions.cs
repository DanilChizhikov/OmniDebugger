using System;
using DTech.OmniDebugger.UI;
using UnityEngine;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// How an <see cref="OmniDebugger"/> sets itself up. The defaults put a panel on screen as soon as
	/// the debugger is constructed in play mode; nothing has to be added to a scene.
	/// </summary>
	[Serializable]
	public sealed class OmniDebuggerOptions
	{
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

		/// <summary>Fresh defaults: a panel, scaled for the platform, opened with the floating button.</summary>
		public static OmniDebuggerOptions Default => new ();

		[Tooltip("Builds the runtime panel together with the debugger. Ignored outside play mode.")]
		[SerializeField] private bool _createPanel = true;

		[SerializeField] private OmniDebuggerPanelOptions _panel = new ();
	}
}