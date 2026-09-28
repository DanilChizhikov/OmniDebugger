using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class NullableArgumentField : IArgumentField
	{
		public event Action OnCommitted;

		public VisualElement Root => _root;
		
		private readonly VisualElement _root;
		private readonly Toggle _set;
		private readonly IArgumentField _inner;

		private bool _disposed;

		public NullableArgumentField(IArgumentField inner, in ArgumentFieldRequest request)
		{
			_inner = inner ?? throw new ArgumentNullException(nameof(inner));

			_root = new VisualElement();
			_root.AddToClassList(OmniDebuggerUiClasses.Field);
			_root.style.flexDirection = FlexDirection.Row;

			_set = new Toggle();
			_set.AddToClassList(OmniDebuggerUiClasses.FieldNullableToggle);
			_set.tooltip = $"Send a value for '{request.Argument.Name}'.";
			_set.SetValueWithoutNotify(request.InitialValue != null);
			_set.RegisterValueChangedCallback(OnSetChanged);
			_root.Add(_set);

			_root.Add(_inner.Root);
			_inner.OnCommitted += OnInnerCommitted;
			_inner.Root.SetEnabled(_set.value);
		}

		public bool TryGetValue(out object value)
		{
			if (!_set.value)
			{
				value = null;
				return true;
			}

			return _inner.TryGetValue(out value);
		}

		public void SetValue(object value)
		{
			_set.SetValueWithoutNotify(value != null);
			_inner.Root.SetEnabled(value != null);
			_inner.SetValue(value);
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_set.UnregisterValueChangedCallback(OnSetChanged);
			_inner.OnCommitted -= OnInnerCommitted;
			_inner.Dispose();
			OnCommitted = null;
			_root.RemoveFromHierarchy();
		}

		private void OnSetChanged(ChangeEvent<bool> evt)
		{
			_inner.Root.SetEnabled(evt.newValue);
			OnCommitted?.Invoke();
		}

		private void OnInnerCommitted() => OnCommitted?.Invoke();
	}
}