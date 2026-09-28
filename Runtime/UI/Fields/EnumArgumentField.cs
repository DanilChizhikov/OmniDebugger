using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class EnumArgumentField : ArgumentFieldBase<Enum>
	{
		private readonly Type _enumType;

		public EnumArgumentField(in ArgumentFieldRequest request)
			: base(new EnumPopupField(request.ValueType), request, commitOnChange: true)
		{
			_enumType = request.ValueType;
			Field.SetValueWithoutNotify(Parse(request.InitialValue));
		}

		public override bool TryGetValue(out object value)
		{
			value = Field.value;
			return value != null;
		}

		protected override Enum Parse(object value)
		{
			if (value is Enum typed && typed.GetType() == _enumType)
			{
				return typed;
			}

			if (value != null)
			{
				try
				{
					return (Enum)Enum.ToObject(_enumType, value);
				}
				catch (Exception)
				{
				}
			}

			return FirstValue(_enumType);
		}

		private static Enum FirstValue(Type enumType)
		{
			Array values = Enum.GetValues(enumType);
			return values.Length > 0 ? (Enum)values.GetValue(0) : (Enum)Enum.ToObject(enumType, 0);
		}
	}
}