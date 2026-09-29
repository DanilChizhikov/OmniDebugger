using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class GroupSection : VisualElement, IDisposable
	{
		private const string ToggleTooltip = "Show commands";
		private const string OpenTooltip = "Open the group";

		private readonly string _group;
		private readonly IReadOnlyList<CommandDefinition> _commands;
		private readonly Func<CommandDefinition, CommandRow> _createRow;
		private readonly Action<string> _opened;
		private readonly Action<string, bool> _expandedChanged;
		private readonly VisualElement _body;
		private readonly List<CommandRow> _rows = new ();

		private bool _expanded;
		private bool _disposed;

		public GroupSection(
			string group,
			string title,
			IReadOnlyList<CommandDefinition> commands,
			bool expanded,
			bool isFavorites,
			Func<CommandDefinition, CommandRow> createRow,
			Action<string> opened,
			Action<string, bool> expandedChanged)
		{
			_group = group;
			_commands = commands;
			_createRow = createRow ?? throw new ArgumentNullException(nameof(createRow));
			_opened = opened;
			_expandedChanged = expandedChanged;

			AddToClassList(OmniDebuggerUiClasses.Card);
			AddToClassList(OmniDebuggerUiClasses.Group);
			EnableInClassList(OmniDebuggerUiClasses.GroupFavorites, isFavorites);

			Button head = new Button(Open) { tooltip = OpenTooltip };
			head.AddToClassList(OmniDebuggerUiClasses.GroupHead);
			Add(head);

			Button toggle = UiBuild.IconButton(IconGlyph.ChevronDown, ToggleExpanded, ToggleTooltip);
			toggle.AddToClassList(OmniDebuggerUiClasses.GroupToggle);
			head.Add(toggle);

			if (isFavorites)
			{
				OmniIcon star = new OmniIcon(IconGlyph.Star);
				star.AddToClassList(OmniDebuggerUiClasses.GroupStar);
				head.Add(star);
			}

			head.Add(UiBuild.Label(title.ToUpperInvariant(), OmniDebuggerUiClasses.GroupTitle));
			head.Add(UiBuild.Label(commands.Count.ToString(CultureInfo.InvariantCulture), OmniDebuggerUiClasses.GroupCount));

			OmniIcon open = new OmniIcon(IconGlyph.Chevron);
			open.AddToClassList(OmniDebuggerUiClasses.GroupOpen);
			head.Add(open);

			_body = UiBuild.Element(OmniDebuggerUiClasses.GroupBody);
			Add(_body);

			SetExpanded(expanded);
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			ClearRows();
			RemoveFromHierarchy();
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
			EnableInClassList(OmniDebuggerUiClasses.GroupExpanded, expanded);

			if (expanded)
			{
				BuildRows();
			}
			else
			{
				ClearRows();
			}
		}

		private void BuildRows()
		{
			if (_rows.Count > 0)
			{
				return;
			}

			for (int i = 0; i < _commands.Count; i++)
			{
				CommandRow row = _createRow(_commands[i]);
				row.EnableInClassList(OmniDebuggerUiClasses.First, i == 0);
				_rows.Add(row);
				_body.Add(row);
			}
		}

		private void ClearRows()
		{
			for (int i = 0; i < _rows.Count; i++)
			{
				_rows[i].Dispose();
			}

			_rows.Clear();
		}
	}
}
