using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class TextArgumentField : ArgumentFieldBase<string>
	{
		private readonly bool _isChar;
		private readonly bool _isString;

		public TextArgumentField(in ArgumentFieldRequest request)
			: base(CreateField(request), request, commitOnChange: false)
		{
			_isChar = request.ValueType == typeof(char);
			_isString = request.ValueType == typeof(string);
			Field.SetValueWithoutNotify(Parse(request.InitialValue));
		}

		public override bool TryGetValue(out object value)
		{
			string text = Field.value;

			if (string.IsNullOrEmpty(text))
			{
				value = _isString ? string.Empty : null;
				return _isString;
			}

			if (_isChar)
			{
				value = text[0];
				return true;
			}

			value = text;
			return true;
		}

		protected override string Parse(object value)
		{
			if (value == null)
			{
				return string.Empty;
			}

			if (value is string text)
			{
				return text;
			}

			return value is IFormattable formattable
				? formattable.ToString(null, CultureInfo.InvariantCulture)
				: value.ToString();
		}

		private static TextField CreateField(in ArgumentFieldRequest request)
		{
			TextField field = new TextField();

			if (request.ValueType == typeof(char))
			{
				field.maxLength = 1;
			}

			return field;
		}
	}
}