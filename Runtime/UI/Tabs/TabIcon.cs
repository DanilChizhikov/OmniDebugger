using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal static class TabIcon
	{
		public static VisualElement Create(IOmniDebuggerTabFactory factory, IIconRegistry icons) =>
			IconView.Create(factory.Icon, icons, IconGlyph.Grid, OmniDebuggerUiClasses.TabImage);
	}
}
