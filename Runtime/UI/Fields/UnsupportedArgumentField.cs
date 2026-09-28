using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class UnsupportedArgumentField : IArgumentField
	{
		public event Action OnCommitted;
		
		public VisualElement Root => _root;

		private readonly VisualElement _root;

		public UnsupportedArgumentField(in ArgumentFieldRequest request)
		{
			_root = new VisualElement();
			_root.AddToClassList(OmniDebuggerUiClasses.Field);

			TextField field = new TextField
			{
				label = request.ShowLabel ? request.Argument.Name : null,
				value = request.ValueType.Name,
			};

			field.SetEnabled(false);
			_root.Add(field);

			Label hint = new Label($"{request.ValueType.Name} has no field. Register an IArgumentFieldHandler.");
			hint.AddToClassList(OmniDebuggerUiClasses.FieldHint);
			_root.Add(hint);
		}

		public bool TryGetValue(out object value)
		{
			value = null;
			return false;
		}

		public void SetValue(object value)
		{
		}

		public void Dispose()
		{
			OnCommitted = null;
			_root.RemoveFromHierarchy();
		}
	}
}