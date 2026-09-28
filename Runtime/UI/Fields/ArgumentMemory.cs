using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ArgumentMemory
	{
		private readonly Dictionary<string, Dictionary<string, object>> _values = new (StringComparer.Ordinal);

		public bool TryGet(string commandKey, string argumentName, out object value)
		{
			if (!_values.TryGetValue(commandKey, out Dictionary<string, object> arguments))
			{
				value = null;
				return false;
			}

			return arguments.TryGetValue(argumentName, out value);
		}

		public void Set(string commandKey, string argumentName, object value)
		{
			if (!_values.TryGetValue(commandKey, out Dictionary<string, object> arguments))
			{
				arguments = new Dictionary<string, object>(StringComparer.Ordinal);
				_values.Add(commandKey, arguments);
			}

			if (value == null)
			{
				arguments.Remove(argumentName);
				return;
			}

			arguments[argumentName] = value;
		}

		public void Clear() => _values.Clear();
	}
}