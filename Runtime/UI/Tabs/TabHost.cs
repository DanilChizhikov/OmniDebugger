using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class TabHost : IDisposable
	{
		public event Action OnSelectionChanged;

		private const string EmptyMessage =
			"No tabs registered. Register one with IOmniDebugger.Tabs.Register(factory).";

		private readonly VisualElement _bar;
		private readonly ScrollView _barScroll;
		private readonly VisualElement _body;
		private readonly IOmniDebugger _debugger;
		private readonly ITabRegistry _tabs;
		private readonly OmniDebuggerViewState _state;
		private readonly string _origin;
		private readonly ViewServices _services;
		private readonly Dictionary<string, Entry> _entries = new (StringComparer.Ordinal);
		private readonly Dictionary<string, Button> _buttons = new (StringComparer.Ordinal);
		private readonly Label _empty;

		private string _selectedId;
		private bool _panelOpen;
		private bool _disposed;

		public TabHost(
			VisualElement bar,
			VisualElement body,
			IOmniDebugger debugger,
			OmniDebuggerViewState state,
			string origin,
			ViewServices services)
		{
			_bar = bar ?? throw new ArgumentNullException(nameof(bar));
			_body = body ?? throw new ArgumentNullException(nameof(body));
			_debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
			_tabs = debugger.Tabs;
			_state = state ?? throw new ArgumentNullException(nameof(state));
			_origin = origin;
			_services = services;

			_barScroll = new ScrollView(ScrollViewMode.Horizontal)
			{
				horizontalScrollerVisibility = ScrollerVisibility.Hidden,
				verticalScrollerVisibility = ScrollerVisibility.Hidden,
			};

			_barScroll.AddToClassList(OmniDebuggerUiClasses.TabBarScroll);
			_barScroll.contentContainer.AddToClassList(OmniDebuggerUiClasses.TabBarContent);
			UiBuild.MakeDraggable(_barScroll);
			_bar.Add(_barScroll);

			_empty = new Label(EmptyMessage);
			_empty.AddToClassList(OmniDebuggerUiClasses.Status);
			_body.Add(_empty);
		}

		public void SetVertical(bool vertical)
		{
			ScrollViewMode mode = vertical ? ScrollViewMode.Vertical : ScrollViewMode.Horizontal;

			if (_barScroll.mode == mode)
			{
				return;
			}

			_barScroll.mode = mode;
			_barScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
			_barScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;

			if (_selectedId != null && _buttons.TryGetValue(_selectedId, out Button selected))
			{
				_barScroll.schedule.Execute(() => _barScroll.ScrollTo(selected));
			}
		}

		public void Rebuild()
		{
			if (_disposed)
			{
				return;
			}

			IReadOnlyList<IOmniDebuggerTabFactory> factories = _tabs.All;

			DropStaleEntries(factories);
			RebuildBar(factories);

			string selected = ResolveSelection(factories);

			if (selected == null)
			{
				_selectedId = null;
				_empty.style.display = DisplayStyle.Flex;
				return;
			}

			_empty.style.display = DisplayStyle.None;
			Show(selected);
		}

		public void Select(string id)
		{
			if (_disposed || string.IsNullOrWhiteSpace(id) || string.Equals(id, _selectedId, StringComparison.Ordinal))
			{
				return;
			}

			Show(id);
		}

		public void SetPanelOpen(bool open)
		{
			if (_disposed || open == _panelOpen)
			{
				return;
			}

			_panelOpen = open;

			if (!TryGetSelectedTab(out IOmniDebuggerTab tab))
			{
				return;
			}

			if (open)
			{
				tab.OnOpen();
			}
			else
			{
				tab.OnClose();
			}
		}

		public void Refresh()
		{
			if (_disposed || !TryGetSelectedTab(out IOmniDebuggerTab tab))
			{
				return;
			}

			tab.Refresh();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			foreach (KeyValuePair<string, Entry> pair in _entries)
			{
				DisposeEntry(pair.Value);
			}

			_entries.Clear();
			_buttons.Clear();
			_barScroll.Clear();
			_bar.Clear();
			_body.Clear();
			OnSelectionChanged = null;
		}

		private static void DisposeEntry(Entry entry)
		{
			if (entry.Tab == null)
			{
				return;
			}

			entry.Tab.Root?.RemoveFromHierarchy();
			entry.Tab.Dispose();
		}

		private static bool IsCurrent(IReadOnlyList<IOmniDebuggerTabFactory> factories, IOmniDebuggerTabFactory factory)
		{
			for (int i = 0; i < factories.Count; i++)
			{
				if (ReferenceEquals(factories[i], factory))
				{
					return true;
				}
			}

			return false;
		}

		private static bool Contains(IReadOnlyList<IOmniDebuggerTabFactory> factories, string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				return false;
			}

			for (int i = 0; i < factories.Count; i++)
			{
				if (string.Equals(factories[i].Id, id, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private void Show(string id)
		{
			if (!TryGetFactory(id, out IOmniDebuggerTabFactory factory))
			{
				return;
			}

			if (string.Equals(id, _selectedId, StringComparison.Ordinal) &&
				TryGetSelectedTab(out IOmniDebuggerTab current) &&
				current.Root != null &&
				current.Root.parent == _body)
			{
				UpdateButtons(id);
				return;
			}

			if (TryGetSelectedTab(out IOmniDebuggerTab previous))
			{
				if (_panelOpen)
				{
					previous.OnClose();
				}

				previous.Root?.RemoveFromHierarchy();
			}

			_selectedId = id;
			_state.SelectedTabId = id;
			UpdateButtons(id);

			IOmniDebuggerTab tab = GetOrCreate(factory);

			if (tab?.Root == null)
			{
				return;
			}

			_body.Add(tab.Root);

			if (_panelOpen)
			{
				tab.OnOpen();
			}

			tab.Refresh();
			OnSelectionChanged?.Invoke();
		}

		private void UpdateButtons(string id)
		{
			foreach (KeyValuePair<string, Button> pair in _buttons)
			{
				pair.Value.EnableInClassList(
					OmniDebuggerUiClasses.TabSelected,
					string.Equals(pair.Key, id, StringComparison.Ordinal));
			}
		}

		private IOmniDebuggerTab GetOrCreate(IOmniDebuggerTabFactory factory)
		{
			if (_entries.TryGetValue(factory.Id, out Entry entry) && entry.Tab != null)
			{
				return entry.Tab;
			}

			OmniDebuggerTabContext context = new OmniDebuggerTabContext(
				_debugger,
				_state.GetTabState(factory.Id),
				_origin,
				_services);

			IOmniDebuggerTab tab;

			try
			{
				tab = factory.CreateTab(context);
			}
			catch (Exception exception)
			{
				UnityLogSink.Default.Exception($"Tab failed to build. Id: {factory.Id}.", exception);
				return null;
			}

			_entries[factory.Id] = new Entry(factory, tab);
			return tab;
		}

		private void RebuildBar(IReadOnlyList<IOmniDebuggerTabFactory> factories)
		{
			_barScroll.Clear();
			_buttons.Clear();
			_bar.style.display = factories.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;

			for (int i = 0; i < factories.Count; i++)
			{
				IOmniDebuggerTabFactory factory = factories[i];
				string id = factory.Id;

				Button button = new Button(() => Select(id))
				{
					text = factory.DisplayName,
				};

				button.AddToClassList(OmniDebuggerUiClasses.Tab);
				_barScroll.Add(button);
				_buttons[id] = button;
			}
		}

		private void DropStaleEntries(IReadOnlyList<IOmniDebuggerTabFactory> factories)
		{
			List<string> stale = null;

			foreach (KeyValuePair<string, Entry> pair in _entries)
			{
				if (IsCurrent(factories, pair.Value.Factory))
				{
					continue;
				}

				stale ??= new List<string>();
				stale.Add(pair.Key);
			}

			if (stale == null)
			{
				return;
			}

			for (int i = 0; i < stale.Count; i++)
			{
				DisposeEntry(_entries[stale[i]]);
				_entries.Remove(stale[i]);
			}
		}

		private string ResolveSelection(IReadOnlyList<IOmniDebuggerTabFactory> factories)
		{
			if (factories.Count == 0)
			{
				return null;
			}

			if (Contains(factories, _selectedId))
			{
				return _selectedId;
			}

			if (Contains(factories, _state.SelectedTabId))
			{
				return _state.SelectedTabId;
			}

			return factories[0].Id;
		}

		private bool TryGetFactory(string id, out IOmniDebuggerTabFactory factory)
		{
			IReadOnlyList<IOmniDebuggerTabFactory> factories = _tabs.All;

			for (int i = 0; i < factories.Count; i++)
			{
				if (string.Equals(factories[i].Id, id, StringComparison.Ordinal))
				{
					factory = factories[i];
					return true;
				}
			}

			factory = null;
			return false;
		}

		private bool TryGetSelectedTab(out IOmniDebuggerTab tab)
		{
			if (_selectedId != null && _entries.TryGetValue(_selectedId, out Entry entry) && entry.Tab != null)
			{
				tab = entry.Tab;
				return true;
			}

			tab = null;
			return false;
		}

		private readonly struct Entry
		{
			public IOmniDebuggerTabFactory Factory { get; }
			public IOmniDebuggerTab Tab { get; }

			public Entry(IOmniDebuggerTabFactory factory, IOmniDebuggerTab tab)
			{
				Factory = factory;
				Tab = tab;
			}
		}
	}
}
