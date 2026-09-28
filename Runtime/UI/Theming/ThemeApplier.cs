using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ThemeApplier
	{
		private readonly VisualElement _root;
		private readonly List<StyleSheet> _applied = new ();

		public OmniDebuggerTheme Theme { get; private set; }

		public ThemeApplier(VisualElement root)
		{
			if (root == null)
			{
				throw new ArgumentNullException(nameof(root));
			}

			_root = root;
		}

		public void Apply(OmniDebuggerTheme theme)
		{
			Clear();

			Theme = theme;

			Add(OmniDebuggerUiAssets.PanelStyleSheet);
			Add(OmniDebuggerUiAssets.DarkTokens);

			if (theme == null)
			{
				return;
			}

			IReadOnlyList<StyleSheet> sheets = theme.StyleSheets;

			for (int i = 0; i < sheets.Count; i++)
			{
				Add(sheets[i]);
			}
		}

		public void Clear()
		{
			for (int i = _applied.Count - 1; i >= 0; i--)
			{
				_root.styleSheets.Remove(_applied[i]);
			}

			_applied.Clear();
			Theme = null;
		}

		private void Add(StyleSheet sheet)
		{
			if (sheet == null || _applied.Contains(sheet))
			{
				return;
			}

			_root.styleSheets.Add(sheet);
			_applied.Add(sheet);
		}
	}
}