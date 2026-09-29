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
		private const string VersionPrefix = "v";
		private const string ThemeTooltip = "Switch theme";

		private readonly VisualElement _brand;
		private readonly VisualElement _actions;
		private readonly VisualElement _pageBar;
		private readonly VisualElement _pageIcon;
		private readonly Label _pageTitle;
		private readonly Label _keycap;
		private readonly VisualElement _footer;
		private readonly PopupLayer _popups;
		private readonly IIconRegistry _icons;
		private readonly Button _themeButton;
		private readonly Button _themePicker;
		private readonly Label _themePickerText;
		private readonly List<OmniDebuggerTheme> _themes = new ();

		public VisualElement Brand => _brand;

		public VisualElement PageBar => _pageBar;

		public VisualElement Footer => _footer;

		private OmniDebuggerTheme _selected;

		public PanelChrome(bool showCloseButton, PopupLayer popups, IIconRegistry icons, string version)
		{
			_popups = popups ?? throw new ArgumentNullException(nameof(popups));
			_icons = icons;

			_brand = UiBuild.Element(OmniDebuggerUiClasses.Brand);
			_brand.Add(UiBuild.Label(Title, OmniDebuggerUiClasses.BrandTitle));
			_brand.Add(UiBuild.Element(OmniDebuggerUiClasses.Spacer));

			_actions = UiBuild.Element(OmniDebuggerUiClasses.Actions);

			_themeButton = UiBuild.IconButton(IconGlyph.Sun, CycleTheme, ThemeTooltip);
			_actions.Add(_themeButton);

			_themePicker = new Button(OpenThemeList) { tooltip = ThemeTooltip };
			_themePicker.AddToClassList(OmniDebuggerUiClasses.EnumButton);
			_themePicker.AddToClassList(OmniDebuggerUiClasses.ActionsThemePicker);
			_themePickerText = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.EnumButtonText);
			_themePicker.Add(_themePickerText);
			_themePicker.Add(new OmniIcon(IconGlyph.ChevronDown));
			_actions.Add(_themePicker);

			if (showCloseButton)
			{
				_actions.Add(UiBuild.IconButton(IconGlyph.Close, RequestClose, "Close"));
			}

			_pageBar = UiBuild.Element(OmniDebuggerUiClasses.PageBar);
			_pageIcon = UiBuild.Element(OmniDebuggerUiClasses.PageBarIcon);
			_pageBar.Add(_pageIcon);
			_pageTitle = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.PageBarTitle);
			_pageBar.Add(_pageTitle);
			_keycap = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.Keycap);
			UiBuild.SetVisible(_keycap, false);
			_pageBar.Add(_keycap);
			_pageBar.Add(UiBuild.Element(OmniDebuggerUiClasses.Spacer));

			_footer = UiBuild.Element(OmniDebuggerUiClasses.RailFooter);

			if (!string.IsNullOrEmpty(version))
			{
				_footer.Add(UiBuild.Label(VersionPrefix + version, OmniDebuggerUiClasses.RailVersion));
			}

			SetLandscape(false);
		}

		public void SetLandscape(bool landscape)
		{
			VisualElement host = landscape ? _pageBar : _brand;

			if (_actions.parent != host)
			{
				host.Add(_actions);
			}
		}

		public void SetPage(IOmniDebuggerTabFactory factory)
		{
			_pageIcon.Clear();

			if (factory == null)
			{
				_pageTitle.text = string.Empty;
				return;
			}

			_pageIcon.Add(TabIcon.Create(factory, _icons));
			_pageTitle.text = factory.DisplayName;
		}

		public void SetShortcutHint(string hint)
		{
			_keycap.text = hint ?? string.Empty;
			UiBuild.SetVisible(_keycap, !string.IsNullOrEmpty(hint));
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

			bool isLight = selected != null &&
				string.Equals(selected.Id, ThemeCatalog.LightThemeId, StringComparison.OrdinalIgnoreCase);

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
			_brand.RemoveFromHierarchy();
			_pageBar.RemoveFromHierarchy();
			_footer.RemoveFromHierarchy();
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
			list.AddToClassList(OmniDebuggerUiClasses.PopupList);

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
