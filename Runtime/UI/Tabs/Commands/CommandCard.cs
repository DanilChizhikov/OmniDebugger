using System;
using System.Globalization;
using System.Text;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandCard : VisualElement, IValueRefresher, IDisposable
	{
		private const string ExecuteLabel = "Execute";
		private const string ExecutedMessage = "Executed";
		private const string InvalidMessage = "Fix the highlighted parameters";
		private const string FailedMessage = "Failed, see the Logs tab";
		private const string MissingValue = "—";
		private const string CurrentValueCaption = "Current value";

		private readonly ViewServices _services;
		private readonly ValuePulse _pulse;
		private readonly CommandDefinition _definition;
		private readonly CommandCardMode _mode;
		private readonly Action<CommandDefinition> _clicked;

		public CommandDefinition Definition => _definition;

		private Button _pinButton;
		private Button _starButton;
		private ArgumentFieldRow _arguments;
		private Label _value;
		private Label _status;
		private bool _disposed;

		public CommandCard(
			ViewServices services,
			ValuePulse pulse,
			CommandDefinition definition,
			CommandCardMode mode,
			Action<CommandDefinition> clicked = null)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_definition = definition ?? throw new ArgumentNullException(nameof(definition));
			_pulse = pulse;
			_mode = mode;
			_clicked = clicked;

			AddToClassList(OmniDebuggerUiClasses.Command);
			AddToClassList(mode == CommandCardMode.Compact ? OmniDebuggerUiClasses.CommandCompact : OmniDebuggerUiClasses.Card);

			BuildHeader();

			if (mode == CommandCardMode.Summary)
			{
				AddToClassList(OmniDebuggerUiClasses.CommandClickable);
				AddToClassList(OmniDebuggerUiClasses.Tappable);
				RegisterCallback<NavigationSubmitEvent>(OnCardSubmitted);
			}
			else
			{
				BuildBody();
			}
		}

		public void RefreshValue()
		{
			if (_disposed || _value == null)
			{
				return;
			}

			_value.text = _services.Debugger.Commands.TryGetValue(_definition.Key, out object value)
				? Format(value)
				: MissingValue;
		}

		public void ClearStatus()
		{
			if (_status != null)
			{
				SetStatus(null, false);
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
			_services.Favorites.OnChanged -= RefreshMarks;

			if (_services.Pins != null)
			{
				_services.Pins.OnChanged -= RefreshMarks;
			}

			if (_arguments != null)
			{
				_arguments.OnCommitted -= OnArgumentsCommitted;
				_arguments.Dispose();
			}

			UnregisterCallback<NavigationSubmitEvent>(OnCardSubmitted);
			RemoveFromHierarchy();
		}

		private static string Format(object value)
		{
			if (value == null)
			{
				return MissingValue;
			}

			return value is IFormattable formattable
				? formattable.ToString(null, CultureInfo.InvariantCulture)
				: value.ToString();
		}

		private void BuildHeader()
		{
			VisualElement header = UiBuild.Element(OmniDebuggerUiClasses.CommandHeader);
			Add(header);

			if (_mode != CommandCardMode.Compact && !_definition.Icon.IsEmpty)
			{
				header.Add(BuildIcon());
			}

			VisualElement titles = UiBuild.Element(OmniDebuggerUiClasses.CommandTitles);
			titles.Add(UiBuild.Label(_definition.Name, OmniDebuggerUiClasses.CommandName));

			if (_mode != CommandCardMode.Compact)
			{
				titles.Add(UiBuild.Label(DescribeMeta(), OmniDebuggerUiClasses.CommandMeta));
			}

			header.Add(titles);

			if (_mode == CommandCardMode.Compact)
			{
				return;
			}

			VisualElement actions = UiBuild.Element(OmniDebuggerUiClasses.CommandActions);
			header.Add(actions);

			if (!string.IsNullOrEmpty(_definition.Description))
			{
				actions.Add(UiBuild.IconButton(IconGlyph.Info, ShowDescription, "Description"));
			}

			if (_services.Pins != null)
			{
				_pinButton = UiBuild.IconButton(IconGlyph.PinOutline, TogglePin, "Pin to a floating window");
				actions.Add(_pinButton);
				_services.Pins.OnChanged += RefreshMarks;
			}

			_starButton = UiBuild.IconButton(IconGlyph.StarOutline, ToggleFavorite, "Favourite");
			actions.Add(_starButton);
			_services.Favorites.OnChanged += RefreshMarks;

			RefreshMarks();
		}

		private VisualElement BuildIcon()
		{
			VisualElement icon = UiBuild.Element(OmniDebuggerUiClasses.CommandIcon);

			if (_services.Debugger.Icons.TryGet(_definition.Icon, out Background background))
			{
				icon.style.backgroundImage = background;
			}
			else
			{
				icon.Add(new OmniIcon(IconGlyph.Warning));
				icon.style.alignItems = Align.Center;
				icon.style.justifyContent = Justify.Center;
			}

			return icon;
		}

		private void BuildBody()
		{
			VisualElement body = UiBuild.Element(OmniDebuggerUiClasses.CommandBody);
			Add(body);

			switch (_definition.Kind)
			{
				case CommandKind.ReadonlyValue:
					body.Add(UiBuild.Label(CurrentValueCaption, OmniDebuggerUiClasses.CommandValueCaption));
					_value = UiBuild.Label(MissingValue, OmniDebuggerUiClasses.CommandValue);
					body.Add(_value);
					_pulse?.Register(this);
					RefreshValue();
					break;

				case CommandKind.Value:
					_arguments = new ArgumentFieldRow(_services.Arguments, _services.Debugger.Fields, showLabels: false);
					_arguments.Bind(_definition);
					_arguments.OnCommitted += OnArgumentsCommitted;
					body.Add(_arguments.Root);
					ReadValueIntoField();
					AddStatus(body, withExecute: false);
					break;

				default:
					_arguments = new ArgumentFieldRow(_services.Arguments, _services.Debugger.Fields, showLabels: true);
					_arguments.Bind(_definition);
					UiBuild.SetVisible(_arguments.Root, _definition.Arguments.Count > 0);
					body.Add(_arguments.Root);
					AddStatus(body, withExecute: true);
					break;
			}
		}

		private void AddStatus(VisualElement body, bool withExecute)
		{
			VisualElement footer = UiBuild.Element(OmniDebuggerUiClasses.CommandFooter);
			body.Add(footer);

			_status = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.CommandStatus);
			footer.Add(_status);

			if (withExecute)
			{
				footer.Add(UiBuild.TextButton(ExecuteLabel, Execute, OmniDebuggerUiClasses.CommandExecute));
			}

			SetStatus(null, false);
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

			InvocationRequest request = new InvocationRequest(_services.Origin, values);

			if (!_services.Debugger.Commands.TryExecute(_definition, request))
			{
				SetStatus(FailedMessage, false);
				return;
			}

			if (_definition.Kind == CommandKind.Value)
			{
				ReadValueIntoField();
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

		private void ReadValueIntoField()
		{
			if (_services.Debugger.Commands.TryGetValue(_definition.Key, out object value))
			{
				_arguments.SetSingleValue(value);
			}
		}

		private void SetStatus(string message, bool success)
		{
			bool visible = !string.IsNullOrEmpty(message);

			_status.text = message ?? string.Empty;
			_status.EnableInClassList(OmniDebuggerUiClasses.CommandStatusSuccess, visible && success);
			_status.EnableInClassList(OmniDebuggerUiClasses.CommandStatusError, visible && !success);

			if (_definition.Kind == CommandKind.Value)
			{
				UiBuild.SetVisible(_status.parent, visible);
			}
		}

		private void ShowDescription()
		{
			Label text = UiBuild.Label(_definition.Description, OmniDebuggerUiClasses.PopupText);
			_services.Popups?.ShowCentered(_definition.Name, text);
		}

		private void TogglePin()
		{
			if (_services.Pins.Toggle(_definition.Key))
			{
				_services.Debugger.Windows.Open(PinnedWindow.Id);
			}
		}

		private void ToggleFavorite() => _services.Favorites.Toggle(_definition.Key);

		private void RefreshMarks()
		{
			if (_starButton != null)
			{
				bool favorite = _services.Favorites.Contains(_definition.Key);
				UiBuild.SetGlyph(_starButton, favorite ? IconGlyph.Star : IconGlyph.StarOutline);
				_starButton.EnableInClassList(OmniDebuggerUiClasses.IconButtonActive, favorite);
			}

			if (_pinButton != null && _services.Pins != null)
			{
				bool pinned = _services.Pins.Contains(_definition.Key);
				UiBuild.SetGlyph(_pinButton, pinned ? IconGlyph.Pin : IconGlyph.PinOutline);
				_pinButton.EnableInClassList(OmniDebuggerUiClasses.IconButtonActive, pinned);
			}
		}

		private void OnCardSubmitted(NavigationSubmitEvent evt) => _clicked?.Invoke(_definition);

		private string DescribeMeta()
		{
			StringBuilder builder = new StringBuilder();

			if (_definition.Tags.Count > 0)
			{
				builder.Append("Tags: ");
				builder.Append(string.Join(", ", _definition.Tags));
				builder.Append('\n');
			}

			builder.Append("ID: ");
			builder.Append(_definition.Key);
			return builder.ToString();
		}
	}
}