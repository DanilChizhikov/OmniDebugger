using System;

namespace DTech.OmniDebugger
{
	internal ref struct TermScanner
	{
		private readonly ReadOnlySpan<char> _text;

		public ReadOnlySpan<char> Current => _current;
		
		private int _cursor;
		private ReadOnlySpan<char> _current;

		public TermScanner(ReadOnlySpan<char> text)
		{
			_text = text;
			_cursor = 0;
			_current = default;
		}

		public bool MoveNext()
		{
			while (_cursor < _text.Length && !char.IsLetterOrDigit(_text[_cursor]))
			{
				_cursor++;
			}

			if (_cursor >= _text.Length)
			{
				_current = default;
				return false;
			}

			int start = _cursor;
			_cursor++;

			while (_cursor < _text.Length && char.IsLetterOrDigit(_text[_cursor]) && !IsBreak(_text, _cursor))
			{
				_cursor++;
			}

			_current = _text.Slice(start, _cursor - start);
			return true;
		}
		
		public static int CountTerms(ReadOnlySpan<char> text)
		{
			TermScanner scanner = new TermScanner(text);
			int count = 0;

			while (scanner.MoveNext())
			{
				count++;
			}

			return count;
		}
		
		private static bool IsBreak(ReadOnlySpan<char> text, int index)
		{
			char current = text[index];
			char previous = text[index - 1];

			bool currentIsDigit = char.IsDigit(current);
			if (currentIsDigit != char.IsDigit(previous))
			{
				return true;
			}

			if (currentIsDigit || !char.IsUpper(current))
			{
				return false;
			}

			if (!char.IsUpper(previous))
			{
				return true;
			}
			
			return index + 1 < text.Length && char.IsLower(text[index + 1]);
		}
	}
}