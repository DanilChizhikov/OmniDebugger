using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class EnumArgumentField : ArgumentFieldBase<object>
	{
		private readonly Type _enumType;

		public EnumArgumentField(in ArgumentFieldRequest request)
			: base(new ChoicePopupField(CollectValues(request.ValueType)), request, commitOnChange: true)
		{
			_enumType = request.ValueType;
			Field.SetValueWithoutNotify(Parse(request.InitialValue));
		}

		public override bool TryGetValue(out object value)
		{
			value = Field.value;
			return value != null;
		}

		protected override object Parse(object value)
		{
			if (value is Enum typed && typed.GetType() == _enumType)
			{
				return typed;
			}

			if (value != null)
			{
				try
				{
					return Enum.ToObject(_enumType, value);
				}
				catch (Exception)
				{
				}
			}

			return FirstValue(_enumType);
		}

		private static Action<ICollection<object>> CollectValues(Type enumType)
		{
			Array values = Enum.GetValues(enumType);

			return options =>
			{
				for (int i = 0; i < values.Length; i++)
				{
					options.Add(values.GetValue(i));
				}
			};
		}

		private static object FirstValue(Type enumType)
		{
			Array values = Enum.GetValues(enumType);
			return values.Length > 0 ? values.GetValue(0) : Enum.ToObject(enumType, 0);
		}
	}
}
