using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandsTab : IOmniDebuggerTab
	{
		private const string GroupStateKey = "group";
		private const string QueryStateKey = "query";
		private const string AllLabel = "All";
		private const string SearchPlaceholder = "Search commands…";
		private const string EmptyMessage = "No commands yet. Register an object with debugger.Commands.Register(obj), or build some with debugger.Commands.Build().";
		private const string NoResultsMessage = "Nothing matches.";
		private const string HighlightClass = OmniDebuggerUiClasses.RowHighlight;
		private const long SearchDebounceMs = 150;
		private const long HighlightMs = 1200;

		private static readonly CustomStyleProperty<float> _refreshProperty = new ("--od-pulse-ms");

		private readonly OmniDebuggerTabContext _context;
		private readonly ViewServices _services;
		private readonly PanelModel _model = new ();
		private readonly List<CommandRow> _rows = new ();
		private readonly VisualElement _root;
		private readonly TextField _search;
		private readonly ChipBar _chips;
		private readonly ScrollView _scroll;
		private readonly ValuePulse _pulse;
		private readonly SearchController _searchController;
		private readonly IVisualElementScheduledItem _debounce;

		public VisualElement Root => _root;

		private string _group = string.Empty;
		private string _query = string.Empty;
		private bool _open;
		private bool _rebuilding;
		private bool _disposed;

		public CommandsTab(in OmniDebuggerTabContext context)
		{
			_context = context;
			_services = context.Services ?? throw new ArgumentException("The commands tab needs the view services.", nameof(context));

			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);
			_root.AddToClassList(OmniDebuggerUiClasses.CommandsPage);

			VisualElement toolbar = UiBuild.Element(OmniDebuggerUiClasses.Toolbar);
			_search = UiBuild.SearchBox(toolbar, SearchPlaceholder);
			_search.RegisterValueChangedCallback(OnSearchChanged);
			_root.Add(toolbar);

			_chips = new ChipBar();
			_root.Add(_chips);

			_scroll = UiBuild.Scroll();
			_root.Add(_scroll);

			_pulse = new ValuePulse(_root);
			_searchController = new SearchController(_root);
			_searchController.OnResultsChanged += OnSearchResults;
			_debounce = _root.schedule.Execute(ApplyQuery);
			_debounce.Pause();

			_root.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

			RestoreState();
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
			_searchController.Resume();
		}

		public void OnClose()
		{
			if (_disposed || !_open)
			{
				return;
			}

			_open = false;
			_pulse.Pause();
			_searchController.Pause();
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
			_debounce.Pause();
			_root.UnregisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
			_searchController.OnResultsChanged -= OnSearchResults;
			_searchController.Dispose();
			ClearRows();
			_pulse.Dispose();
			_root.RemoveFromHierarchy();
		}

		public void Reveal(CommandDefinition definition)
		{
			if (_disposed || definition == null)
			{
				return;
			}

			_search.SetValueWithoutNotify(string.Empty);
			SetQuery(string.Empty);
			SelectGroup(definition.GroupPath);

			for (int i = 0; i < _rows.Count; i++)
			{
				CommandRow row = _rows[i];

				if (!ReferenceEquals(row.Definition, definition) &&
					!string.Equals(row.Definition.Path, definition.Path, StringComparison.Ordinal))
				{
					continue;
				}

				_scroll.schedule.Execute(() => _scroll.ScrollTo(row));
				row.AddToClassList(HighlightClass);
				row.schedule.Execute(() => row.RemoveFromClassList(HighlightClass)).ExecuteLater(HighlightMs);
				return;
			}
		}

		public void FocusSearch() => _search.Focus();

		private void RestoreState()
		{
			if (_context.State.TryGetValue(GroupStateKey, out object group) && group is string storedGroup)
			{
				_group = storedGroup;
			}

			if (_context.State.TryGetValue(QueryStateKey, out object query) && query is string storedQuery)
			{
				_query = storedQuery;
				_search.SetValueWithoutNotify(storedQuery);
				_searchController.SetQuery(storedQuery);
			}
		}

		private void Rebuild()
		{
			_model.Rebuild(_services.Debugger.Commands.All, _services.Debugger.Groups);

			_rebuilding = true;
			_searchController.SetSource(_model.All);
			_rebuilding = false;

			if (_group.Length > 0 && !_model.HasGroup(_group))
			{
				_group = CommandPath.GetParent(_group);
				while (_group.Length > 0 && !_model.HasGroup(_group))
				{
					_group = CommandPath.GetParent(_group);
				}
			}

			Vector2 offset = _scroll.scrollOffset;
			ShowPage();
			_scroll.schedule.Execute(() => _scroll.scrollOffset = offset);
		}

		private void ShowPage()
		{
			BuildChips();
			ClearRows();

			if (_model.All.Count == 0)
			{
				_scroll.Add(UiBuild.Label(EmptyMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			if (_searchController.HasQuery)
			{
				ShowResults();
				return;
			}

			ShowGroup();
		}

		private void BuildChips()
		{
			_chips.ClearChips();
			UiBuild.SetVisible(_chips, !_searchController.HasQuery && _model.GetChildren(string.Empty).Count > 0);

			_chips.AddChip(AllLabel, () => SelectGroup(string.Empty), selected: _group.Length == 0);

			if (_group.Length == 0)
			{
				AddChildChips(string.Empty);
				return;
			}

			string[] segments = CommandPath.Split(_group);
			string path = string.Empty;

			for (int i = 0; i < segments.Length; i++)
			{
				path = path.Length == 0 ? segments[i] : path + CommandPath.Separator + segments[i];
				string target = path;
				_chips.AddChip(segments[i], () => SelectGroup(target), selected: i == segments.Length - 1, OmniDebuggerUiClasses.ChipCrumb);
			}

			if (_model.GetChildren(_group).Count > 0)
			{
				_chips.AddSeparator();
				AddChildChips(_group);
			}
		}

		private void AddChildChips(string parent)
		{
			IReadOnlyList<string> children = _model.GetChildren(parent);

			for (int i = 0; i < children.Count; i++)
			{
				string child = children[i];
				_chips.AddChip(CommandPath.GetName(child), () => SelectGroup(child));
			}
		}

		private void ShowGroup()
		{
			IReadOnlyList<CommandDefinition> commands = _model.GetSubtree(_group);
			VisualElement card = null;
			string section = null;

			for (int i = 0; i < commands.Count; i++)
			{
				CommandDefinition definition = commands[i];

				if (card == null || !string.Equals(section, definition.GroupPath, StringComparison.Ordinal))
				{
					section = definition.GroupPath;
					Label header = UiBuild.Label(DescribeSection(section), OmniDebuggerUiClasses.SectionHeader);
					header.EnableInClassList(OmniDebuggerUiClasses.First, card == null);
					_scroll.Add(header);

					card = UiBuild.Element(OmniDebuggerUiClasses.Card);
					card.AddToClassList(OmniDebuggerUiClasses.RowList);
					_scroll.Add(card);
				}

				AddRow(card, definition, first: card.childCount == 0, showGroup: false);
			}
		}

		private void ShowResults()
		{
			IReadOnlyList<CommandDefinition> results = _searchController.Results;

			if (results.Count == 0)
			{
				_scroll.Add(UiBuild.Label(NoResultsMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			VisualElement card = UiBuild.Element(OmniDebuggerUiClasses.Card);
			card.AddToClassList(OmniDebuggerUiClasses.RowList);
			_scroll.Add(card);

			for (int i = 0; i < results.Count; i++)
			{
				AddRow(card, results[i], first: i == 0, showGroup: true);
			}
		}

		private string DescribeSection(string groupPath)
		{
			if (_group.Length == 0 || groupPath.Length <= _group.Length)
			{
				return groupPath.Replace(CommandPath.Separator.ToString(), " / ");
			}

			return groupPath.Substring(_group.Length + 1).Replace(CommandPath.Separator.ToString(), " / ");
		}

		private void AddRow(VisualElement card, CommandDefinition definition, bool first, bool showGroup)
		{
			CommandRow row = new CommandRow(_services, _pulse, definition, CommandRowMode.Full, showGroup: showGroup);
			row.EnableInClassList(OmniDebuggerUiClasses.First, first);
			_rows.Add(row);
			card.Add(row);
		}

		private void SelectGroup(string group)
		{
			_group = group ?? string.Empty;
			_context.State.SetValue(GroupStateKey, _group);
			ShowPage();
			_scroll.scrollOffset = Vector2.zero;
		}

		private void OnSearchChanged(ChangeEvent<string> evt) => _debounce.ExecuteLater(SearchDebounceMs);

		private void ApplyQuery() => SetQuery(_search.value);

		private void SetQuery(string query)
		{
			_query = query ?? string.Empty;
			_context.State.SetValue(QueryStateKey, _query.Length == 0 ? null : _query);
			_searchController.SetQuery(_query);
		}

		private void OnSearchResults()
		{
			if (_disposed || _rebuilding)
			{
				return;
			}

			ShowPage();
			_scroll.scrollOffset = Vector2.zero;
		}

		private void ClearRows()
		{
			for (int i = 0; i < _rows.Count; i++)
			{
				_rows[i].Dispose();
			}

			_rows.Clear();
			_scroll.Clear();
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
