using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal abstract class ArgumentFieldBase<TValue> : IArgumentField
	{
		public event Action OnCommitted;

		private readonly BaseField<TValue> _field;
		private readonly bool _commitOnChange;

		public VisualElement Root => _field;
		
		protected BaseField<TValue> Field => _field;

		private bool _disposed;

		protected ArgumentFieldBase(BaseField<TValue> field, in ArgumentFieldRequest request, bool commitOnChange)
		{
			_field = field ?? throw new ArgumentNullException(nameof(field));
			_commitOnChange = commitOnChange;

			_field.AddToClassList(OmniDebuggerUiClasses.Field);
			_field.label = request.ShowLabel ? request.Argument.Name : null;
			_field.tooltip = request.Argument.ToString();

			if (commitOnChange)
			{
				_field.RegisterValueChangedCallback(OnValueChanged);
			}
			else
			{
				_field.RegisterCallback<KeyDownEvent>(OnKeyDown);
				_field.RegisterCallback<FocusOutEvent>(OnFocusOut);
			}
		}

		public abstract bool TryGetValue(out object value);

		public void SetValue(object value)
		{
			if (_disposed)
			{
				return;
			}

			_field.SetValueWithoutNotify(Parse(value));
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (_commitOnChange)
			{
				_field.UnregisterValueChangedCallback(OnValueChanged);
			}
			else
			{
				_field.UnregisterCallback<KeyDownEvent>(OnKeyDown);
				_field.UnregisterCallback<FocusOutEvent>(OnFocusOut);
			}

			OnCommitted = null;
			_field.RemoveFromHierarchy();
		}

		protected abstract TValue Parse(object value);

		protected void Commit()
		{
			if (_disposed)
			{
				return;
			}

			OnCommitted?.Invoke();
		}

		private void OnValueChanged(ChangeEvent<TValue> evt) => Commit();

		private void OnKeyDown(KeyDownEvent evt)
		{
			if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
			{
				return;
			}

			Commit();
		}

		private void OnFocusOut(FocusOutEvent evt) => Commit();
	}
}