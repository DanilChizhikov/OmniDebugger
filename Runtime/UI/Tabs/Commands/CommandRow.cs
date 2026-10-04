using System;
using System.Globalization;
using System.Text;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandRow : VisualElement, IValueRefresher, IDisposable
	{
		private const string ExecutedMessage = "Executed";
		private const string InvalidMessage = "Fix the highlighted parameters";
		private const string FailedMessage = "Failed, see the Logs tab";
		private const string MissingValue = "—";
		private const string RunTooltip = "Run";
		private const string MoreTooltip = "More";
		private const string PinTooltip = "Pin to the hotbar";
		private const string UnpinTooltip = "Unpin from the hotbar";
		private const string DescriptionLabel = "Description";

		private readonly ViewServices _services;
		private readonly ValuePulse _pulse;
		private readonly CommandDefinition _definition;
		private readonly CommandRowMode _mode;
		private readonly Action<CommandDefinition> _clicked;
		private readonly VisualElement _main;
		private readonly bool _showGroup;

		public CommandDefinition Definition => _definition;

		private ArgumentFieldRow _arguments;
		private SwitchField _switch;
		private Button _moreButton;
		private Button _pinButton;
		private IHotbar _hotbar;
		private Label _value;
		private Label _status;
		private object _lastRead;
		private object _lastShown;
		private bool _readable;
		private bool _hasRead;
		private bool _disposed;

		public CommandRow(
			ViewServices services,
			ValuePulse pulse,
			CommandDefinition definition,
			CommandRowMode mode,
			Action<CommandDefinition> clicked = null,
			bool showGroup = false)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_definition = definition ?? throw new ArgumentNullException(nameof(definition));
			_pulse = pulse;
			_mode = mode;
			_clicked = clicked;
			_showGroup = showGroup || mode == CommandRowMode.Summary;

			AddToClassList(OmniDebuggerUiClasses.Row);
			EnableInClassList(OmniDebuggerUiClasses.RowSummary, mode == CommandRowMode.Summary);
			EnableInClassList(OmniDebuggerUiClasses.RowCompact, mode == CommandRowMode.Compact);

			_main = UiBuild.Element(OmniDebuggerUiClasses.RowMain);
			Add(_main);

			BuildTitles();

			if (mode == CommandRowMode.Summary)
			{
				OmniIcon chevron = new OmniIcon(IconGlyph.Chevron);
				chevron.AddToClassList(OmniDebuggerUiClasses.RowChevron);
				_main.Add(chevron);
				focusable = true;
				MakeTappable();
				return;
			}

			BuildControl();

			if (mode == CommandRowMode.Full)
			{
				_pinButton = UiBuild.IconButton(IconGlyph.PinOutline, TogglePin, PinTooltip);
				_pinButton.AddToClassList(OmniDebuggerUiClasses.RowPin);
				_main.Add(_pinButton);
				_hotbar = _services.Debugger.Hotbar;
				ShowPin();
				_hotbar.OnChanged += ShowPin;

				_moreButton = UiBuild.IconButton(IconGlyph.More, OpenMenu, MoreTooltip);
				_moreButton.AddToClassList(OmniDebuggerUiClasses.RowMore);
				_main.Add(_moreButton);
			}
		}

		public bool Activate()
		{
			if (_disposed)
			{
				return false;
			}

			if (_switch != null)
			{
				_switch.value = !_switch.value;
				return true;
			}

			if (_definition.Kind != CommandKind.Action)
			{
				return false;
			}

			Execute();
			return true;
		}

		public void RefreshValue()
		{
			if (_disposed)
			{
				return;
			}

			if (_value != null)
			{
				_value.text = TryRead(out object value)
					? Format(value)
					: MissingValue;

				return;
			}

			if (_readable && !IsEdited())
			{
				ReadValueIntoField(force: false);
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_pulse?.Unregister(this);

			if (_hotbar != null)
			{
				_hotbar.OnChanged -= ShowPin;
			}

			if (_arguments != null)
			{
				_arguments.OnCommitted -= OnArgumentsCommitted;
				_arguments.Dispose();
			}

			UnregisterCallback<NavigationSubmitEvent>(OnSubmitted);
			RemoveFromHierarchy();
		}

		internal static string Format(object value)
		{
			if (value == null)
			{
				return MissingValue;
			}

			return value is IFormattable formattable
				? formattable.ToString(null, CultureInfo.InvariantCulture)
				: value.ToString();
		}

		private void BuildTitles()
		{
			if (_mode != CommandRowMode.Compact && _definition.IconKey != null)
			{
				_main.Add(BuildIcon());
			}

			VisualElement titles = UiBuild.Element(OmniDebuggerUiClasses.RowTitles);
			titles.Add(UiBuild.Label(_definition.Name, OmniDebuggerUiClasses.RowName));

			if (_showGroup)
			{
				titles.Add(UiBuild.Label(_definition.GroupPath, OmniDebuggerUiClasses.RowCaption));
			}

			_main.Add(titles);
		}

		private VisualElement BuildIcon()
		{
			VisualElement icon = UiBuild.Element(OmniDebuggerUiClasses.RowIcon);
			icon.Add(IconView.Create(_definition.IconKey, _services.Debugger.Icons, IconGlyph.Warning, OmniDebuggerUiClasses.RowIconImage));
			return icon;
		}

		private void BuildControl()
		{
			VisualElement control = UiBuild.Element(OmniDebuggerUiClasses.RowControl);
			_main.Add(control);

			switch (_definition.Kind)
			{
				case CommandKind.ReadonlyValue:
					_value = UiBuild.Label(MissingValue, OmniDebuggerUiClasses.RowValue);
					control.Add(_value);
					_pulse?.Register(this);
					RefreshValue();
					break;

				case CommandKind.Value:
					BuildArguments(control, showLabels: false);
					_arguments.OnCommitted += OnArgumentsCommitted;
					BindValue();
					BindSwitch();
					break;

				default:
					BuildArguments(control, showLabels: _definition.Arguments.Count > 1);

					Button run = UiBuild.IconButton(IconGlyph.TriangleRight, Execute, RunTooltip);
					run.AddToClassList(OmniDebuggerUiClasses.RowRun);
					run.AddManipulator(new Halo());
					control.Add(run);
					break;
			}

			_status = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.RowStatus);
			Add(_status);
			SetStatus(null, false);
		}

		private void BuildArguments(VisualElement control, bool showLabels)
		{
			_arguments = new ArgumentFieldRow(
				_services.Commands,
				_services.Debugger.Fields,
				_services.Debugger.Commands,
				showLabels);
			_arguments.Bind(_definition);

			if (_definition.Arguments.Count == 0)
			{
				return;
			}

			bool inline = _definition.Arguments.Count == 1 &&
				_arguments.Root.Q(className: OmniDebuggerUiClasses.Range) == null;

			if (inline)
			{
				control.Add(_arguments.Root);
				return;
			}

			VisualElement extra = UiBuild.Element(OmniDebuggerUiClasses.RowExtra);
			extra.Add(_arguments.Root);
			Add(extra);
		}

		private void BindValue()
		{
			_readable = true;
			ReadValueIntoField(force: true);

			if (_hasRead)
			{
				_pulse?.Register(this);
			}
		}

		private void BindSwitch()
		{
			_switch = _arguments.Root.Q<SwitchField>();

			if (_switch != null)
			{
				MakeTappable();
			}
		}

		private void MakeTappable()
		{
			AddToClassList(OmniDebuggerUiClasses.Tappable);
			RegisterCallback<NavigationSubmitEvent>(OnSubmitted);
		}

		private void Execute()
		{
			if (_disposed || _arguments == null)
			{
				return;
			}

			if (!_arguments.TryGetValues(out object[] values))
			{
				SetStatus(InvalidMessage, false);
				return;
			}

			_arguments.Remember();

			InvocationRequest request = new InvocationRequest(_services.Origin, values);

			if (!_services.Debugger.Commands.TryExecute(_definition, request))
			{
				SetStatus(FailedMessage, false);
				return;
			}

			if (_definition.Kind == CommandKind.Value)
			{
				if (_readable)
				{
					ReadValueIntoField(force: true);
				}

				SetStatus(null, false);
				return;
			}

			SetStatus(ExecutedMessage, true);
		}

		private void OnArgumentsCommitted()
		{
			if (_definition.Kind == CommandKind.Value)
			{
				Execute();
			}
		}

		private void ReadValueIntoField(bool force)
		{
			if (!TryRead(out object value))
			{
				_pulse?.Unregister(this);
				return;
			}

			if (!force && _hasRead && Equals(value, _lastRead))
			{
				return;
			}

			_lastRead = value;
			_hasRead = true;
			_arguments.SetSingleValue(value);
			_arguments.TryGetSingleValue(out _lastShown);
		}

		private bool IsEdited()
		{
			if (!_hasRead)
			{
				return false;
			}

			_arguments.TryGetSingleValue(out object shown);
			return !Equals(shown, _lastShown);
		}

		private void SetStatus(string message, bool success)
		{
			bool visible = !string.IsNullOrEmpty(message);

			_status.text = message ?? string.Empty;
			_status.EnableInClassList(OmniDebuggerUiClasses.RowStatusSuccess, visible && success);
			_status.EnableInClassList(OmniDebuggerUiClasses.RowStatusError, visible && !success);
			UiBuild.SetVisible(_status, visible);
		}

		private void OnSubmitted(NavigationSubmitEvent evt)
		{
			if (evt.target != this)
			{
				return;
			}

			if (_mode == CommandRowMode.Summary)
			{
				_clicked?.Invoke(_definition);
				return;
			}

			if (_switch != null && _switch.enabledInHierarchy)
			{
				_switch.value = !_switch.value;
			}
		}

		private void OpenMenu()
		{
			PopupLayer popups = _services.Popups;

			if (popups == null)
			{
				return;
			}

			VisualElement menu = UiBuild.Element(OmniDebuggerUiClasses.Menu);
			menu.Add(UiBuild.Label(_definition.Name, OmniDebuggerUiClasses.MenuTitle));
			menu.Add(UiBuild.Label(DescribeMeta(), OmniDebuggerUiClasses.MenuCaption));

			if (!string.IsNullOrEmpty(_definition.Description))
			{
				menu.Add(UiBuild.MenuItem(IconGlyph.Info, DescriptionLabel, ShowDescription));
			}

			popups.ShowAnchored(_moreButton, menu, onHidden: null);
		}

		private void TogglePin() => _hotbar.Toggle(_definition.Path);

		private void ShowPin()
		{
			bool pinned = _hotbar.Contains(_definition.Path);
			UiBuild.SetGlyph(_pinButton, pinned ? IconGlyph.Pin : IconGlyph.PinOutline);
			_pinButton.tooltip = pinned ? UnpinTooltip : PinTooltip;
			_pinButton.EnableInClassList(OmniDebuggerUiClasses.RowPinActive, pinned);
		}

		private bool TryRead(out object value)
		{
			if (!_services.Debugger.Commands.TryGet(_definition.Path, out _))
			{
				value = null;
				return false;
			}

			return _services.Debugger.Commands.TryGetValue(_definition.Path, out value);
		}

		private void ShowDescription()
		{
			Label text = UiBuild.Label(_definition.Description, OmniDebuggerUiClasses.PopupText);
			_services.Popups?.ShowCentered(_definition.Name, text);
		}

		private string DescribeMeta()
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(_definition.Path);

			if (_definition.Tags.Count > 0)
			{
				builder.Append('\n');
				builder.Append(string.Join(", ", _definition.Tags));
			}

			return builder.ToString();
		}
	}
}
