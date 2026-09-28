using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class SearchTab : IOmniDebuggerTab
	{
		private const long DebounceMs = 300;
		private const string Placeholder = "Search commands…";
		private const string PromptMessage = "Type to search by name, group or tag.";
		private const string NothingFoundMessage = "No commands found.";
		private const string QueryStateKey = "query";
		private const string CaseStateKey = "case";

		private readonly OmniDebuggerTabContext _context;
		private readonly ViewServices _services;
		private readonly PanelModel _model = new ();
		private readonly List<CommandCard> _cards = new ();
		private readonly VisualElement _root;
		private readonly VisualElement _toolbar;
		private readonly TextField _query;
		private readonly Button _caseButton;
		private readonly ScrollView _scroll;
		private readonly SearchController _search;
		private readonly ValuePulse _pulse;
		private readonly IVisualElementScheduledItem _debounce;

		public VisualElement Root => _root;

		private CommandDefinition _details;
		private Vector2 _resultsOffset;
		private bool _caseSensitive;
		private bool _disposed;

		public SearchTab(in OmniDebuggerTabContext context)
		{
			_context = context;
			_services = context.Services ?? throw new ArgumentException("The search tab needs the view services.", nameof(context));

			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);

			_toolbar = UiBuild.Element(OmniDebuggerUiClasses.PageHeader);
			_toolbar.style.paddingLeft = 18.0f;
			_toolbar.style.paddingRight = 18.0f;
			_toolbar.style.paddingTop = 18.0f;
			_toolbar.style.marginBottom = 0.0f;
			_root.Add(_toolbar);

			_query = UiBuild.SearchBox(_toolbar, Placeholder);
			_query.RegisterValueChangedCallback(OnQueryChanged);

			_caseButton = UiBuild.TextButton("Aa", ToggleCase, OmniDebuggerUiClasses.Chip);
			_caseButton.tooltip = "Match case";
			_caseButton.style.marginLeft = 6.0f;
			_caseButton.style.marginBottom = 0.0f;
			_toolbar.Add(_caseButton);

			_scroll = UiBuild.Scroll();
			_root.Add(_scroll);

			_pulse = new ValuePulse(_root);
			_search = new SearchController(_root);
			_search.OnResultsChanged += OnResultsChanged;

			_debounce = _root.schedule.Execute(ApplyQuery);
			_debounce.Pause();

			_services.Favorites.OnChanged += OnMarksChanged;

			Restore();
		}

		public void OnOpen()
		{
			_pulse.Resume();
			_search.Resume();
		}

		public void OnClose()
		{
			_pulse.Pause();
			_search.Pause();
			_debounce.Pause();
		}

		public void Refresh()
		{
			if (_disposed)
			{
				return;
			}

			_model.Rebuild(_services.Debugger.Catalog.Commands, _services.Debugger.Groups);
			_search.SetSource(_model.All);

			if (_details != null &&
				!_services.Debugger.Catalog.TryGetDefinition(_details.Key, out _details))
			{
				_details = null;
			}

			Show();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_services.Favorites.OnChanged -= OnMarksChanged;
			_query.UnregisterValueChangedCallback(OnQueryChanged);
			_search.OnResultsChanged -= OnResultsChanged;
			_debounce.Pause();
			ClearCards();
			_search.Dispose();
			_pulse.Dispose();
			_root.RemoveFromHierarchy();
		}

		private void Restore()
		{
			if (_context.State.TryGetValue(CaseStateKey, out object storedCase) && storedCase is bool caseSensitive)
			{
				SetCase(caseSensitive);
			}

			if (_context.State.TryGetValue(QueryStateKey, out object storedQuery) && storedQuery is string query)
			{
				_query.SetValueWithoutNotify(query);
				_search.SetQuery(query);
			}
		}

		private void OnQueryChanged(ChangeEvent<string> evt)
		{
			_context.State.SetValue(QueryStateKey, evt.newValue);
			_debounce.ExecuteLater(DebounceMs);
		}

		private void ApplyQuery()
		{
			_details = null;
			_resultsOffset = Vector2.zero;
			_search.SetQuery(_query.value);
			Show();
		}

		private void ToggleCase()
		{
			SetCase(!_caseSensitive);
			_context.State.SetValue(CaseStateKey, _caseSensitive);
		}

		private void SetCase(bool caseSensitive)
		{
			_caseSensitive = caseSensitive;
			_caseButton.EnableInClassList(OmniDebuggerUiClasses.ChipActive, caseSensitive);
			_search.SetOptions(caseSensitive ? SearchOptions.CaseSensitive : SearchOptions.None);
		}

		private void OnResultsChanged()
		{
			if (_details == null)
			{
				Show();
			}
		}

		private void OnMarksChanged()
		{
			if (_details == null)
			{
				Show();
			}
		}

		private void Show()
		{
			if (_details != null)
			{
				ShowDetails(_details);
				return;
			}

			ClearCards();
			UiBuild.SetVisible(_toolbar, true);

			if (!_search.HasQuery)
			{
				_scroll.Add(UiBuild.Label(PromptMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			IReadOnlyList<CommandDefinition> results = _search.Results;

			if (results.Count == 0)
			{
				_scroll.Add(UiBuild.Label(NothingFoundMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			ResponsiveGrid grid = new ResponsiveGrid();
			_scroll.Add(grid);

			for (int i = 0; i < results.Count; i++)
			{
				CommandCard card = new CommandCard(_services, _pulse, results[i], CommandCardMode.Summary, OpenDetails);
				_cards.Add(card);
				grid.AddCell(card);
			}

			Vector2 offset = _resultsOffset;
			_scroll.schedule.Execute(() => _scroll.scrollOffset = offset);
		}

		private void OpenDetails(CommandDefinition definition)
		{
			_resultsOffset = _scroll.scrollOffset;
			_details = definition;
			ShowDetails(definition);
			_scroll.scrollOffset = Vector2.zero;
		}

		private void ShowDetails(CommandDefinition definition)
		{
			ClearCards();
			UiBuild.SetVisible(_toolbar, false);

			VisualElement header = UiBuild.Element(OmniDebuggerUiClasses.PageHeader);
			header.Add(UiBuild.IconButton(IconGlyph.Back, CloseDetails, "Back to results"));
			header.Add(UiBuild.Label(definition.Name, OmniDebuggerUiClasses.PageHeaderTitle));
			_scroll.Add(header);

			CommandCard card = new CommandCard(_services, _pulse, definition, CommandCardMode.Full);
			_cards.Add(card);
			_scroll.Add(card);
		}

		private void CloseDetails()
		{
			_details = null;
			Show();
		}

		private void ClearCards()
		{
			for (int i = 0; i < _cards.Count; i++)
			{
				_cards[i].Dispose();
			}

			_cards.Clear();
			_scroll.Clear();
		}
	}
}