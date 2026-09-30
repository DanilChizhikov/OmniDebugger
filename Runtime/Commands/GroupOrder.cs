using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal sealed class GroupOrder : IGroupOrder
	{
		private readonly Dictionary<string, int> _orders = new (StringComparer.Ordinal);

		public int DefaultOrder => CommandDefinition.DefaultSortOrder;

		public void SetOrder(string groupPath, int order)
		{
			MainThreadGuard.Verify(nameof(SetOrder));

			if (string.IsNullOrWhiteSpace(groupPath))
			{
				throw new ArgumentException("Group path cannot be null or whitespace.", nameof(groupPath));
			}

			_orders[CommandPath.Normalize(groupPath)] = order;
		}

		public int GetOrder(string groupPath)
		{
			MainThreadGuard.Verify(nameof(GetOrder));

			if (string.IsNullOrWhiteSpace(groupPath))
			{
				return DefaultOrder;
			}

			return _orders.TryGetValue(CommandPath.Normalize(groupPath), out int order) ? order : DefaultOrder;
		}

		public void Clear() => _orders.Clear();
	}
}