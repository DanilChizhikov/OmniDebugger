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

		private readonly ILogFeed _feed;
		private readonly LogsFilterState _filter;
		private readonly List<LogRecord> _items = new ();
		private readonly List<LogRecord> _page = new ();
		private readonly VisualElement _root;
		private readonly VisualElement _listPage;
		private readonly VisualElement _toolbar;
		private readonly LogTagsPage _tagsPage;
		private readonly TextField _search;
		private readonly Button _tagsButton;
		private readonly Button _logFilter;
		private readonly Button _warningFilter;
		private readonly Button _errorFilter;
		private readonly ListView _list;
		private readonly ScrollView _listScroll;
		private readonly DragScroll _drag;
		private readonly Label _empty;
		private readonly Button _follow;
		private readonly Label _toast;
		private readonly IVisualElementScheduledItem _poll;
		private readonly IVisualElementScheduledItem _searchDebounce;
		private readonly IVisualElementScheduledItem _hideToast;
		private readonly IVisualElementScheduledItem _settle;

		public VisualElement Root => _root;

		private long _seenVersion = -1;
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

			_search = UiBuild.SearchBox(_toolbar, "Search logs…");
			_search.SetValueWithoutNotify(_filter.Text);
			_search.RegisterValueChangedCallback(OnSearchChanged);

			VisualElement controls = UiBuild.Element(OmniDebuggerUiClasses.LogsControls);
			_toolbar.Add(controls);

			_logFilter = CreateFilter(controls, IconGlyph.Message, OmniDebuggerUiClasses.LogsFilterLog, LogTypeMask.Log);
			_warningFilter = CreateFilter(controls, IconGlyph.Warning, OmniDebuggerUiClasses.LogsFilterWarning, LogTypeMask.Warning);
			_errorFilter = CreateFilter(controls, IconGlyph.Error, OmniDebuggerUiClasses.LogsFilterError, LogTypeMask.Error);

			_tagsButton = UiBuild.TextButton("Tags", ShowTags, OmniDebuggerUiClasses.LogsTags);
			controls.Add(_tagsButton);
			controls.Add(UiBuild.IconButton(IconGlyph.Copy, CopyAll, "Copy all"));
			controls.Add(UiBuild.IconButton(IconGlyph.Trash, ClearFeed, "Clear"));

			_list = new ListView(_items, EstimatedRowHeight, MakeRow, BindRow)
			{
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				selectionType = SelectionType.None,
			};

			_list.AddToClassList(OmniDebuggerUiClasses.LogsList);
			_listPage.Add(_list);

			_listScroll = _list.Q<ScrollView>();
			_drag = UiBuild.MakeDraggable(_listScroll);
			_drag.OnDragStarted += StopFollowing;
			_listScroll.verticalScroller.valueChanged += OnScrolled;
			_listScroll.verticalScroller.RegisterCallback<PointerDownEvent>(OnScrollerPressed, TrickleDown.TrickleDown);
			_listScroll.RegisterCallback<WheelEvent>(OnWheel);

			_empty = UiBuild.Label(NoLogsMessage, OmniDebuggerUiClasses.Empty);
			_listPage.Add(_empty);

			_follow = UiBuild.IconButton(IconGlyph.ArrowDown, FollowNewest, "Jump to the newest");
			_follow.AddToClassList(OmniDebuggerUiClasses.LogsFollow);
			_follow.AddManipulator(new Halo());
			UiBuild.SetVisible(_follow, false);
			_listPage.Add(_follow);

			_toast = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.Toast);
			_toast.pickingMode = PickingMode.Ignore;
			_root.Add(_toast);

			_tagsPage = new LogTagsPage(_filter, ShowList, OnFilterChanged);

			_poll = _root.schedule.Execute(Poll).Every(PollMs);
			_poll.Pause();
			_searchDebounce = _root.schedule.Execute(ApplySearch);
			_searchDebounce.Pause();
			_hideToast = _root.schedule.Execute(() => _toast.RemoveFromClassList(OmniDebuggerUiClasses.ToastVisible));
			_hideToast.Pause();
			_settle = _list.schedule.Execute(Settle).Every(SettleIntervalMs);
			_settle.Pause();

			RefreshFilterButtons();
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

		private VisualElement MakeRow() => new LogRow(Copy);

		private void BindRow(VisualElement element, int index)
		{
			if (element is LogRow row && index >= 0 && index < _items.Count)
			{
				row.Bind(_items[index]);
			}
		}

		private void Poll()
		{
			long version = _feed.Version;

			if (version != _seenVersion)
			{
				_seenVersion = version;
				RefreshCounts();

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
			bool more = _feed.Query(_filter.ToQuery(PageSize).After(_items[_items.Count - 1].Id), _page);

			if (more)
			{
				Reload();
				return false;
			}

			if (_page.Count == 0)
			{
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
			_filter.Text = _search.value ?? string.Empty;
			OnFilterChanged();
		}

		private void ToggleType(LogTypeMask type)
		{
			LogTypeMask types = _filter.Types ^ type;
			_filter.Types = types == LogTypeMask.None ? LogTypeMask.All : types;
			OnFilterChanged();
		}

		private void OnFilterChanged()
		{
			RefreshFilterButtons();
			Reload();
		}

		private void RefreshFilterButtons()
		{
			_logFilter.EnableInClassList(OmniDebuggerUiClasses.LogsFilterActive, (_filter.Types & LogTypeMask.Log) != 0);
			_warningFilter.EnableInClassList(OmniDebuggerUiClasses.LogsFilterActive, (_filter.Types & LogTypeMask.Warning) != 0);
			_errorFilter.EnableInClassList(OmniDebuggerUiClasses.LogsFilterActive, (_filter.Types & LogTypeMask.Error) != 0);

			int selected = _filter.Tags.Count;
			_tagsButton.text = selected > 0 ? $"Tags ({selected})" : "Tags";
			_tagsButton.EnableInClassList(OmniDebuggerUiClasses.ButtonPrimary, selected > 0);
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

		private void ShowTags()
		{
			_listPage.RemoveFromHierarchy();
			_root.Insert(0, _tagsPage.Root);
			_tagsPage.Show(_feed);
		}

		private void ShowList()
		{
			_tagsPage.Root.RemoveFromHierarchy();
			_root.Insert(0, _listPage);
			RefreshFilterButtons();
		}

		private void ClearFeed()
		{
			_feed.Clear();
			_seenVersion = -1;
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