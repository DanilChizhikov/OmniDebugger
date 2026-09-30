using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal static class IconView
	{
		public static VisualElement Create(string key, IIconRegistry icons, IconGlyph fallback, string imageClass)
		{
			if (OmniGlyphs.TryGet(key, out IconGlyph glyph))
			{
				return new OmniIcon(glyph);
			}

			if (icons != null && icons.TryGetImage(key, out Background background))
			{
				VisualElement image = UiBuild.Element(imageClass);
				image.pickingMode = PickingMode.Ignore;
				image.style.backgroundImage = background;
				return image;
			}

			return fallback == IconGlyph.None ? null : new OmniIcon(fallback);
		}
	}
}
