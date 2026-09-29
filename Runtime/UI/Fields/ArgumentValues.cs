using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal static class ArgumentValues
	{
		public static bool TryBuild(IReadOnlyList<ArgumentDefinition> arguments, IReadOnlyList<ArgumentSlot> slots, out object[] values, out int invalidIndex)
		{
			if (arguments == null)
			{
				throw new ArgumentNullException(nameof(arguments));
			}

			if (slots == null)
			{
				throw new ArgumentNullException(nameof(slots));
			}

			if (arguments.Count != slots.Count)
			{
				throw new ArgumentException(
					$"One slot per argument is required. Arguments: {arguments.Count}; Slots: {slots.Count}.",
					nameof(slots));
			}

			invalidIndex = -1;

			if (arguments.Count == 0)
			{
				values = Array.Empty<object>();
				return true;
			}

			for (int i = 0; i < arguments.Count; i++)
			{
				if (slots[i].HasValue || arguments[i].IsOptional)
				{
					continue;
				}

				invalidIndex = i;
				values = null;
				return false;
			}

			int count = arguments.Count;

			while (count > 0 && !slots[count - 1].HasValue && arguments[count - 1].IsOptional)
			{
				count--;
			}

			if (count == 0)
			{
				values = Array.Empty<object>();
				return true;
			}

			values = new object[count];

			for (int i = 0; i < count; i++)
			{
				values[i] = slots[i].HasValue ? slots[i].Value : arguments[i].DefaultValue;
			}

			return true;
		}
	}
}