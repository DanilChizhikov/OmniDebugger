using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ArgumentFieldRow : IDisposable
	{
		public event Action OnCommitted;

		private readonly VisualElement _root;
		private readonly List<IArgumentField> _fields = new ();
		private readonly List<ArgumentSlot> _slots = new ();
		private readonly CommandStateStore _states;
		private readonly IArgumentFieldRegistry _registry;

		public VisualElement Root => _root;
		
		private bool ShowLabels { get; }

		private CommandDefinition _definition;
		private CommandState _state;
		private bool _disposed;

		public ArgumentFieldRow(CommandStateStore states, IArgumentFieldRegistry registry, bool showLabels)
		{
			_states = states;
			_registry = registry;
			ShowLabels = showLabels;

			_root = new VisualElement();
			_root.AddToClassList(OmniDebuggerUiClasses.Arguments);
		}

		public void Bind(CommandDefinition definition)
		{
			Release();

			_definition = definition;

			if (definition == null)
			{
				return;
			}

			_state = _states?.Get(definition.Path);

			IReadOnlyList<ArgumentDefinition> arguments = definition.Arguments;

			for (int i = 0; i < arguments.Count; i++)
			{
				ArgumentDefinition argument = arguments[i];
				object remembered = null;
				_state?.TryGetArgument(argument, out remembered);

				ArgumentFieldRequest request = ArgumentFieldRequest.For(argument, remembered, ShowLabels);
				IArgumentField field = _registry.Create(request);
				field.OnCommitted += OnFieldCommitted;

				_fields.Add(field);
				_root.Add(field.Root);
			}
		}

		public void SetSingleValue(object value)
		{
			if (_fields.Count != 1)
			{
				return;
			}

			_fields[0].SetValue(value);
		}

		public bool TryGetSingleValue(out object value)
		{
			if (_fields.Count != 1)
			{
				value = null;
				return false;
			}

			return _fields[0].TryGetValue(out value);
		}

		public bool TryGetValues(out object[] values)
		{
			values = null;

			if (_definition == null)
			{
				return false;
			}

			_slots.Clear();

			for (int i = 0; i < _fields.Count; i++)
			{
				_slots.Add(_fields[i].TryGetValue(out object value)
					? ArgumentSlot.From(value)
					: ArgumentSlot.Empty());
			}

			bool built = ArgumentValues.TryBuild(_definition.Arguments, _slots, out values, out int invalidIndex);

			for (int i = 0; i < _fields.Count; i++)
			{
				_fields[i].Root.EnableInClassList(OmniDebuggerUiClasses.FieldInvalid, i == invalidIndex);
			}

			return built;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			Release();
			OnCommitted = null;
			_root.RemoveFromHierarchy();
		}

		public void Remember()
		{
			if (_state == null || _definition == null)
			{
				return;
			}

			IReadOnlyList<ArgumentDefinition> arguments = _definition.Arguments;

			for (int i = 0; i < _fields.Count && i < arguments.Count; i++)
			{
				string argumentName = arguments[i].Name;
				object fieldValue = _fields[i].TryGetValue(out object value) ? value : null;
				_state.SetArgument(argumentName, fieldValue);
			}
		}

		private void Release()
		{
			for (int i = 0; i < _fields.Count; i++)
			{
				_fields[i].OnCommitted -= OnFieldCommitted;
				_fields[i].Dispose();
			}

			_fields.Clear();
			_slots.Clear();
			_definition = null;
			_state = null;
		}

		private void OnFieldCommitted()
		{
			Remember();
			OnCommitted?.Invoke();
		}
	}
}