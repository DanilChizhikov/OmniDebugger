using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PanelModel
	{
		public const string AllGroup = "*";

		private static readonly Comparison<CommandDefinition> _commandOrder = CompareCommands;

		private readonly Dictionary<string, List<CommandDefinition>> _byGroup = new (StringComparer.Ordinal);
		private readonly Dictionary<string, int> _groupOrders = new (StringComparer.Ordinal);
		private readonly List<CommandDefinition> _all = new ();
		private readonly List<string> _groups = new ();
		private readonly Comparison<string> _groupOrder;

		public IReadOnlyList<string> Groups => _groups;

		public IReadOnlyList<CommandDefinition> All => _all;

		public PanelModel() => _groupOrder = CompareGroups;

		public void Rebuild(IReadOnlyList<CommandDefinition> commands, IGroupOrder order)
		{
			if (commands == null)
			{
				throw new ArgumentNullException(nameof(commands));
			}

			if (order == null)
			{
				throw new ArgumentNullException(nameof(order));
			}

			_byGroup.Clear();
			_groupOrders.Clear();
			_groups.Clear();
			_all.Clear();

			for (int i = 0; i < commands.Count; i++)
			{
				CommandDefinition definition = commands[i];

				if (definition == null)
				{
					continue;
				}

				string group = definition.GroupName;

				if (!_byGroup.TryGetValue(group, out List<CommandDefinition> bucket))
				{
					bucket = new List<CommandDefinition>();
					_byGroup.Add(group, bucket);
					_groups.Add(group);

					_groupOrders.Add(group, order.GetOrder(group));
				}

				bucket.Add(definition);
			}

			_groups.Sort(_groupOrder);

			for (int i = 0; i < _groups.Count; i++)
			{
				List<CommandDefinition> bucket = _byGroup[_groups[i]];
				bucket.Sort(_commandOrder);
				_all.AddRange(bucket);
			}
		}

		public IReadOnlyList<CommandDefinition> GetCommands(string group)
		{
			if (string.IsNullOrEmpty(group) || string.Equals(group, AllGroup, StringComparison.Ordinal))
			{
				return _all;
			}

			return _byGroup.TryGetValue(group, out List<CommandDefinition> bucket)
				? bucket
				: Array.Empty<CommandDefinition>();
		}

		public bool HasGroup(string group) =>
			string.Equals(group, AllGroup, StringComparison.Ordinal) || _byGroup.ContainsKey(group);

		private static int CompareCommands(CommandDefinition left, CommandDefinition right)
		{
			int byOrder = left.SortOrder.CompareTo(right.SortOrder);
			return byOrder != 0 ? byOrder : string.CompareOrdinal(left.Name, right.Name);
		}

		private int CompareGroups(string left, string right)
		{
			int byOrder = _groupOrders[left].CompareTo(_groupOrders[right]);
			return byOrder != 0 ? byOrder : string.CompareOrdinal(left, right);
		}
	}
}