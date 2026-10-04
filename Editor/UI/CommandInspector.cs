#if OMNI_DEBUGGER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI.Editor
{
	internal sealed class CommandInspector : IDisposable
	{
		private const string Origin = "Editor Window";
		private const string RunLabel = "Run";
		private const string PinLabel = "Pin to hotbar";
		private const string InvalidMessage = "Fill in the highlighted arguments.";
		private const string FailedMessage = "Failed — see the Console.";
		private const string ExecutedMessage = "Executed.";

		private readonly IOmniDebuggerHost _debugger;
		private readonly IHotbar _hotbar;
		private readonly CommandDefinition _definition;
		private readonly CommandStateStore _states;
		private readonly List<EditorArgumentField> _fields = new ();
		private readonly Toggle _pin;
		private readonly TextField _readonlyValue;
		private readonly HelpBox _status;

		public VisualElement Root { get; }

		private object _lastRead;
		private bool _disposed;

		public CommandInspector(IOmniDebuggerHost debugger, CommandDefinition definition, CommandStateStore states, bool compact)
		{
			_debugger = debugger;
			_hotbar = debugger.Hotbar;
			_definition = definition;
			_states = states;

			Root = new VisualElement();
			Root.style.marginBottom = 8.0f;

			Label title = new Label(definition.Name);
			title.style.unityFontStyleAndWeight = FontStyle.Bold;
			title.style.fontSize = compact ? 12 : 14;
			Root.Add(title);

			if (!compact)
			{
				AddCaption(definition.Path + "  ·  " + DescribeKind(definition.Kind));

				if (definition.Tags.Count > 0)
				{
					AddCaption("Tags: " + string.Join(", ", definition.Tags));
				}
			}

			if (definition.Description != null)
			{
				Label description = new Label(definition.Description);
				description.style.whiteSpace = WhiteSpace.Normal;
				description.style.marginBottom = 4.0f;
				Root.Add(description);
			}

			switch (definition.Kind)
			{
				case CommandKind.ReadonlyValue:
					_readonlyValue = new TextField(definition.Arguments[0].Name) { isReadOnly = true };
					Root.Add(_readonlyValue);
					break;

				case CommandKind.Value:
					EditorArgumentField value = AddField(definition.Arguments[0], definition.Arguments[0].Name);
					value.OnCommitted += Execute;
					break;

				default:
					BuildArguments();
					break;
			}

			_status = new HelpBox(string.Empty, HelpBoxMessageType.None);
			_status.style.display = DisplayStyle.None;
			Root.Add(_status);

			_pin = new Toggle(PinLabel);
			_pin.SetValueWithoutNotify(_hotbar.Contains(definition.Path));
			_pin.RegisterValueChangedCallback(OnPinChanged);
			Root.Add(_pin);
			_hotbar.OnChanged += ShowPin;

			RefreshValue(force: true);
		}

		public void RefreshValue(bool force = false)
		{
			if (_disposed || _definition.Kind == CommandKind.Action)
			{
				return;
			}

			if (!_debugger.Commands.TryGet(_definition.Path, out _) ||
				!_debugger.Commands.TryGetValue(_definition.Path, out object value))
			{
				return;
			}

			if (!force && Equals(value, _lastRead))
			{
				return;
			}

			if (_readonlyValue != null)
			{
				_lastRead = value;
				_readonlyValue.SetValueWithoutNotify(CommandRow.Format(value));
				return;
			}

			EditorArgumentField field = _fields[0];
			if (!force && field.IsEditing)
			{
				return;
			}

			_lastRead = value;
			field.SetValue(value);
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_hotbar.OnChanged -= ShowPin;

			for (int i = 0; i < _fields.Count; i++)
			{
				_fields[i].Dispose();
			}

			_fields.Clear();
			Root.RemoveFromHierarchy();
		}

		private static string DescribeKind(CommandKind kind) => kind switch
		{
			CommandKind.Value => "value",
			CommandKind.ReadonlyValue => "read-only value",
			_ => "action",
		};

		private void AddCaption(string text)
		{
			Label caption = new Label(text);
			caption.style.opacity = 0.7f;
			caption.style.whiteSpace = WhiteSpace.Normal;
			caption.selection.isSelectable = true;
			Root.Add(caption);
		}

		private void BuildArguments()
		{
			CommandState state = _states.Get(_definition.Path);
			IReadOnlyList<ArgumentDefinition> arguments = _definition.Arguments;

			for (int i = 0; i < arguments.Count; i++)
			{
				ArgumentDefinition argument = arguments[i];
				string label = argument.IsOptional ? argument.Name + " (optional)" : argument.Name;
				EditorArgumentField field = AddField(argument, ObjectNames.NicifyVariableName(label));

				if (state.TryGetArgument(argument, out object remembered))
				{
					field.SetValue(remembered);
				}
				else if (argument.DefaultValue != null)
				{
					field.SetValue(argument.DefaultValue);
				}

				field.OnCommitted += Remember;
			}

			Button run = new Button(Execute) { text = RunLabel };
			run.style.alignSelf = Align.FlexStart;
			run.style.minWidth = 80.0f;
			Root.Add(run);
		}

		private EditorArgumentField AddField(ArgumentDefinition argument, string label)
		{
			string path = _definition.Path;
			int index = _fields.Count;
			Action<ICollection<object>> options = argument.HasOptions
				? list => _debugger.Commands.TryGetOptions(path, index, list)
				: null;

			EditorArgumentField field = EditorArgumentField.Create(argument, _debugger.Fields, label, options);
			_fields.Add(field);
			Root.Add(field.Root);
			return field;
		}

		private void Remember()
		{
			CommandState state = _states.Get(_definition.Path);

			for (int i = 0; i < _fields.Count; i++)
			{
				state.SetArgument(_definition.Arguments[i].Name, _fields[i].TryGetValue(out object value) ? value : null);
			}
		}

		private void Execute()
		{
			if (_disposed)
			{
				return;
			}

			List<ArgumentSlot> slots = new List<ArgumentSlot>(_fields.Count);
			for (int i = 0; i < _fields.Count; i++)
			{
				slots.Add(_fields[i].TryGetValue(out object value) ? ArgumentSlot.From(value) : ArgumentSlot.Empty());
			}

			if (!ArgumentValues.TryBuild(_definition.Arguments, slots, out object[] values, out int invalid))
			{
				ShowStatus(InvalidMessage, HelpBoxMessageType.Warning);

				if (invalid >= 0 && invalid < _fields.Count)
				{
					_fields[invalid].Root.Focus();
				}

				return;
			}

			if (_definition.Kind == CommandKind.Action)
			{
				Remember();
			}

			bool ran = _debugger.Commands.TryExecute(_definition, new InvocationRequest(Origin, values));

			if (_definition.Kind == CommandKind.Value)
			{
				ShowStatus(ran ? null : FailedMessage, HelpBoxMessageType.Error);
				RefreshValue(force: true);
				return;
			}

			ShowStatus(ran ? ExecutedMessage : FailedMessage, ran ? HelpBoxMessageType.Info : HelpBoxMessageType.Error);
		}

		private void ShowStatus(string message, HelpBoxMessageType type)
		{
			_status.text = message ?? string.Empty;
			_status.messageType = type;
			_status.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private void OnPinChanged(ChangeEvent<bool> evt)
		{
			if (evt.newValue)
			{
				_hotbar.Pin(_definition.Path);
			}
			else
			{
				_hotbar.Unpin(_definition.Path);
			}
		}

		private void ShowPin() => _pin.SetValueWithoutNotify(_hotbar.Contains(_definition.Path));
	}
}
#endif
