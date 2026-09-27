using System;
using System.Collections.Generic;
using System.Globalization;

namespace DTech.OmniDebugger
{
	internal static class CommandArguments
	{
		public static BindCommandResponse TryBind(CommandDefinition definition, object[] values)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			object[] bound = null;
			string error = null;

			IReadOnlyList<ArgumentDefinition> arguments = definition.Arguments;
			int suppliedCount = values?.Length ?? 0;

			if (suppliedCount > arguments.Count)
			{
				error = $"expected at most {arguments.Count} argument(s), but got {suppliedCount}";
				return new BindCommandResponse(bound, error);
			}

			if (arguments.Count == 0)
			{
				bound = Array.Empty<object>();
				return new BindCommandResponse(bound, error);
			}

			object[] result = null;

			for (int i = 0; i < arguments.Count; i++)
			{
				ArgumentDefinition argument = arguments[i];

				if (i >= suppliedCount)
				{
					if (!argument.IsOptional)
					{
						error = $"argument '{argument.Name}' is required";
						return new BindCommandResponse(bound, error);
					}

					result ??= CopyInto(values, arguments.Count);
					result[i] = argument.DefaultValue;
					continue;
				}

				object value = values[i];

				if (IsAssignable(value, argument.Type))
				{
					if (result != null)
					{
						result[i] = value;
					}

					continue;
				}

				if (!TryConvert(value, argument.Type, out object converted))
				{
					error = $"argument '{argument.Name}' cannot be read as {argument.Type.Name}";
					return new BindCommandResponse(bound, error);
				}

				result ??= CopyInto(values, arguments.Count);
				result[i] = converted;
			}

			bound = result ?? values;
			return new BindCommandResponse(bound, error);
		}

		public static bool TryConvert(object value, Type targetType, out object converted)
		{
			if (targetType == null)
			{
				throw new ArgumentNullException(nameof(targetType));
			}

			converted = null;

			Type valueType = Nullable.GetUnderlyingType(targetType) ?? targetType;
			bool isNullable = valueType != targetType;

			if (value == null || (value is string empty && string.IsNullOrWhiteSpace(empty)))
			{
				return isNullable || !targetType.IsValueType;
			}

			if (valueType.IsInstanceOfType(value))
			{
				converted = value;
				return true;
			}

			try
			{
				if (valueType == typeof(string))
				{
					converted = value is IFormattable formattable
						? formattable.ToString(null, CultureInfo.InvariantCulture)
						: value.ToString();
					return true;
				}

				if (valueType.IsEnum)
				{
					converted = value is string enumText
						? Enum.Parse(valueType, enumText, ignoreCase: true)
						: Enum.ToObject(valueType, value);

					return true;
				}

				if (valueType == typeof(char) && value is string charText)
				{
					if (charText.Length != 1)
					{
						return false;
					}

					converted = charText[0];
					return true;
				}

				if (value is IConvertible)
				{
					converted = Convert.ChangeType(value, valueType, CultureInfo.InvariantCulture);
					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}

			return false;
		}

		private static bool IsAssignable(object value, Type targetType)
		{
			if (value == null)
			{
				return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
			}

			return targetType.IsInstanceOfType(value);
		}

		private static object[] CopyInto(object[] values, int length)
		{
			object[] result = new object[length];

			int count = Math.Min(values?.Length ?? 0, length);
			for (int i = 0; i < count; i++)
			{
				result[i] = values[i];
			}

			return result;
		}
	}
}
