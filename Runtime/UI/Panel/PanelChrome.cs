using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PanelChrome : IDisposable
	{
		public event Action<OmniDebuggerTheme> OnThemeSelected;
		public event Action OnCloseRequested;

		private const string Title = "OmniDebugger";
		private const string LightThemeId = "light";

		private readonly VisualElement _root;
		private readonly Button _themeButton;
		private readonly List<OmniDebuggerTheme> _themes = new ();

		public VisualElement Root => _root;

		private OmniDebuggerTheme _selected;

		public PanelChrome(bool showCloseButton)
		{
			_root = UiBuild.Element(OmniDebuggerUiClasses.Header);
			_root.Add(UiBuild.Label(Title, OmniDebuggerUiClasses.HeaderTitle));
			_root.Add(UiBuild.Element(OmniDebuggerUiClasses.HeaderSpacer));

			_themeButton = UiBuild.IconButton(IconGlyph.Sun, CycleTheme, "Switch theme");
			_themeButton.AddToClassList(OmniDebuggerUiClasses.HeaderButton);
			_root.Add(_themeButton);

			if (showCloseButton)
			{
				Button close = UiBuild.IconButton(IconGlyph.Close, RequestClose, "Close");
				close.AddToClassList(OmniDebuggerUiClasses.HeaderButton);
				_root.Add(close);
			}
		}

		public void SetThemes(IReadOnlyList<OmniDebuggerTheme> themes, OmniDebuggerTheme selected)
		{
			_themes.Clear();

			if (themes != null)
			{
				for (int i = 0; i < themes.Count; i++)
				{
					if (themes[i] != null)
					{
						_themes.Add(themes[i]);
					}
				}
			}

			_selected = selected;

			bool isLight = selected != null && string.Equals(selected.Id, LightThemeId, StringComparison.OrdinalIgnoreCase);
			UiBuild.SetGlyph(_themeButton, isLight ? IconGlyph.Moon : IconGlyph.Sun);
			UiBuild.SetVisible(_themeButton, _themes.Count > 1);
		}

		public void Dispose()
		{
			OnThemeSelected = null;
			OnCloseRequested = null;
			_themes.Clear();
			_root.RemoveFromHierarchy();
		}

		private void CycleTheme()
		{
			if (_themes.Count < 2)
			{
				return;
			}

			int index = _themes.IndexOf(_selected);
			OmniDebuggerTheme next = _themes[(index + 1) % _themes.Count];
			OnThemeSelected?.Invoke(next);
		}

		private void RequestClose() => OnCloseRequested?.Invoke();
	}
}