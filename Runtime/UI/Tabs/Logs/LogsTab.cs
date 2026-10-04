using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LogsTab : IOmniDebuggerTab
	{
		private const int PageSize = 64;
		private const int CopyLimit = int.MaxValue;
		private const long PollMs = 200;
		private const long SearchDebounceMs = 200;
		private const long ToastMs = 1500;
		private const float EstimatedRowHeight = 44.0f;
		private const int PrefetchRows = 5;
		private const float LoadOlderDistance = EstimatedRowHeight * PrefetchRows;
		private const float BottomTolerance = 12.0f;
		private const int SettleTicks = 4;
		private const long SettleIntervalMs = 16;
		private const float WideToolbarWidth = 600.0f;
		private const float NarrowToolbarWidth = 570.0f;
		private const string FilterStateKey = "filter";
		private const string NoLogsMessage = "No logs match.";
		private const string SearchPlaceholder = "Filter — tag:Net -tag:Ads type:error text";
		private const string ExcludePrefix = "−";

		private readonly ILogFeed _feed;
		private readonly LogsFilterState _filter;
		private readonly List<LogRecord> _items = new ();
		private readonly List<LogRecord> _page = new ();
		private readonly List<string> _knownTags = new ();
		private readonly List<string> _scratchTags = new ();
		private readonly VisualElement _root;
		private readonly VisualElement _listPage;
		private readonly VisualElement _toolbar;
		private readonly VisualElement _listArea;
		private readonly ChipBar _chips;
		private readonly TextField _search;
		private readonly Button _logFilter;
		private readonly Button _warningFilter;
		private readonly Button _errorFilter;
		private readonly ListView _list;
		private readonly ScrollView _listScroll;
		private readonly DragScroll _drag;
		private readonly LogDetail _detail;
		private readonly Label _empty;
		private readonly Button _follow;
		private readonly Label _toast;
		private readonly IVisualElementScheduledItem _poll;
		private readonly IVisualElementScheduledItem _searchDebounce;
		private readonly IVisualElementScheduledItem _hideToast;
		private readonly IVisualElementScheduledItem _settle;

		public VisualElement Root => _root;

		private long _seenVersion = -1;
		private long _selectedId = -1;
		private int _settleTicks;
		private bool _wideToolbar;
		private bool _following = true;
		private bool _pending;
		private bool _hasOlder;
		private bool _loadingOlder;
		private bool _disposed;

		public LogsTab(in OmniDebuggerTabContext context)
		{
			_feed = context.Debugger.Logs;
			_filter = context.State.GetOrCreate(FilterStateKey, () => new LogsFilterState());

			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);
			_listPage = UiBuild.Element(OmniDebuggerUiClasses.TabPage);
			_root.Add(_listPage);

			_toolbar = UiBuild.Element(OmniDebuggerUiClasses.LogsToolbar);
			_toolbar.RegisterCallback<GeometryChangedEvent>(OnToolbarGeometryChanged);
			_listPage.Add(_toolbar);

			_search = UiBuild.SearchBox(_toolbar, SearchPlaceholder);
			_search.SetValueWithoutNotify(_filter.Query);
			_search.RegisterValueChangedCallback(OnSearchChanged);

			VisualElement controls = UiBuild.Element(OmniDebuggerUiClasses.LogsControls);
			_toolbar.Add(controls);

			_logFilter = CreateFilter(controls, IconGlyph.Message, OmniDebuggerUiClasses.LogsFilterLog, LogTypeMask.Log);
			_warningFilter = CreateFilter(controls, IconGlyph.Warning, OmniDebuggerUiClasses.LogsFilterWarning, LogTypeMask.Warning);
			_errorFilter = CreateFilter(controls, IconGlyph.Error, OmniDebuggerUiClasses.LogsFilterError, LogTypeMask.Error);

			controls.Add(UiBuild.IconButton(IconGlyph.Copy, CopyAll, "Copy all"));
			controls.Add(UiBuild.IconButton(IconGlyph.Trash, ClearFeed, "Clear"));

			_chips = new ChipBar();
			_chips.AddToClassList(OmniDebuggerUiClasses.LogsChips);
			_listPage.Add(_chips);

			_list = new ListView(_items, EstimatedRowHeight, MakeRow, BindRow)
			{
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				selectionType = SelectionType.None,
			};

			_list.AddToClassList(OmniDebuggerUiClasses.LogsList);
			_listArea = UiBuild.Element(OmniDebuggerUiClasses.LogsArea);
			_listArea.Add(_list);
			_listPage.Add(_listArea);

			_detail = new LogDetail(Copy, CloseDetail);
			UiBuild.SetVisible(_detail, false);
			_listPage.Add(_detail);

			_listScroll = _list.Q<ScrollView>();
			_drag = UiBuild.MakeDraggable(_listScroll);
			_drag.OnDragStarted += StopFollowing;
			_listScroll.verticalScroller.valueChanged += OnScrolled;
			_listScroll.verticalScroller.RegisterCallback<PointerDownEvent>(OnScrollerPressed, TrickleDown.TrickleDown);
			_listScroll.RegisterCallback<WheelEvent>(OnWheel);

			_empty = UiBuild.Label(NoLogsMessage, OmniDebuggerUiClasses.Empty);
			_listArea.Add(_empty);

			_follow = UiBuild.IconButton(IconGlyph.ArrowDown, FollowNewest, "Jump to the newest");
			_follow.AddToClassList(OmniDebuggerUiClasses.LogsFollow);
			_follow.AddManipulator(new Halo());
			UiBuild.SetVisible(_follow, false);
			_listArea.Add(_follow);

			_toast = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.Toast);
			_toast.pickingMode = PickingMode.Ignore;
			_root.Add(_toast);

			_poll = _root.schedule.Execute(Poll).Every(PollMs);
			_poll.Pause();
			_searchDebounce = _root.schedule.Execute(ApplySearch);
			_searchDebounce.Pause();
			_hideToast = _root.schedule.Execute(() => _toast.RemoveFromClassList(OmniDebuggerUiClasses.ToastVisible));
			_hideToast.Pause();
			_settle = _list.schedule.Execute(Settle).Every(SettleIntervalMs);
			_settle.Pause();

			RefreshFilterButtons();
			RefreshChips();
		}

		public void OnOpen()
		{
			_poll.Resume();
			Poll();
		}

		public void OnClose()
		{
			_poll.Pause();
			_searchDebounce.Pause();
			_toast.RemoveFromClassList(OmniDebuggerUiClasses.ToastVisible);
		}

		public void Refresh()
		{
			if (!_disposed)
			{
				Poll();
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_poll.Pause();
			_searchDebounce.Pause();
			_hideToast.Pause();
			_settle.Pause();
			_toolbar.UnregisterCallback<GeometryChangedEvent>(OnToolbarGeometryChanged);
			_search.UnregisterValueChangedCallback(OnSearchChanged);
			_drag.OnDragStarted -= StopFollowing;
			_listScroll.verticalScroller.valueChanged -= OnScrolled;
			_listScroll.verticalScroller.UnregisterCallback<PointerDownEvent>(OnScrollerPressed, TrickleDown.TrickleDown);
			_listScroll.UnregisterCallback<WheelEvent>(OnWheel);
			_root.RemoveFromHierarchy();
		}

		private static void SetCount(Button filter, int count)
		{
			Label label = filter.Q<Label>();

			if (label != null)
			{
				label.text = count > 999 ? "999+" : count.ToString(CultureInfo.InvariantCulture);
			}
		}

		private static bool SameTags(List<string> left, List<string> right)
		{
			if (left.Count != right.Count)
			{
				return false;
			}

			for (int i = 0; i < left.Count; i++)
			{
				if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
				{
					return false;
				}
			}

			return true;
		}

		private Button CreateFilter(VisualElement row, IconGlyph glyph, string className, LogTypeMask type)
		{
			Button button = new Button(() => ToggleType(type));
			button.AddToClassList(OmniDebuggerUiClasses.LogsFilter);
			button.AddToClassList(className);
			button.Add(new OmniIcon(glyph));
			button.Add(new Label("0"));
			row.Add(button);
			return button;
		}

		private void OnToolbarGeometryChanged(GeometryChangedEvent evt)
		{
			float width = evt.newRect.width;

			if (width <= 0.0f)
			{
				return;
			}

			bool wide = _wideToolbar ? width >= NarrowToolbarWidth : width >= WideToolbarWidth;

			if (wide == _wideToolbar)
			{
				return;
			}

			_wideToolbar = wide;
			_toolbar.EnableInClassList(OmniDebuggerUiClasses.LogsToolbarWide, wide);
		}

		private VisualElement MakeRow() => new LogRow(Copy, Select);

		private void BindRow(VisualElement element, int index)
		{
			if (element is LogRow row && index >= 0 && index < _items.Count)
			{
				LogRecord record = _items[index];
				row.Bind(record, record.Id == _selectedId);
			}
		}

		private void Select(LogRecord record)
		{
			if (record.Id == _selectedId)
			{
				CloseDetail();
				return;
			}

			bool opening = _selectedId < 0;
			_selectedId = record.Id;
			_detail.Show(record);
			SetDetailVisible(true);
			_list.RefreshItems();

			if (opening && !_following)
			{
				int index = IndexOf(record.Id);
				_list.schedule.Execute(() =>
				{
					if (!_disposed && index >= 0 && index < _items.Count)
					{
						_list.ScrollToItem(index);
					}
				});
			}
		}

		private void CloseDetail()
		{
			if (_selectedId < 0)
			{
				return;
			}

			_selectedId = -1;
			SetDetailVisible(false);
			_list.RefreshItems();
		}

		private void SetDetailVisible(bool visible)
		{
			UiBuild.SetVisible(_detail, visible);
			_list.EnableInClassList(OmniDebuggerUiClasses.LogsListWithDetail, visible);
		}

		private int IndexOf(long id)
		{
			for (int i = _items.Count - 1; i >= 0; i--)
			{
				if (_items[i].Id == id)
				{
					return i;
				}
			}

			return -1;
		}

		private void Poll()
		{
			long version = _feed.Version;

			if (version != _seenVersion)
			{
				_seenVersion = version;
				RefreshCounts();
				RefreshSuggestions();

				if (_items.Count == 0 || _feed.Count == 0)
				{
					Reload();
					return;
				}

				_pending = true;
			}

			if (CanFollow())
			{
				_following = true;

				if (TakePending() || !IsAtBottom())
				{
					ScrollToBottom();
				}
			}

			UpdateFollowButton();
		}

		private bool CanFollow() =>
			!_drag.IsDragging && (_following || _settleTicks > 0 || IsAtBottom());

		private bool TakePending()
		{
			if (!_pending || _items.Count == 0)
			{
				return false;
			}

			_pending = false;
			_page.Clear();

			LogRecord last = _items[_items.Count - 1];
			if (last.Id <= 1)
			{
				Reload();
				return true;
			}

			bool more = _feed.Query(_filter.ToQuery(PageSize + 1).After(last.Id - 1), _page);

			if (more)
			{
				Reload();
				return false;
			}

			bool repeated = false;
			if (_page.Count > 0 && _page[0].Id == last.Id)
			{
				repeated = _page[0].RepeatCount != last.RepeatCount;
				_items[_items.Count - 1] = _page[0];

				if (repeated && last.Id == _selectedId)
				{
					_detail.Show(_page[0]);
				}

				_page.RemoveAt(0);
			}

			if (_page.Count == 0)
			{
				if (repeated)
				{
					_list.RefreshItem(_items.Count - 1);
				}

				return false;
			}

			_items.AddRange(_page);
			_list.RefreshItems();
			UpdateEmpty();
			return true;
		}

		private void Reload()
		{
			_items.Clear();
			_pending = false;
			_following = true;
			_hasOlder = _feed.Query(_filter.ToQuery(PageSize), _items);
			RebuildList();
			UpdateEmpty();
			ScrollToBottom();

			_listScroll.scrollOffset = Vector2.zero;
		}

		private void LoadOlder()
		{
			if (!_hasOlder || _loadingOlder || _items.Count == 0)
			{
				return;
			}

			_page.Clear();
			_hasOlder = _feed.Query(_filter.ToQuery(PageSize).Before(_items[0].Id), _page);

			if (_page.Count == 0)
			{
				return;
			}

			_loadingOlder = true;
			int added = _page.Count;
			_items.InsertRange(0, _page);
			RebuildList();

			_list.schedule.Execute(() =>
			{
				if (!_disposed)
				{
					_list.ScrollToItem(added);
				}

				_loadingOlder = false;
			});
		}

		private void RebuildList()
		{
			_list.itemsSource = null;
			_list.itemsSource = _items;
			_list.Rebuild();
		}

		private void FollowNewest()
		{
			_following = true;
			TakePending();
			ScrollToBottom();
		}

		private void ScrollToBottom()
		{
			UiBuild.SetVisible(_follow, false);

			if (_items.Count == 0)
			{
				return;
			}

			_settleTicks = SettleTicks;
			_settle.Resume();
		}

		private void Settle()
		{
			if (_items.Count > 0)
			{
				_list.ScrollToItem(_items.Count - 1);
			}

			if (--_settleTicks > 0)
			{
				return;
			}

			_settleTicks = 0;
			_settle.Pause();
			UpdateFollowButton();
		}

		private void StopFollowing() => _following = false;

		private void OnScrollerPressed(PointerDownEvent evt) => StopFollowing();

		private void OnWheel(WheelEvent evt)
		{
			if (evt.delta.y < 0.0f)
			{
				StopFollowing();
			}
		}

		private void OnScrolled(float value)
		{
			if (_settleTicks > 0)
			{
				return;
			}

			if (IsAtBottom())
			{
				_following = true;
			}
			else if (HasScrollableContent() && _listScroll.scrollOffset.y <= LoadOlderDistance)
			{
				LoadOlder();
			}

			UpdateFollowButton();
		}

		private void UpdateFollowButton()
		{
			bool visible = _settleTicks <= 0 &&
				_items.Count > 0 &&
				(!_following || _pending) &&
				HasScrollableContent() &&
				!IsAtBottom();

			UiBuild.SetVisible(_follow, visible);
		}

		private bool IsAtBottom()
		{
			float max = MaxOffset();
			return max <= BottomTolerance || _listScroll.scrollOffset.y >= max - BottomTolerance;
		}

		private bool HasScrollableContent() => MaxOffset() > BottomTolerance;

		private float MaxOffset()
		{
			float content = _listScroll.contentContainer.layout.height;
			float viewport = _listScroll.contentViewport.layout.height;

			if (float.IsNaN(content) || float.IsNaN(viewport))
			{
				return 0.0f;
			}

			return Mathf.Max(0.0f, content - viewport);
		}

		private void OnSearchChanged(ChangeEvent<string> evt) => _searchDebounce.ExecuteLater(SearchDebounceMs);

		private void ApplySearch()
		{
			_filter.Query = _search.value ?? string.Empty;
			OnFilterChanged();
		}

		private void ToggleType(LogTypeMask type)
		{
			_filter.ToggleType(type);
			ShowQuery();
		}

		private void AddTag(string tag)
		{
			_filter.AddTag(tag, excluded: false);
			ShowQuery();
		}

		private void RemoveTerm(LogQueryToken token)
		{
			_filter.Remove(token);
			ShowQuery();
		}

		private void ShowQuery()
		{
			_searchDebounce.Pause();
			_search.SetValueWithoutNotify(_filter.Query);
			OnFilterChanged();
		}

		private void OnFilterChanged()
		{
			RefreshFilterButtons();
			RefreshChips();
			Reload();
		}

		private void RefreshFilterButtons()
		{
			LogTypeMask types = _filter.Types;
			_logFilter.EnableInClassList(OmniDebuggerUiClasses.LogsFilterActive, (types & LogTypeMask.Log) != 0);
			_warningFilter.EnableInClassList(OmniDebuggerUiClasses.LogsFilterActive, (types & LogTypeMask.Warning) != 0);
			_errorFilter.EnableInClassList(OmniDebuggerUiClasses.LogsFilterActive, (types & LogTypeMask.Error) != 0);
		}

		private void RefreshChips()
		{
			_chips.ClearChips();

			foreach (LogQueryToken token in _filter.GetTerms())
			{
				LogQueryToken captured = token;
				string key = token.Kind == LogQueryTokenKind.Tag ? LogQuerySyntax.TagKey : LogQuerySyntax.TypeKey;
				string text = (token.Negated ? ExcludePrefix : string.Empty) + key + ":" + token.Value;
				_chips.AddRemovableChip(text, () => RemoveTerm(captured), token.Negated ? OmniDebuggerUiClasses.ChipNegated : null);
			}

			bool separated = _chips.Count == 0;

			for (int i = 0; i < _knownTags.Count; i++)
			{
				string tag = _knownTags[i];
				if (_filter.HasTag(tag))
				{
					continue;
				}

				if (!separated)
				{
					_chips.AddSeparator();
					separated = true;
				}

				_chips.AddChip(tag, () => AddTag(tag), selected: false, OmniDebuggerUiClasses.ChipSuggestion);
			}

			UiBuild.SetVisible(_chips, _chips.Count > 0);
		}

		private void RefreshSuggestions()
		{
			_scratchTags.Clear();
			_feed.GetKnownTags(_scratchTags);

			if (SameTags(_scratchTags, _knownTags))
			{
				return;
			}

			_knownTags.Clear();
			_knownTags.AddRange(_scratchTags);
			RefreshChips();
		}

		private void RefreshCounts()
		{
			_feed.CountByType(out int logs, out int warnings, out int errors);
			SetCount(_logFilter, logs);
			SetCount(_warningFilter, warnings);
			SetCount(_errorFilter, errors);
		}

		private void UpdateEmpty()
		{
			bool empty = _items.Count == 0;
			UiBuild.SetVisible(_empty, empty);
			UiBuild.SetVisible(_list, !empty);
		}

		private void ClearFeed()
		{
			_feed.Clear();
			_seenVersion = -1;
			CloseDetail();
			Poll();
		}

		private void Copy(LogRecord record)
		{
			GUIUtility.systemCopyBuffer = LogFormat.ForClipboard(record);
			ShowToast("Copied");
		}

		private void CopyAll()
		{
			List<LogRecord> all = new List<LogRecord>();
			_feed.Query(_filter.ToQuery(CopyLimit), all);
			GUIUtility.systemCopyBuffer = LogFormat.ForClipboard(all);
			ShowToast(all.Count == 1 ? "Copied 1 log" : $"Copied {all.Count} logs");
		}

		private void ShowToast(string message)
		{
			_toast.text = message;
			_toast.BringToFront();
			_toast.AddToClassList(OmniDebuggerUiClasses.ToastVisible);
			_hideToast.ExecuteLater(ToastMs);
		}
	}
}