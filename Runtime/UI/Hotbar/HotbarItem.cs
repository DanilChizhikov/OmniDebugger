using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class HotbarItem : Button, IValueRefresher, IDisposable
	{
		private const long LongPressMs = 500;

		private readonly ViewServices _services;
		private readonly ValuePulse _pulse;
		private readonly CommandDefinition _definition;
		private readonly Label _value;
		private readonly IVisualElementScheduledItem _longPress;

		public CommandDefinition Definition => _definition;

		private CommandRow _detail;
		private bool _longPressed;
		private bool _disposed;

		public HotbarItem(ViewServices services, ValuePulse pulse, CommandDefinition definition)
		{
			_services = services;
			_pulse = pulse;
			_definition = definition;

			AddToClassList(OmniDebuggerUiClasses.HotbarItem);
			clicked += OnClicked;

			VisualElement icon = IconView.Create(_definition.IconKey ?? DefaultIcon(definition), services.Debugger.Icons, IconGlyph.Play, OmniDebuggerUiClasses.HotbarItemImage);
			icon.AddToClassList(OmniDebuggerUiClasses.HotbarItemIcon);
			Add(icon);

			Add(UiBuild.Label(definition.Name, OmniDebuggerUiClasses.HotbarItemName));

			if (definition.Kind != CommandKind.Action)
			{
				_value = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.HotbarItemValue);
				Add(_value);
				_pulse.Register(this);
			}

			_longPress = schedule.Execute(OnLongPress);
			_longPress.Pause();
			RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
		}

		public void RefreshValue()
		{
			if (_disposed || _value == null)
			{
				return;
			}

			ICommandRegistry commands = _services.Debugger.Commands;
			if (!commands.TryGet(_definition.Path, out _) || !commands.TryGetValue(_definition.Path, out object value))
			{
				_value.text = "—";
				return;
			}

			if (value is bool on)
			{
				_value.text = on ? "On" : "Off";
				EnableInClassList(OmniDebuggerUiClasses.HotbarItemOn, on);
				return;
			}

			_value.text = CommandRow.Format(value);
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_longPress.Pause();
			_pulse.Unregister(this);
			clicked -= OnClicked;
			UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
			DisposeDetail();
			RemoveFromHierarchy();
		}

		private static string DefaultIcon(CommandDefinition definition)
		{
			switch (definition.Kind)
			{
				case CommandKind.Action:
					return "play";
				case CommandKind.ReadonlyValue:
					return "eye";
				default:
					return definition.Arguments[0].Type == typeof(bool) ? "check" : "sliders";
			}
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			if (evt.button == 0)
			{
				_longPressed = false;
				_longPress.ExecuteLater(LongPressMs);
			}
		}

		private void OnPointerUp(PointerUpEvent evt) => _longPress.Pause();

		private void OnPointerLeave(PointerLeaveEvent evt) => _longPress.Pause();

		private void OnLongPress()
		{
			_longPressed = true;
			ShowDetail();
		}

		private void OnClicked()
		{
			if (_longPressed)
			{
				_longPressed = false;
				return;
			}

			if (!_services.Debugger.Commands.TryGet(_definition.Path, out _))
			{
				return;
			}

			if (TryToggle() || TryRun())
			{
				RefreshValue();
				return;
			}

			if (_definition.Kind != CommandKind.ReadonlyValue)
			{
				ShowDetail();
			}
		}

		private bool TryToggle()
		{
			ICommandRegistry commands = _services.Debugger.Commands;

			if (_definition.Kind != CommandKind.Value || _definition.Arguments[0].Type != typeof(bool) ||
				!commands.TryGetValue(_definition.Path, out object current) || !(current is bool on))
			{
				return false;
			}

			commands.TryExecute(_definition, new InvocationRequest(_services.Origin, new object[] { !on }));
			return true;
		}

		private bool TryRun()
		{
			if (_definition.Kind != CommandKind.Action)
			{
				return false;
			}

			object[] values = new object[_definition.Arguments.Count];
			CommandState state = null;
			_services.Commands?.TryGet(_definition.Path, out state);

			for (int i = 0; i < values.Length; i++)
			{
				ArgumentDefinition argument = _definition.Arguments[i];

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

			bool ran = _services.Debugger.Commands.TryExecute(_definition, new InvocationRequest(_services.Origin, values));
			EnableInClassList(OmniDebuggerUiClasses.HotbarItemFailed, !ran);
			return true;
		}

		private void ShowDetail()
		{
			if (_services.Popups == null || !_services.Debugger.Commands.TryGet(_definition.Path, out CommandDefinition current))
			{
				return;
			}

			DisposeDetail();
			_detail = new CommandRow(_services, _pulse, current, CommandRowMode.Full, showGroup: true);
			_detail.AddToClassList(OmniDebuggerUiClasses.First);

			VisualElement card = UiBuild.Element(OmniDebuggerUiClasses.RowList);
			card.Add(_detail);
			_services.Popups.ShowCentered(card, OmniDebuggerUiClasses.PopupHotbar, DisposeDetail);
		}

		private void DisposeDetail()
		{
			_detail?.Dispose();
			_detail = null;
		}
	}
}
