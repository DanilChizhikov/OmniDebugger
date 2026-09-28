using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal static class PanelScaling
	{
		private const float ReferenceDpi = 96.0f;
		private const float PortraitAspect = 0.56f;
		private const float LandscapeAspect = 1.78f;
		private const float LandscapeMatch = 0.7f;
		
		private static readonly Vector2 _referenceResolution = new (360.0f, 640.0f);

		public static OmniDebuggerScaleMode Resolve(OmniDebuggerScaleMode mode, bool isMobilePlatform)
		{
			if (mode != OmniDebuggerScaleMode.Auto)
			{
				return mode;
			}

			return isMobilePlatform ? OmniDebuggerScaleMode.ScreenSize : OmniDebuggerScaleMode.PhysicalSize;
		}

		public static void Apply(PanelSettings settings, OmniDebuggerScaleMode mode, float scale)
		{
			if (settings == null)
			{
				return;
			}

			scale = scale > 0.0f ? scale : 1.0f;

			switch (Resolve(mode, Application.isMobilePlatform))
			{
				case OmniDebuggerScaleMode.ScreenSize:
					settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
					settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
					settings.referenceResolution = ToResolution(_referenceResolution / scale);
					settings.match = ResolveMatch(Screen.width, Screen.height);
					break;
				default:
					settings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
					settings.referenceDpi = ReferenceDpi / scale;
					settings.fallbackDpi = ReferenceDpi / scale;
					break;
			}
		}

		public static void UpdateMatch(PanelSettings settings)
		{
			if (settings == null || settings.scaleMode != PanelScaleMode.ScaleWithScreenSize)
			{
				return;
			}

			float match = ResolveMatch(Screen.width, Screen.height);

			if (!Mathf.Approximately(settings.match, match))
			{
				settings.match = match;
			}
		}

		public static float ResolveMatch(float width, float height)
		{
			if (width <= 0.0f || height <= 0.0f)
			{
				return 0.0f;
			}

			return Mathf.InverseLerp(PortraitAspect, LandscapeAspect, width / height) * LandscapeMatch;
		}

		private static Vector2Int ToResolution(Vector2 size) =>
			new (Mathf.Max(1, Mathf.RoundToInt(size.x)), Mathf.Max(1, Mathf.RoundToInt(size.y)));
	}
}