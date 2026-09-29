#if OMNI_DEBUGGER
using System;
using UnityEditor;
using UnityEngine;

namespace DTech.OmniDebugger.UI.Editor
{
	internal static class EditorWindowPrefs
	{
		public static event Action OnChanged;

		public const float MinZoom = 0.5f;
		public const float MaxZoom = 1.25f;
		public const float DefaultZoom = 0.75f;

		private const string Prefix = "DTech.OmniDebugger.UI.Editor.";
		private const string ZoomKey = Prefix + "Zoom";
		private const string OrientationKey = Prefix + "Orientation";

		public static float Zoom
		{
			get
			{
				_zoom ??= EditorPrefs.GetFloat(ZoomKey, DefaultZoom);
				return Mathf.Clamp(_zoom.Value, MinZoom, MaxZoom);
			}
			set
			{
				float clamped = Mathf.Clamp(value, MinZoom, MaxZoom);

				if (Mathf.Approximately(Zoom, clamped))
				{
					return;
				}

				_zoom = clamped;
				EditorPrefs.SetFloat(ZoomKey, clamped);
				OnChanged?.Invoke();
			}
		}

		public static ViewOrientation Orientation
		{
			get
			{
				_orientation ??= ToOrientation(EditorPrefs.GetInt(OrientationKey, (int)ViewOrientation.Auto));
				return _orientation.Value;
			}
			set
			{
				if (Orientation == value)
				{
					return;
				}

				_orientation = value;
				EditorPrefs.SetInt(OrientationKey, (int)value);
				OnChanged?.Invoke();
			}
		}

		private static float? _zoom;
		private static ViewOrientation? _orientation;

		public static void Reset()
		{
			Zoom = DefaultZoom;
			Orientation = ViewOrientation.Auto;
		}

		private static ViewOrientation ToOrientation(int value) =>
			value >= (int)ViewOrientation.Auto && value <= (int)ViewOrientation.Landscape
				? (ViewOrientation)value
				: ViewOrientation.Auto;
	}
}
#endif
