using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandState
	{
		private readonly Dictionary<string, object> _arguments = new (StringComparer.Ordinal);
		private readonly CommandStateStore _owner;

		public string Path { get; }

		public bool IsEmpty => _arguments.Count == 0;

		internal IEnumerable<KeyValuePair<string, object>> Arguments => _arguments;

		public CommandState(string path, CommandStateStore owner)
		{
			Path = path;
			_owner = owner;
		}

		public bool TryGetArgument(ArgumentDefinition argument, out object value)
		{
			if (!_arguments.TryGetValue(argument.Name, out value))
			{
				return false;
			}

			if (value is StoredText stored)
			{
				if (!CommandArguments.TryConvert(stored.Text, argument.Type, out value))
				{
					_arguments.Remove(argument.Name);
					value = null;
					return false;
				}

				_arguments[argument.Name] = value;
			}

			return true;
		}

		public void SetArgument(string name, object value)
		{
			bool changed = value == null
				? _arguments.Remove(name)
				: !_arguments.TryGetValue(name, out object current) || !Equals(current, value);

			if (value != null)
			{
				_arguments[name] = value;
			}

			if (changed)
			{
				_owner?.NotifyChanged();
			}
		}

		internal void Restore(string name, string text) => _arguments[name] = new StoredText(text);
	}
}
