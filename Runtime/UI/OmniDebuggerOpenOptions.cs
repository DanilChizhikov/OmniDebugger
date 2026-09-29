using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Ways into the runtime panel: the floating button and keyboard shortcuts. Both can be used
	/// together; turn the button off and a shortcut is the only way in.
	/// </summary>
	[Serializable]
	public sealed class OmniDebuggerOpenOptions
	{
		public const int MaxButtonClicks = 10;
		public const float MinButtonOpacity = 0f;

		/// <summary>Shows the floating button. True by default.</summary>
		public bool ButtonEnabled
		{
			get => _buttonEnabled;
			set => _buttonEnabled = value;
		}

		/// <summary>
		/// Taps in a row it takes to open the panel, clamped to 1..<see cref="MaxButtonClicks"/>.
		/// 1 by default.
		/// </summary>
		public int ButtonClicks
		{
			get => Mathf.Clamp(_buttonClicks, 1, MaxButtonClicks);
			set => _buttonClicks = value;
		}

		/// <summary>
		/// Longest pause between two taps of one series, in seconds. The series starts over after a
		/// longer pause. 0.4 by default.
		/// </summary>
		public float MultiClickWindow
		{
			get => _multiClickWindow > 0.0f ? _multiClickWindow : 0.4f;
			set => _multiClickWindow = value;
		}

		/// <summary>Where the button starts before anyone drags it. <see cref="OpenButtonAnchor.Right"/> by default.</summary>
		public OpenButtonAnchor ButtonAnchor
		{
			get => _buttonAnchor;
			set => _buttonAnchor = value;
		}

		/// <summary>
		/// Opacity of the button while it is not touched, clamped to <see cref="MinButtonOpacity"/>..1.
		/// A pressed button is always fully opaque. 0.5 by default.
		/// </summary>
		public float ButtonOpacity
		{
			get => Mathf.Clamp(_buttonOpacity, MinButtonOpacity, 1.0f);
			set => _buttonOpacity = value;
		}

		/// <summary>
		/// Shortcuts that toggle the panel. Any one of them works. Empty by default, so the game's
		/// own keys are never taken.
		/// </summary>
		public List<OmniDebuggerShortcut> Shortcuts
		{
			get => _shortcuts ??= new List<OmniDebuggerShortcut>();
			set => _shortcuts = value;
		}

		[Tooltip("Shows the floating button that opens the panel.")]
		[FormerlySerializedAs("_showButton")]
		[SerializeField] private bool _buttonEnabled = true;

		[Tooltip("How many taps on the button open the panel. Guards against opening it by accident.")]
		[SerializeField] [Range(1, MaxButtonClicks)] private int _buttonClicks = 1;

		[Tooltip("Longest pause, in seconds, between two taps that still count as one series.")]
		[SerializeField] private float _multiClickWindow = 0.4f;

		[Tooltip("Where the button starts before anyone drags it.")]
		[SerializeField] private OpenButtonAnchor _buttonAnchor = OpenButtonAnchor.Right;

		[Tooltip("How visible the button is while it is not touched. A pressed button is always fully opaque.")]
		[SerializeField] [Range(MinButtonOpacity, 1.0f)] private float _buttonOpacity = 0.5f;

		[Tooltip("Each entry toggles the panel. An entry fires when all its keys are held and the last one goes down.")]
		[SerializeField] private List<OmniDebuggerShortcut> _shortcuts = new ();

		internal OmniDebuggerOpenOptions Clone()
		{
			List<OmniDebuggerShortcut> shortcuts = new List<OmniDebuggerShortcut>(Shortcuts.Count);

			for (int i = 0; i < Shortcuts.Count; i++)
			{
				OmniDebuggerShortcut shortcut = Shortcuts[i];

				if (shortcut != null)
				{
					shortcuts.Add(shortcut.Clone());
				}
			}

			return new OmniDebuggerOpenOptions
			{
				_buttonEnabled = _buttonEnabled,
				_buttonClicks = _buttonClicks,
				_multiClickWindow = _multiClickWindow,
				_buttonAnchor = _buttonAnchor,
				_buttonOpacity = _buttonOpacity,
				_shortcuts = shortcuts,
			};
		}
	}
}