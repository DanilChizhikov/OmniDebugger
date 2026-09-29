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
		private readonly PopupLayer _popups;
		private readonly Button _themeButton;
		private readonly Button _themePicker;
		private readonly Label _themePickerText;
		private readonly List<OmniDebuggerTheme> _themes = new ();

		public VisualElement Root => _root;

		private OmniDebuggerTheme _selected;

		public PanelChrome(bool showCloseButton, PopupLayer popups)
		{
			_popups = popups ?? throw new ArgumentNullException(nameof(popups));
			_root = UiBuild.Element(OmniDebuggerUiClasses.Header);
			_root.Add(UiBuild.Label(Title, OmniDebuggerUiClasses.HeaderTitle));
			_root.Add(UiBuild.Element(OmniDebuggerUiClasses.HeaderSpacer));

			_themeButton = UiBuild.IconButton(IconGlyph.Sun, CycleTheme, "Switch theme");
			_themeButton.AddToClassList(OmniDebuggerUiClasses.HeaderButton);
			_root.Add(_themeButton);

			_themePicker = new Button(OpenThemeList) { tooltip = "Switch theme" };
			_themePicker.AddToClassList(OmniDebuggerUiClasses.EnumButton);
			_themePicker.AddToClassList(OmniDebuggerUiClasses.HeaderButton);
			_themePicker.AddToClassList(OmniDebuggerUiClasses.HeaderThemePicker);
			_themePickerText = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.EnumButtonText);
			_themePicker.Add(_themePickerText);
			_themePicker.Add(new OmniIcon(IconGlyph.TriangleDown));
			_root.Add(_themePicker);

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

			bool switchable = _themes.Count > 1;
			bool builtInOnly = AreBuiltIn(_themes);

			bool isLight = selected != null && string.Equals(selected.Id, LightThemeId, StringComparison.OrdinalIgnoreCase);
			UiBuild.SetGlyph(_themeButton, isLight ? IconGlyph.Moon : IconGlyph.Sun);
			UiBuild.SetVisible(_themeButton, switchable && builtInOnly);

			_themePickerText.text = selected == null ? string.Empty : selected.DisplayName;
			UiBuild.SetVisible(_themePicker, switchable && !builtInOnly);
		}

		public void Dispose()
		{
			OnThemeSelected = null;
			OnCloseRequested = null;
			_themes.Clear();
			_root.RemoveFromHierarchy();
		}

		private static bool AreBuiltIn(List<OmniDebuggerTheme> themes)
		{
			for (int i = 0; i < themes.Count; i++)
			{
				if (!themes[i].IsBuiltIn)
				{
					return false;
				}
			}

			return true;
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

		private void OpenThemeList()
		{
			ScrollView list = UiBuild.Scroll();
			list.contentContainer.style.paddingLeft = 0.0f;
			list.contentContainer.style.paddingRight = 0.0f;
			list.contentContainer.style.paddingTop = 0.0f;
			list.contentContainer.style.paddingBottom = 0.0f;

			for (int i = 0; i < _themes.Count; i++)
			{
				OmniDebuggerTheme theme = _themes[i];
				Button item = UiBuild.TextButton(theme.DisplayName, () => PickTheme(theme), OmniDebuggerUiClasses.PopupOption);
				item.EnableInClassList(OmniDebuggerUiClasses.PopupOptionSelected, ReferenceEquals(theme, _selected));
				list.Add(item);
			}

			_popups.ShowAnchored(_themePicker, list, onHidden: null);
		}

		private void PickTheme(OmniDebuggerTheme theme)
		{
			_popups.Hide();
			OnThemeSelected?.Invoke(theme);
		}

		private void RequestClose() => OnCloseRequested?.Invoke();
	}
}