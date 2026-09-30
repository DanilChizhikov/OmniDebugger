using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandPalette : IDisposable
	{
		private const string Placeholder = "Run a command…";
		private const string EmptyMessage = "No commands match.";
		private const string Hint = "↑↓ pick · Enter run · Esc close";
		private const int IdleResults = 30;

		private readonly ViewServices _services;
		private readonly Action<CommandDefinition> _reveal;
		private readonly VisualElement _root;
		private readonly TextField _field;
		private readonly ScrollView _list;
		private readonly SearchController _search;
		private readonly List<CommandDefinition> _shown = new ();
		private readonly List<CommandRow> _rows = new ();

		public bool IsShowing { get; private set; }

		private int _selected;
		private bool _disposed;

		public CommandPalette(ViewServices services, Action<CommandDefinition> reveal)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_reveal = reveal ?? throw new ArgumentNullException(nameof(reveal));

			_root = UiBuild.Element(OmniDebuggerUiClasses.Palette);

			VisualElement toolbar = UiBuild.Element(OmniDebuggerUiClasses.Toolbar);
			_field = UiBuild.SearchBox(toolbar, Placeholder);
			_field.RegisterValueChangedCallback(evt => _search.SetQuery(evt.newValue));
			_field.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
			_root.Add(toolbar);

			_list = UiBuild.Scroll();
			_list.AddToClassList(OmniDebuggerUiClasses.PaletteList);
			_root.Add(_list);

			_root.Add(UiBuild.Label(Hint, OmniDebuggerUiClasses.PaletteHint));

			_search = new SearchController(_root);
			_search.OnResultsChanged += ShowResults;
		}

		public void Show()
		{
			if (_disposed || _services.Popups == null)
			{
				return;
			}

			_search.SetSource(_services.Debugger.Commands.All);
			_field.SetValueWithoutNotify(string.Empty);
			_search.SetQuery(string.Empty);

			IsShowing = true;
			_services.Popups.ShowCentered(_root, OmniDebuggerUiClasses.PopupPalette, OnHidden);
			_search.Resume();
			ShowResults();
			_field.schedule.Execute(() => _field.Focus());
		}

		public void Hide()
		{
			if (IsShowing)
			{
				_services.Popups.Hide();
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			Hide();
			ClearRows();
			_search.OnResultsChanged -= ShowResults;
			_search.Dispose();
		}

		private void OnHidden()
		{
			IsShowing = false;
			_search.Pause();
			ClearRows();
		}

		private void ShowResults()
		{
			if (_disposed || !IsShowing)
			{
				return;
			}

			ClearRows();
			_shown.Clear();

			if (_search.HasQuery)
			{
				_shown.AddRange(_search.Results);
			}
			else
			{
				CollectIdle();
			}

			if (_shown.Count == 0)
			{
				_list.Add(UiBuild.Label(EmptyMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			for (int i = 0; i < _shown.Count; i++)
			{
				CommandRow row = new CommandRow(_services, null, _shown[i], CommandRowMode.Summary, Activate);
				row.EnableInClassList(OmniDebuggerUiClasses.First, i == 0);
				_rows.Add(row);
				_list.Add(row);
			}

			Select(0);
		}

		private void CollectIdle()
		{
			ICommandRegistry commands = _services.Debugger.Commands;
			IReadOnlyList<string> pinned = _services.Debugger.Hotbar.Paths;

			for (int i = 0; i < pinned.Count && _shown.Count < IdleResults; i++)
			{
				if (commands.TryGet(pinned[i], out CommandDefinition definition))
				{
					_shown.Add(definition);
				}
			}

			IReadOnlyList<CommandDefinition> all = commands.All;
			for (int i = 0; i < all.Count && _shown.Count < IdleResults; i++)
			{
				if (!_services.Debugger.Hotbar.Contains(all[i].Path))
				{
					_shown.Add(all[i]);
				}
			}
		}

		private void OnKeyDown(KeyDownEvent evt)
		{
			switch (evt.keyCode)
			{
				case UnityEngine.KeyCode.DownArrow:
					Select(_selected + 1);
					break;

				case UnityEngine.KeyCode.UpArrow:
					Select(_selected - 1);
					break;

				case UnityEngine.KeyCode.Return:
				case UnityEngine.KeyCode.KeypadEnter:
					if (_selected >= 0 && _selected < _shown.Count)
					{
						Activate(_shown[_selected]);
					}

					break;

				case UnityEngine.KeyCode.Escape:
					Hide();
					break;

				default:
					return;
			}

			evt.StopPropagation();
		}

		private void Select(int index)
		{
			if (_rows.Count == 0)
			{
				_selected = -1;
				return;
			}

			_selected = Math.Clamp(index, 0, _rows.Count - 1);

			for (int i = 0; i < _rows.Count; i++)
			{
				_rows[i].EnableInClassList(OmniDebuggerUiClasses.RowSelected, i == _selected);
			}

			_list.ScrollTo(_rows[_selected]);
		}

		private void Activate(CommandDefinition definition)
		{
			if (definition == null)
			{
				return;
			}

			Hide();

			if (TryRun(definition))
			{
				return;
			}

			_reveal(definition);
		}

		private bool TryRun(CommandDefinition definition)
		{
			ICommandRegistry commands = _services.Debugger.Commands;

			if (definition.Kind == CommandKind.Value &&
				definition.Arguments[0].Type == typeof(bool) &&
				commands.TryGetValue(definition.Path, out object current) &&
				current is bool on)
			{
				return commands.TryExecute(definition, new InvocationRequest(_services.Origin, new object[] { !on }));
			}

			if (definition.Kind != CommandKind.Action || !TryCollectArguments(definition, out object[] values))
			{
				return false;
			}

			commands.TryExecute(definition, new InvocationRequest(_services.Origin, values));
			return true;
		}

		private bool TryCollectArguments(CommandDefinition definition, out object[] values)
		{
			IReadOnlyList<ArgumentDefinition> arguments = definition.Arguments;
			values = new object[arguments.Count];

			CommandState state = null;
			_services.Commands?.TryGet(definition.Path, out state);

			for (int i = 0; i < arguments.Count; i++)
			{
				ArgumentDefinition argument = arguments[i];

				if (state != null && state.TryGetArgument(argument, out object remembered))
				{
					values[i] = remembered;
				}
				else if (argument.IsOptional)
				{
					values[i] = argument.DefaultValue;
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		private void ClearRows()
		{
			for (int i = 0; i < _rows.Count; i++)
			{
				_rows[i].Dispose();
			}

			_rows.Clear();
			_list.Clear();
			_selected = -1;
		}
	}
}
