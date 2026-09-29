using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal sealed class GroupOrder : IGroupOrder
	{
		private readonly Dictionary<string, int> _orders = new (StringComparer.Ordinal);

		public int DefaultOrder => CommandDefinition.DefaultSortOrder;

		public void SetOrder(string groupName, int order)
		{
			MainThreadGuard.Verify(nameof(SetOrder));

			if (string.IsNullOrWhiteSpace(groupName))
			{
				throw new ArgumentException("Group name cannot be null or whitespace.", nameof(groupName));
			}

			_orders[groupName] = order;
		}

		public int GetOrder(string groupName)
		{
			MainThreadGuard.Verify(nameof(GetOrder));

			if (string.IsNullOrWhiteSpace(groupName))
			{
				return DefaultOrder;
			}

			return _orders.TryGetValue(groupName, out int order) ? order : DefaultOrder;
		}

		public void Clear() => _orders.Clear();
	}
}