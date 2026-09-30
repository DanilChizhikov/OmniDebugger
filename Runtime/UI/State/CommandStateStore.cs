using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandStateStore
	{
		public event Action OnChanged;

		private const char Separator = '\t';
		private const char LineBreak = '\n';

		private readonly Dictionary<string, CommandState> _states = new (StringComparer.Ordinal);

		public bool IsRestored { get; set; }

		public CommandState Get(string path)
		{
			if (!_states.TryGetValue(path, out CommandState state))
			{
				state = new CommandState(path, this);
				_states.Add(path, state);
			}

			return state;
		}

		public bool TryGet(string path, out CommandState state) => _states.TryGetValue(path, out state);

		public void Clear()
		{
			_states.Clear();
			IsRestored = false;
		}

		public string Serialize()
		{
			StringBuilder builder = new StringBuilder();

			foreach (KeyValuePair<string, CommandState> pair in _states)
			{
				foreach (KeyValuePair<string, object> argument in pair.Value.Arguments)
				{
					string text = Format(argument.Value);
					if (text == null)
					{
						continue;
					}

					builder.Append(Escape(pair.Key)).Append(Separator)
						.Append(Escape(argument.Key)).Append(Separator)
						.Append(Escape(text)).Append(LineBreak);
				}
			}

			return builder.ToString();
		}

		public void Restore(string serialized)
		{
			if (string.IsNullOrEmpty(serialized))
			{
				return;
			}

			string[] lines = serialized.Split(LineBreak);

			for (int i = 0; i < lines.Length; i++)
			{
				string[] parts = lines[i].Split(Separator);
				if (parts.Length != 3 || parts[0].Length == 0 || parts[1].Length == 0)
				{
					continue;
				}

				Get(Unescape(parts[0])).Restore(Unescape(parts[1]), Unescape(parts[2]));
			}
		}

		internal void NotifyChanged() => OnChanged?.Invoke();

		private static string Format(object value) => value switch
		{
			null => null,
			StoredText stored => stored.Text,
			string text => text,
			DateTime time => time.ToString("o", CultureInfo.InvariantCulture),
			Enum member => member.ToString(),
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			IConvertible convertible => convertible.ToString(CultureInfo.InvariantCulture),
			_ => null,
		};

		private static string Escape(string text) =>
			text.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");

		private static string Unescape(string text)
		{
			if (text.IndexOf('\\') < 0)
			{
				return text;
			}

			StringBuilder builder = new StringBuilder(text.Length);

			for (int i = 0; i < text.Length; i++)
			{
				char character = text[i];

				if (character != '\\' || i + 1 >= text.Length)
				{
					builder.Append(character);
					continue;
				}

				char next = text[++i];
				builder.Append(next switch
				{
					't' => '\t',
					'n' => '\n',
					'r' => '\r',
					_ => next,
				});
			}

			return builder.ToString();
		}
	}
}
