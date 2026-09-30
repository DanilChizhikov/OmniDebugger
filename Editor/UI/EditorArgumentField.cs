#if OMNI_DEBUGGER
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI.Editor
{
	internal abstract class EditorArgumentField : IDisposable
	{
		public event Action OnCommitted;

		public abstract VisualElement Root { get; }

		public virtual bool IsEditing => Root.focusController?.focusedElement is VisualElement focused && Root.Contains(focused);

		public static EditorArgumentField Create(ArgumentDefinition argument, IArgumentFieldRegistry fallback, string label)
		{
			Type type = Nullable.GetUnderlyingType(argument.Type) ?? argument.Type;
			bool nullable = type != argument.Type;
			EditorArgumentField field = CreateFor(type, argument, label) ?? CreateFallback(argument, fallback, label);

			return nullable ? new NullableField(field, label) : field;
		}

		public abstract bool TryGetValue(out object value);

		public abstract void SetValue(object value);

		public virtual void Dispose()
		{
			OnCommitted = null;
		}

		protected void Commit() => OnCommitted?.Invoke();

		private static EditorArgumentField CreateFor(Type type, ArgumentDefinition argument, string label)
		{
			ArgumentRange range = argument.Range;

			if (type == typeof(bool))
			{
				return new Native<bool>(new Toggle(label), value => value, value => value is bool on && on);
			}

			if (type.IsEnum)
			{
				Enum initial = (Enum)Enum.ToObject(type, 0);
				Array values = Enum.GetValues(type);
				if (values.Length > 0)
				{
					initial = (Enum)values.GetValue(0);
				}

				BaseField<Enum> popup = type.IsDefined(typeof(FlagsAttribute), false)
					? new EnumFlagsField(label, initial)
					: new EnumField(label, initial);

				return new Native<Enum>(popup, value => value, value => value as Enum ?? initial);
			}

			if (type == typeof(int) || type == typeof(short) || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte))
			{
				BaseField<int> number = range.IsEmpty
					? new IntegerField(label) { isDelayed = true }
					: new SliderInt(label, (int)Math.Ceiling(range.Min), (int)Math.Floor(range.Max)) { showInputField = true };

				return new Native<int>(number, value => Convert(value, type), value => (int)ToDouble(value));
			}

			if (type == typeof(long) || type == typeof(uint) || type == typeof(ulong))
			{
				return new Native<long>(new LongField(label) { isDelayed = true }, value => Convert(value, type), value => (long)ToDouble(value));
			}

			if (type == typeof(float))
			{
				BaseField<float> number = range.IsEmpty
					? new FloatField(label) { isDelayed = true }
					: new Slider(label, (float)range.Min, (float)range.Max) { showInputField = true };

				return new Native<float>(number, value => Snap(value, range), value => (float)ToDouble(value));
			}

			if (type == typeof(double))
			{
				return new Native<double>(new DoubleField(label) { isDelayed = true }, value => value, ToDouble);
			}

			if (type == typeof(string))
			{
				return new Native<string>(new TextField(label) { isDelayed = true }, value => value, value => value as string ?? string.Empty);
			}

			if (typeof(IConvertible).IsAssignableFrom(type))
			{
				return new Parsed(new TextField(label) { isDelayed = true }, type);
			}

			return null;
		}

		private static EditorArgumentField CreateFallback(ArgumentDefinition argument, IArgumentFieldRegistry registry, string label)
		{
			ArgumentFieldRequest request = ArgumentFieldRequest.For(argument, null, showLabel: true);
			return new Registered(registry.Create(request), label);
		}

		private static object Convert(int value, Type type) =>
			CommandArguments.TryConvert(value, type, out object converted) ? converted : value;

		private static object Convert(long value, Type type) =>
			CommandArguments.TryConvert(value, type, out object converted) ? converted : value;

		private static float Snap(float value, ArgumentRange range)
		{
			if (range.IsEmpty || !(range.Step > 0.0))
			{
				return value;
			}

			double steps = Math.Round((value - range.Min) / range.Step);
			return (float)Math.Min(range.Max, range.Min + steps * range.Step);
		}

		private static double ToDouble(object value)
		{
			if (value == null)
			{
				return 0.0;
			}

			try
			{
				return System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				return 0.0;
			}
		}

		private sealed class Native<T> : EditorArgumentField
		{
			private readonly BaseField<T> _field;
			private readonly Func<T, object> _toObject;
			private readonly Func<object, T> _fromObject;

			public override VisualElement Root => _field;

			public Native(BaseField<T> field, Func<T, object> toObject, Func<object, T> fromObject)
			{
				_field = field;
				_toObject = toObject;
				_fromObject = fromObject;
				_field.RegisterValueChangedCallback(OnChanged);
			}

			public override bool TryGetValue(out object value)
			{
				value = _toObject(_field.value);
				return true;
			}

			public override void SetValue(object value) => _field.SetValueWithoutNotify(_fromObject(value));

			public override void Dispose()
			{
				_field.UnregisterValueChangedCallback(OnChanged);
				base.Dispose();
			}

			private void OnChanged(ChangeEvent<T> evt) => Commit();
		}

		private sealed class Parsed : EditorArgumentField
		{
			private readonly TextField _field;
			private readonly Type _type;

			public override VisualElement Root => _field;

			public Parsed(TextField field, Type type)
			{
				_field = field;
				_type = type;
				_field.RegisterValueChangedCallback(OnChanged);
			}

			public override bool TryGetValue(out object value) =>
				CommandArguments.TryConvert(_field.value, _type, out value) && value != null;

			public override void SetValue(object value) =>
				_field.SetValueWithoutNotify(value is IFormattable formattable
					? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
					: value?.ToString() ?? string.Empty);

			public override void Dispose()
			{
				_field.UnregisterValueChangedCallback(OnChanged);
				base.Dispose();
			}

			private void OnChanged(ChangeEvent<string> evt) => Commit();
		}

		private sealed class Registered : EditorArgumentField
		{
			private readonly IArgumentField _field;
			private readonly VisualElement _root;

			public override VisualElement Root => _root;

			public Registered(IArgumentField field, string label)
			{
				_field = field;
				_root = new VisualElement();
				_root.AddToClassList(BaseField<int>.alignedFieldUssClassName);
				_root.style.flexDirection = FlexDirection.Row;

				Label caption = new Label(label);
				caption.AddToClassList(BaseField<int>.labelUssClassName);
				_root.Add(caption);

				field.Root.style.flexGrow = 1.0f;
				_root.Add(field.Root);
				_field.OnCommitted += Commit;
			}

			public override bool TryGetValue(out object value) => _field.TryGetValue(out value);

			public override void SetValue(object value) => _field.SetValue(value);

			public override void Dispose()
			{
				_field.OnCommitted -= Commit;
				_field.Dispose();
				base.Dispose();
			}
		}

		private sealed class NullableField : EditorArgumentField
		{
			private readonly EditorArgumentField _inner;
			private readonly Toggle _hasValue;
			private readonly VisualElement _root;

			public override VisualElement Root => _root;

			public NullableField(EditorArgumentField inner, string label)
			{
				_inner = inner;
				_root = new VisualElement();
				_root.style.flexDirection = FlexDirection.Row;

				_hasValue = new Toggle { tooltip = $"Give {label} a value; off passes null." };
				_hasValue.RegisterValueChangedCallback(OnToggled);
				_root.Add(_hasValue);

				inner.Root.style.flexGrow = 1.0f;
				_root.Add(inner.Root);
				inner.OnCommitted += Commit;
				Apply();
			}

			public override bool TryGetValue(out object value)
			{
				if (!_hasValue.value)
				{
					value = null;
					return true;
				}

				return _inner.TryGetValue(out value);
			}

			public override void SetValue(object value)
			{
				_hasValue.SetValueWithoutNotify(value != null);

				if (value != null)
				{
					_inner.SetValue(value);
				}

				Apply();
			}

			public override void Dispose()
			{
				_inner.OnCommitted -= Commit;
				_inner.Dispose();
				base.Dispose();
			}

			private void OnToggled(ChangeEvent<bool> evt)
			{
				Apply();
				Commit();
			}

			private void Apply() => _inner.Root.SetEnabled(_hasValue.value);
		}
	}
}
#endif
