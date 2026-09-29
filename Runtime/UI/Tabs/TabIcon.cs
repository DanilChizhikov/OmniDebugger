using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal static class TabIcon
	{
		public static VisualElement Create(IOmniDebuggerTabFactory factory, IIconRegistry icons)
		{
			if (factory is IBuiltInTabIcon builtIn)
			{
				return new OmniIcon(builtIn.Glyph);
			}

			if (!factory.Icon.IsEmpty && icons != null && icons.TryGet(factory.Icon, out Background background))
			{
				VisualElement image = UiBuild.Element(OmniDebuggerUiClasses.TabImage);
				image.pickingMode = PickingMode.Ignore;
				image.style.backgroundImage = background;
				return image;
			}

			return new OmniIcon(IconGlyph.Grid);
		}
	}
}
