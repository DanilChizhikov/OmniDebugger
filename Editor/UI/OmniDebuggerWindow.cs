#if OMNI_DEBUGGER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI.Editor
{
	internal sealed class OmniDebuggerWindow : EditorWindow
	{
		private const string MenuPath = "Window/DTech/OmniDebugger";
		private const string WindowTitle = "OmniDebugger";
		private const string SearchPlaceholder = "Search commands (Ctrl/Cmd+K)";
		private const string WaitingMessage =
			"No live debugger. Enter play mode with OmniDebugger enabled — the newest OmniDebuggerHost shows up here on its own.";
		private const string EmptyMessage =
			"No commands yet. Register an object with debugger.Commands.Register(obj), or build some with debugger.Commands.Build().";
		private const string NothingSelectedMessage = "Select a command or a group.";
		private const long ValueRefreshMs = 250;
		private const float MinInspectorWidth = 160.0f;

		private readonly PanelModel _model = new ();
		private readonly CommandStateStore _states = new ();
		private readonly List<CommandInspector> _inspectors = new ();
		private readonly Dictionary<int, string> _pathsById = new ();
		private readonly Dictionary<string, int> _idsByPath = new (StringComparer.Ordinal);

		private IOmniDebuggerHost _debugger;
		private ToolbarToggle _commandsToggle;
		private ToolbarToggle _infoToggle;
		private ToolbarSearchField _search;
		private VisualElement _waiting;
		private VisualElement _commandsBody;
		private TwoPaneSplitView _split;
		private TreeView _tree;
		private ScrollView _inspector;
		private VisualElement _infoHost;
		private EditorInfoView _info;
		private SearchController _searchController;
		private IVisualElementScheduledItem _valueRefresh;
		private bool _restoringTree;

		[MenuItem(MenuPath)]
		private static void Open()
		{
			OmniDebuggerWindow window = GetWindow<OmniDebuggerWindow>();
			window.titleContent = new GUIContent(WindowTitle);
			window.minSize = new Vector2(420.0f, 240.0f);
			window.Show();
		}

		private void OnEnable()
		{
			OmniDebuggerViews.OnCurrentChanged += Bind;
			_states.Restore(EditorWindowPrefs.instance.Arguments);
			_states.OnChanged += SaveArguments;
		}

		private void OnDisable()
		{
			OmniDebuggerViews.OnCurrentChanged -= Bind;
			_states.OnChanged -= SaveArguments;
			Unbind();
			_searchController?.Dispose();
			_searchController = null;
		}

		private void CreateGUI()
		{
			VisualElement root = rootVisualElement;
			root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

			Toolbar toolbar = new Toolbar();
			_commandsToggle = new ToolbarToggle { text = "Commands" };
			_commandsToggle.RegisterValueChangedCallback(evt => { if (evt.newValue) SetMode(EditorWindowMode.Commands); else _commandsToggle.SetValueWithoutNotify(true); });
			_infoToggle = new ToolbarToggle { text = "Info" };
			_infoToggle.RegisterValueChangedCallback(evt => { if (evt.newValue) SetMode(EditorWindowMode.Info); else _infoToggle.SetValueWithoutNotify(true); });
			toolbar.Add(_commandsToggle);
			toolbar.Add(_infoToggle);
			toolbar.Add(new ToolbarSpacer { flex = true });

			_search = new ToolbarSearchField { tooltip = SearchPlaceholder };
			_search.style.width = 220.0f;
			_search.RegisterValueChangedCallback(evt => _searchController.SetQuery(evt.newValue));
			toolbar.Add(_search);
			root.Add(toolbar);

			_waiting = new VisualElement();
			_waiting.style.flexGrow = 1.0f;
			_waiting.style.paddingLeft = 8.0f;
			_waiting.style.paddingRight = 8.0f;
			_waiting.style.paddingTop = 8.0f;
			_waiting.Add(new HelpBox(WaitingMessage, HelpBoxMessageType.Info));
			Button play = new Button(() => EditorApplication.EnterPlaymode()) { text = "Enter Play Mode" };
			play.style.alignSelf = Align.FlexStart;
			_waiting.Add(play);
			root.Add(_waiting);

			BuildCommandsBody(root);

			_infoHost = new VisualElement();
			_infoHost.style.flexGrow = 1.0f;
			root.Add(_infoHost);

			_searchController = new SearchController(root);
			_searchController.OnResultsChanged += RebuildTree;
			_searchController.Resume();

			_valueRefresh = root.schedule.Execute(RefreshValues).Every(ValueRefreshMs);

			Bind();
		}

		private void BuildCommandsBody(VisualElement root)
		{
			_commandsBody = new VisualElement();
			_commandsBody.style.flexGrow = 1.0f;
			root.Add(_commandsBody);

			_split = new TwoPaneSplitView(0, EditorWindowPrefs.instance.SplitWidth, TwoPaneSplitViewOrientation.Horizontal);
			_commandsBody.Add(_split);

			_tree = new TreeView
			{
				makeItem = () => new Label(),
				fixedItemHeight = 20.0f,
				selectionType = SelectionType.Single,
			};

			_tree.bindItem = (element, index) =>
			{
				TreeNode node = _tree.GetItemDataForIndex<TreeNode>(index);
				Label label = (Label)element;
				label.text = node.Label;
				label.style.unityFontStyleAndWeight = node.Definition == null ? FontStyle.Bold : FontStyle.Normal;
				label.tooltip = node.Path;
			};

			_tree.selectionChanged += OnTreeSelectionChanged;
			_tree.itemExpandedChanged += OnTreeExpandedChanged;
			_tree.style.minWidth = EditorWindowPrefs.MinSplitWidth;
			_tree.RegisterCallback<GeometryChangedEvent>(OnTreeGeometryChanged);
			_split.Add(_tree);

			_inspector = new ScrollView(ScrollViewMode.Vertical);
			_inspector.style.minWidth = MinInspectorWidth;
			_inspector.contentContainer.style.paddingLeft = 8.0f;
			_inspector.contentContainer.style.paddingRight = 8.0f;
			_inspector.contentContainer.style.paddingTop = 6.0f;
			_split.Add(_inspector);
		}

		private void Bind()
		{
			if (_tree == null)
			{
				return;
			}

			IOmniDebuggerHost next = OmniDebuggerViews.Current;
			if (ReferenceEquals(next, _debugger))
			{
				return;
			}

			Unbind();
			_debugger = next;

			bool live = _debugger != null;
			_waiting.style.display = live ? DisplayStyle.None : DisplayStyle.Flex;

			if (!live)
			{
				_commandsBody.style.display = DisplayStyle.None;
				_infoHost.style.display = DisplayStyle.None;
				_search.SetEnabled(false);
				return;
			}

			try
			{
				_debugger.Commands.OnChanged += OnCommandsChanged;
			}
			catch (ObjectDisposedException)
			{
				_debugger = null;
				_waiting.style.display = DisplayStyle.Flex;
				return;
			}

			_info = new EditorInfoView(_debugger.Info);
			_infoHost.Add(_info.Root);

			OnCommandsChanged();
			SetMode(EditorWindowPrefs.instance.Mode);
		}

		private void Unbind()
		{
			ClearInspector();

			if (_debugger != null)
			{
				try
				{
					_debugger.Commands.OnChanged -= OnCommandsChanged;
				}
				catch (ObjectDisposedException)
				{
				}
			}

			_info?.Dispose();
			_info = null;
			_debugger = null;
		}

		private void SetMode(EditorWindowMode mode)
		{
			EditorWindowPrefs.instance.Mode = mode;

			bool commands = mode == EditorWindowMode.Commands;
			_commandsToggle.SetValueWithoutNotify(commands);
			_infoToggle.SetValueWithoutNotify(!commands);

			if (_debugger == null)
			{
				return;
			}

			_commandsBody.style.display = commands ? DisplayStyle.Flex : DisplayStyle.None;
			_infoHost.style.display = commands ? DisplayStyle.None : DisplayStyle.Flex;
			_search.SetEnabled(commands);
			_info?.SetActive(!commands);

			if (commands)
			{
				RestoreSplit();
			}
		}

		private void RestoreSplit() => _split.fixedPane.style.width = EditorWindowPrefs.instance.SplitWidth;

		private void OnTreeGeometryChanged(GeometryChangedEvent evt)
		{
			if (_commandsBody.resolvedStyle.display != DisplayStyle.Flex || evt.newRect.width < EditorWindowPrefs.MinSplitWidth)
			{
				return;
			}

			EditorWindowPrefs.instance.SplitWidth = evt.newRect.width;
		}

		private void OnCommandsChanged()
		{
			if (_debugger == null)
			{
				return;
			}

			_model.Rebuild(_debugger.Commands.All, _debugger.Groups);
			_searchController.SetSource(_model.All);
			RebuildTree();
		}

		private void RebuildTree()
		{
			if (_debugger == null || _tree == null)
			{
				return;
			}

			_pathsById.Clear();
			_idsByPath.Clear();

			List<TreeViewItemData<TreeNode>> roots = _searchController.HasQuery
				? BuildSearchTree()
				: BuildGroupTree(string.Empty);

			_restoringTree = true;
			_tree.SetRootItems(roots);
			_tree.Rebuild();

			foreach (KeyValuePair<string, int> pair in _idsByPath)
			{
				if (_searchController.HasQuery || EditorWindowPrefs.instance.IsExpanded(pair.Key))
				{
					_tree.ExpandItem(pair.Value, expandAllChildren: false, refresh: false);
				}
			}

			_tree.RefreshItems();

			string selected = EditorWindowPrefs.instance.SelectedPath;
			if (selected != null && _idsByPath.TryGetValue(selected, out int id))
			{
				_tree.SetSelectionById(id);
				_tree.ScrollToItemById(id);
			}
			else
			{
				ShowInspector(null);
			}

			_restoringTree = false;
		}

		private List<TreeViewItemData<TreeNode>> BuildGroupTree(string groupPath)
		{
			List<TreeViewItemData<TreeNode>> items = new List<TreeViewItemData<TreeNode>>();

			foreach (CommandDefinition definition in _model.GetCommands(groupPath))
			{
				items.Add(new TreeViewItemData<TreeNode>(Register(definition.Path), new TreeNode(definition.Path, definition.Name, definition)));
			}

			foreach (string child in _model.GetChildren(groupPath))
			{
				items.Add(new TreeViewItemData<TreeNode>(
					Register(child),
					new TreeNode(child, CommandPath.GetName(child), null),
					BuildGroupTree(child)));
			}

			return items;
		}

		private List<TreeViewItemData<TreeNode>> BuildSearchTree()
		{
			List<TreeViewItemData<TreeNode>> roots = new List<TreeViewItemData<TreeNode>>();
			Dictionary<string, List<TreeViewItemData<TreeNode>>> byGroup = new (StringComparer.Ordinal);

			foreach (CommandDefinition definition in _searchController.Results)
			{
				if (!byGroup.TryGetValue(definition.GroupPath, out List<TreeViewItemData<TreeNode>> children))
				{
					children = new List<TreeViewItemData<TreeNode>>();
					byGroup.Add(definition.GroupPath, children);
				}

				children.Add(new TreeViewItemData<TreeNode>(Register(definition.Path), new TreeNode(definition.Path, definition.Name, definition)));
			}

			foreach (KeyValuePair<string, List<TreeViewItemData<TreeNode>>> pair in byGroup)
			{
				string label = pair.Key.Replace(CommandPath.Separator.ToString(), " / ");
				roots.Add(new TreeViewItemData<TreeNode>(Register(pair.Key), new TreeNode(pair.Key, label, null), pair.Value));
			}

			return roots;
		}

		private int Register(string path)
		{
			int id = _pathsById.Count + 1;
			_pathsById[id] = path;
			_idsByPath[path] = id;
			return id;
		}

		private void OnTreeSelectionChanged(IEnumerable<object> selection)
		{
			TreeNode node = null;
			foreach (object item in selection)
			{
				node = item as TreeNode;
				break;
			}

			if (!_restoringTree || node != null)
			{
				EditorWindowPrefs.instance.SelectedPath = node?.Path;
			}

			ShowInspector(node);
		}

		private void OnTreeExpandedChanged(TreeViewExpansionChangedArgs args)
		{
			if (_restoringTree || _searchController.HasQuery || !_pathsById.TryGetValue(args.id, out string path))
			{
				return;
			}

			EditorWindowPrefs.instance.SetExpanded(path, args.isExpanded);
		}

		private void ShowInspector(TreeNode node)
		{
			ClearInspector();

			if (_debugger == null)
			{
				return;
			}

			if (_model.All.Count == 0)
			{
				_inspector.Add(new HelpBox(EmptyMessage, HelpBoxMessageType.Info));
				return;
			}

			if (node == null)
			{
				_inspector.Add(new HelpBox(NothingSelectedMessage, HelpBoxMessageType.None));
				return;
			}

			if (node.Definition != null)
			{
				AddInspector(node.Definition, compact: false);
				return;
			}

			Label title = new Label(node.Path.Replace(CommandPath.Separator.ToString(), " / "));
			title.style.unityFontStyleAndWeight = FontStyle.Bold;
			title.style.fontSize = 14;
			title.style.marginBottom = 6.0f;
			_inspector.Add(title);

			IReadOnlyList<CommandDefinition> commands = _model.GetCommands(node.Path);
			for (int i = 0; i < commands.Count; i++)
			{
				AddInspector(commands[i], compact: true);
			}

			if (commands.Count == 0)
			{
				_inspector.Add(new HelpBox("This group only holds other groups.", HelpBoxMessageType.None));
			}
		}

		private void AddInspector(CommandDefinition definition, bool compact)
		{
			CommandInspector inspector = new CommandInspector(_debugger, definition, _states, compact);
			_inspectors.Add(inspector);
			_inspector.Add(inspector.Root);
		}

		private void ClearInspector()
		{
			for (int i = 0; i < _inspectors.Count; i++)
			{
				_inspectors[i].Dispose();
			}

			_inspectors.Clear();
			_inspector?.Clear();
		}

		private void RefreshValues()
		{
			for (int i = 0; i < _inspectors.Count; i++)
			{
				_inspectors[i].RefreshValue();
			}
		}

		private void SaveArguments() => EditorWindowPrefs.instance.Arguments = _states.Serialize();

		private void OnKeyDown(KeyDownEvent evt)
		{
			if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.K)
			{
				SetMode(EditorWindowMode.Commands);
				_search.Focus();
				evt.StopPropagation();
			}
		}

		private sealed class TreeNode
		{
			public readonly string Path;
			public readonly string Label;
			public readonly CommandDefinition Definition;

			public TreeNode(string path, string label, CommandDefinition definition)
			{
				Path = path;
				Label = label;
				Definition = definition;
			}
		}
	}
}
#endif
