using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class GroupCard : VisualElement
	{
		private readonly string _group;
		private readonly Action<string> _opened;
		private readonly Action<string, bool> _expandedChanged;
		private readonly Button _toggle;
		private readonly VisualElement _preview;

		private bool _expanded;

		public GroupCard(
			string group,
			string title,
			IReadOnlyList<CommandDefinition> commands,
			bool expanded,
			bool isFavorites,
			Action<string> opened,
			Action<string, bool> expandedChanged)
		{
			_group = group;
			_opened = opened;
			_expandedChanged = expandedChanged;

			AddToClassList(OmniDebuggerUiClasses.Card);
			AddToClassList(OmniDebuggerUiClasses.GroupCard);
			EnableInClassList(OmniDebuggerUiClasses.GroupCardFavorites, isFavorites);

			Button head = new Button(Open);
			head.AddToClassList(OmniDebuggerUiClasses.GroupCardHead);
			Add(head);

			_toggle = UiBuild.IconButton(IconGlyph.TriangleDown, ToggleExpanded, "Show commands");
			_toggle.AddToClassList(OmniDebuggerUiClasses.GroupCardToggle);
			head.Add(_toggle);

			if (isFavorites)
			{
				head.Add(new OmniIcon(IconGlyph.Star));
			}

			head.Add(UiBuild.Label(title, OmniDebuggerUiClasses.GroupCardTitle));
			head.Add(UiBuild.Label(commands.Count.ToString(), OmniDebuggerUiClasses.GroupCardCount));
			head.Add(new OmniIcon(IconGlyph.Chevron));

			_preview = UiBuild.Element(OmniDebuggerUiClasses.GroupCardPreview);

			for (int i = 0; i < commands.Count; i++)
			{
				_preview.Add(UiBuild.Label(commands[i].Name, OmniDebuggerUiClasses.GroupCardPreviewItem));
			}

			Add(_preview);
			SetExpanded(expanded);
		}

		private void Open() => _opened?.Invoke(_group);

		private void ToggleExpanded()
		{
			SetExpanded(!_expanded);
			_expandedChanged?.Invoke(_group, _expanded);
		}

		private void SetExpanded(bool expanded)
		{
			_expanded = expanded;
			UiBuild.SetVisible(_preview, expanded);
			UiBuild.SetGlyph(_toggle, expanded ? IconGlyph.TriangleUp : IconGlyph.TriangleDown);
		}
	}
}