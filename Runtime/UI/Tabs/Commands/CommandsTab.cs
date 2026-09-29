using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandsTab : IOmniDebuggerTab
	{
		private const string FavoritesGroup = "★favorites";

		private const string FavoritesTitle = "Favourites";
		private const string GroupStateKey = "group";
		private const string ExpandedStateKey = "expanded";
		private const string EmptyMessage = "No commands yet. Add a source with debugger.Catalog.AddSource(obj).";

		private static readonly CustomStyleProperty<float> _refreshProperty = new ("--od-pulse-ms");

		private readonly OmniDebuggerTabContext _context;
		private readonly ViewServices _services;
		private readonly PanelModel _model = new ();
		private readonly List<GroupSection> _sections = new ();
		private readonly List<CommandRow> _rows = new ();
		private readonly List<CommandDefinition> _favorites = new ();
		private readonly HashSet<string> _expanded;
		private readonly VisualElement _root;
		private readonly ScrollView _scroll;
		private readonly ValuePulse _pulse;

		public VisualElement Root => _root;

		private string _group;
		private Vector2 _groupsOffset;
		private bool _open;
		private bool _disposed;

		public CommandsTab(in OmniDebuggerTabContext context)
		{
			_context = context;
			_services = context.Services ?? throw new ArgumentException("The commands tab needs the view services.", nameof(context));
			_expanded = context.State.GetOrCreate(ExpandedStateKey, () => new HashSet<string>(StringComparer.Ordinal));

			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);
			_scroll = UiBuild.Scroll();
			_root.Add(_scroll);

			_pulse = new ValuePulse(_root);
			_root.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
			_services.Favorites.OnChanged += OnFavoritesChanged;

			RestoreGroup();
			Rebuild();
		}

		public void OnOpen()
		{
			if (_disposed || _open)
			{
				return;
			}

			_open = true;
			_pulse.Resume();
		}

		public void OnClose()
		{
			if (_disposed || !_open)
			{
				return;
			}

			_open = false;
			_pulse.Pause();
		}

		public void Refresh()
		{
			if (!_disposed)
			{
				Rebuild();
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_services.Favorites.OnChanged -= OnFavoritesChanged;
			_root.UnregisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
			ClearPage();
			_pulse.Dispose();
			_root.RemoveFromHierarchy();
		}

		private void RestoreGroup()
		{
			if (_context.State.TryGetValue(GroupStateKey, out object stored) && stored is string group)
			{
				_group = group;
			}
		}

		private void Rebuild()
		{
			Vector2 offset = _scroll.scrollOffset;

			RebuildPage();
			_scroll.schedule.Execute(() => _scroll.scrollOffset = offset);
		}

		private void RebuildPage()
		{
			_model.Rebuild(_services.Debugger.Catalog.Commands, _services.Debugger.Groups);
			CollectFavorites();

			if (_group != null && !HasGroup(_group))
			{
				_group = null;
			}

			if (_group == null)
			{
				ShowGroups();
			}
			else
			{
				ShowGroup(_group);
			}
		}

		private void ShowGroups()
		{
			ClearPage();

			if (_model.Groups.Count == 0)
			{
				_scroll.Add(UiBuild.Label(EmptyMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			ResponsiveGrid grid = new ResponsiveGrid(independentColumns: true, wide: true);
			_scroll.Add(grid);

			if (_favorites.Count > 0)
			{
				grid.AddCell(CreateSection(FavoritesGroup, FavoritesTitle, _favorites, isFavorites: true));
			}

			IReadOnlyList<string> groups = _model.Groups;

			for (int i = 0; i < groups.Count; i++)
			{
				grid.AddCell(CreateSection(groups[i], groups[i], _model.GetCommands(groups[i]), isFavorites: false));
			}

			Vector2 offset = _groupsOffset;
			_scroll.schedule.Execute(() => _scroll.scrollOffset = offset);
		}

		private void ShowGroup(string group)
		{
			ClearPage();

			bool isFavorites = string.Equals(group, FavoritesGroup, StringComparison.Ordinal);
			IReadOnlyList<CommandDefinition> commands = isFavorites ? _favorites : _model.GetCommands(group);

			VisualElement header = UiBuild.Element(OmniDebuggerUiClasses.PageHeader);
			header.Add(UiBuild.IconButton(IconGlyph.Back, ShowGroupsPage, "Back"));
			header.Add(UiBuild.Label(isFavorites ? FavoritesTitle : group, OmniDebuggerUiClasses.PageHeaderTitle));
			header.Add(UiBuild.Label(commands.Count.ToString(CultureInfo.InvariantCulture), OmniDebuggerUiClasses.PageHeaderCount));
			_scroll.Add(header);

			VisualElement list = UiBuild.Element(OmniDebuggerUiClasses.Card);
			list.AddToClassList(OmniDebuggerUiClasses.RowList);
			_scroll.Add(list);

			for (int i = 0; i < commands.Count; i++)
			{
				CommandRow row = CreateRow(commands[i]);
				row.EnableInClassList(OmniDebuggerUiClasses.First, i == 0);
				_rows.Add(row);
				list.Add(row);
			}
		}

		private GroupSection CreateSection(string group, string title, IReadOnlyList<CommandDefinition> commands, bool isFavorites)
		{
			GroupSection section = new GroupSection(
				group,
				title,
				commands,
				_expanded.Contains(group),
				isFavorites,
				CreateRow,
				OpenGroup,
				OnExpandedChanged);

			_sections.Add(section);
			return section;
		}

		private CommandRow CreateRow(CommandDefinition definition) =>
			new CommandRow(_services, _pulse, definition, CommandRowMode.Full);

		private void OpenGroup(string group)
		{
			_groupsOffset = _scroll.scrollOffset;
			SelectGroup(group);
			ShowGroup(group);
			_scroll.scrollOffset = Vector2.zero;
		}

		private void ShowGroupsPage()
		{
			SelectGroup(null);
			ShowGroups();
		}

		private void SelectGroup(string group)
		{
			_group = group;
			_context.State.SetValue(GroupStateKey, group);
		}

		private void OnExpandedChanged(string group, bool expanded)
		{
			if (expanded)
			{
				_expanded.Add(group);
			}
			else
			{
				_expanded.Remove(group);
			}
		}

		private void CollectFavorites()
		{
			_favorites.Clear();
			IReadOnlyList<string> keys = _services.Favorites.Keys;

			for (int i = 0; i < keys.Count; i++)
			{
				if (_services.Debugger.Catalog.TryGetDefinition(keys[i], out CommandDefinition definition))
				{
					_favorites.Add(definition);
				}
			}
		}

		private bool HasGroup(string group) =>
			string.Equals(group, FavoritesGroup, StringComparison.Ordinal)
				? _favorites.Count > 0
				: _model.HasGroup(group) && !string.Equals(group, PanelModel.AllGroup, StringComparison.Ordinal);

		private void ClearPage()
		{
			for (int i = 0; i < _sections.Count; i++)
			{
				_sections[i].Dispose();
			}

			for (int i = 0; i < _rows.Count; i++)
			{
				_rows[i].Dispose();
			}

			_sections.Clear();
			_rows.Clear();
			_scroll.Clear();
		}

		private void OnFavoritesChanged()
		{
			bool showingFavorites = string.Equals(_group, FavoritesGroup, StringComparison.Ordinal);

			if (_group == null || showingFavorites)
			{
				Rebuild();
				return;
			}

			CollectFavorites();
		}

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
		{
			if (evt.customStyle.TryGetValue(_refreshProperty, out float milliseconds) && milliseconds > 0.0f)
			{
				_pulse.SetInterval((long)milliseconds);
			}
		}
	}
}