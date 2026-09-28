using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class NumericArgumentField<TValue> : ArgumentFieldBase<TValue>
	{
		private readonly Type _targetType;
		private readonly bool _needsConversion;

		public NumericArgumentField(BaseField<TValue> field, in ArgumentFieldRequest request)
			: base(field, request, commitOnChange: false)
		{
			_targetType = request.ValueType;
			_needsConversion = _targetType != typeof(TValue);
			Field.SetValueWithoutNotify(Parse(request.InitialValue));
		}

		public override bool TryGetValue(out object value)
		{
			TValue raw = Field.value;

			if (!_needsConversion)
			{
				value = raw;
				return true;
			}

			try
			{
				value = Convert.ChangeType(raw, _targetType, CultureInfo.InvariantCulture);
				return true;
			}
			catch (OverflowException)
			{
				value = null;
				return false;
			}
			catch (InvalidCastException)
			{
				value = null;
				return false;
			}
			catch (FormatException)
			{
				value = null;
				return false;
			}
		}

		protected override TValue Parse(object value)
		{
			if (value is TValue typed)
			{
				return typed;
			}

			if (value == null)
			{
				return default;
			}

			try
			{
				return (TValue)Convert.ChangeType(value, typeof(TValue), CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				return default;
			}
		}
	}
}