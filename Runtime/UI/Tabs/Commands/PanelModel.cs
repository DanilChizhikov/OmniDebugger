using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PanelModel
	{
		private static readonly Comparison<CommandDefinition> _commandOrder = CompareCommands;
		private static readonly Comparison<Node> _nodeOrder = CompareNodes;
		private static readonly IReadOnlyList<CommandDefinition> _noCommands = Array.Empty<CommandDefinition>();
		private static readonly IReadOnlyList<string> _noGroups = Array.Empty<string>();

		private readonly Dictionary<string, Node> _nodes = new (StringComparer.Ordinal);
		private readonly List<CommandDefinition> _all = new ();
		private readonly List<string> _groups = new ();
		private readonly List<string> _ancestors = new ();

		public IReadOnlyList<CommandDefinition> All => _all;

		public IReadOnlyList<string> Groups => _groups;

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

			_nodes.Clear();
			_all.Clear();
			_groups.Clear();

			Node root = new Node(string.Empty, 0);
			_nodes.Add(string.Empty, root);

			for (int i = 0; i < commands.Count; i++)
			{
				CommandDefinition definition = commands[i];
				if (definition == null)
				{
					continue;
				}

				GetOrCreate(definition.GroupPath, order).Commands.Add(definition);
			}

			Flatten(root);
		}

		public IReadOnlyList<string> GetChildren(string groupPath) =>
			_nodes.TryGetValue(groupPath ?? string.Empty, out Node node) ? node.ChildPaths : _noGroups;

		public IReadOnlyList<CommandDefinition> GetCommands(string groupPath) =>
			_nodes.TryGetValue(groupPath ?? string.Empty, out Node node) ? node.Commands : _noCommands;

		public IReadOnlyList<CommandDefinition> GetSubtree(string groupPath)
		{
			if (string.IsNullOrEmpty(groupPath))
			{
				return _all;
			}

			return _nodes.TryGetValue(groupPath, out Node node) ? node.Subtree : _noCommands;
		}

		public bool HasGroup(string groupPath) =>
			!string.IsNullOrEmpty(groupPath) && _nodes.ContainsKey(groupPath);

		private static int CompareCommands(CommandDefinition left, CommandDefinition right)
		{
			int byOrder = left.SortOrder.CompareTo(right.SortOrder);
			return byOrder != 0 ? byOrder : string.CompareOrdinal(left.Name, right.Name);
		}

		private static int CompareNodes(Node left, Node right)
		{
			int byOrder = left.Order.CompareTo(right.Order);
			return byOrder != 0 ? byOrder : string.CompareOrdinal(left.Path, right.Path);
		}

		private Node GetOrCreate(string groupPath, IGroupOrder order)
		{
			if (_nodes.TryGetValue(groupPath, out Node existing))
			{
				return existing;
			}

			_ancestors.Clear();
			CommandPath.CollectAncestors(groupPath, _ancestors);

			Node parent = _nodes[string.Empty];

			for (int i = 0; i < _ancestors.Count; i++)
			{
				string path = _ancestors[i];

				if (!_nodes.TryGetValue(path, out Node node))
				{
					node = new Node(path, order.GetOrder(path));
					_nodes.Add(path, node);
					parent.Children.Add(node);
				}

				parent = node;
			}

			return parent;
		}

		private void Flatten(Node node)
		{
			node.Commands.Sort(_commandOrder);
			node.Children.Sort(_nodeOrder);

			int start = _all.Count;

			if (node.Commands.Count > 0 && node.Path.Length > 0)
			{
				_groups.Add(node.Path);
			}

			_all.AddRange(node.Commands);

			for (int i = 0; i < node.Children.Count; i++)
			{
				node.ChildPaths.Add(node.Children[i].Path);
				Flatten(node.Children[i]);
			}

			node.Subtree = _all.GetRange(start, _all.Count - start);
		}

		private sealed class Node
		{
			public readonly string Path;
			public readonly int Order;
			public readonly List<Node> Children = new ();
			public readonly List<string> ChildPaths = new ();
			public readonly List<CommandDefinition> Commands = new ();

			public IReadOnlyList<CommandDefinition> Subtree;

			public Node(string path, int order)
			{
				Path = path;
				Order = order;
			}
		}
	}
}
