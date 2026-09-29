using System;

namespace DTech.OmniDebugger
{
	internal sealed class MyersDistance
	{
		private const int MaxPatternLength = 64;
		private const int AsciiRange = 128;

		private readonly ulong[] _ascii = new ulong[AsciiRange];
		private readonly char[] _wideCharacters = new char[MaxPatternLength];
		private readonly ulong[] _wideMasks = new ulong[MaxPatternLength];

		private int _wideCount;
		private int _patternLength;
		private ulong _highBit;
		
		public static int ToleranceFor(int wordLength)
		{
			if (wordLength < 4)
			{
				return 0;
			}

			return wordLength < 8 ? 1 : 2;
		}
		
		public bool TrySetPattern(ReadOnlySpan<char> pattern)
		{
			if (pattern.Length == 0 || pattern.Length > MaxPatternLength)
			{
				_patternLength = 0;
				return false;
			}

			Array.Clear(_ascii, 0, AsciiRange);
			_wideCount = 0;
			_patternLength = pattern.Length;
			_highBit = 1UL << (pattern.Length - 1);

			for (int i = 0; i < pattern.Length; i++)
			{
				char character = char.ToLowerInvariant(pattern[i]);
				ulong bit = 1UL << i;

				if (character < AsciiRange)
				{
					_ascii[character] |= bit;
					continue;
				}

				int slot = IndexOfWide(character);
				if (slot < 0)
				{
					slot = _wideCount++;
					_wideCharacters[slot] = character;
					_wideMasks[slot] = 0;
				}

				_wideMasks[slot] |= bit;
			}

			return true;
		}
		
		public int Distance(ReadOnlySpan<char> text)
		{
			int length = _patternLength;

			ulong positive = length == 64 ? ~0UL : (1UL << length) - 1;
			ulong negative = 0;
			int score = length;

			for (int i = 0; i < text.Length; i++)
			{
				ulong equal = Equality(text[i]);

				ulong verticalNegative = equal | negative;
				ulong horizontal = (((equal & positive) + positive) ^ positive) | equal;
				ulong horizontalPositive = negative | ~(horizontal | positive);
				ulong horizontalNegative = positive & horizontal;

				if ((horizontalPositive & _highBit) != 0)
				{
					score++;
				}
				else if ((horizontalNegative & _highBit) != 0)
				{
					score--;
				}

				horizontalPositive = (horizontalPositive << 1) | 1;
				horizontalNegative <<= 1;

				positive = horizontalNegative | ~(verticalNegative | horizontalPositive);
				negative = horizontalPositive & verticalNegative;
			}

			return score;
		}

		private ulong Equality(char character)
		{
			if (character < AsciiRange)
			{
				return _ascii[character];
			}

			int slot = IndexOfWide(character);
			return slot < 0 ? 0UL : _wideMasks[slot];
		}

		private int IndexOfWide(char character)
		{
			for (int i = 0; i < _wideCount; i++)
			{
				if (_wideCharacters[i] == character)
				{
					return i;
				}
			}

			return -1;
		}
	}
}