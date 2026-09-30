using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class PanelModelTests
	{
		[Test]
		public void Rebuild_OrdersGroupsByTheirConfiguredOrderAndThenByName()
		{
			PanelModel model = new PanelModel();
			FakeGroupOrder order = new FakeGroupOrder();
			order.Set("Economy", 10);
			order.Set("Audio", 10);
			order.Set("Cheats", 5);

			model.Rebuild(
				new[]
				{
					Command("Economy", "AddCoins"),
					Command("Audio", "Mute"),
					Command("Cheats", "GodMode"),
				},
				order);

			Assert.That(model.Groups, Is.EqualTo(new[] { "Cheats", "Audio", "Economy" }),
				"order first, then an ordinal name comparison as the tie-break");
		}

		[Test]
		public void Rebuild_OrdersCommandsInAGroupBySortOrderAndThenByName()
		{
			PanelModel model = new PanelModel();

			model.Rebuild(
				new[]
				{
					Command("Cheats", "Zebra", sortOrder: 10),
					Command("Cheats", "Alpha", sortOrder: 50),
					Command("Cheats", "Beta", sortOrder: 10),
				},
				new FakeGroupOrder());

			IReadOnlyList<CommandDefinition> commands = model.GetCommands("Cheats");

			Assert.That(Names(commands), Is.EqualTo(new[] { "Beta", "Zebra", "Alpha" }));
		}

		[Test]
		public void All_ListsEveryCommandInGroupOrder()
		{
			PanelModel model = new PanelModel();
			FakeGroupOrder order = new FakeGroupOrder();
			order.Set("Second", 20);
			order.Set("First", 10);

			model.Rebuild(
				new[]
				{
					Command("Second", "B"),
					Command("First", "A"),
				},
				order);

			Assert.That(Names(model.All), Is.EqualTo(new[] { "A", "B" }));
			Assert.That(model.GetSubtree(null), Is.SameAs(model.All), "no group means every command");
		}

		[Test]
		public void Rebuild_BuildsATreeFromThePaths()
		{
			PanelModel model = new PanelModel();
			FakeGroupOrder order = new FakeGroupOrder();
			order.Set("Economy/Shop", 1);

			model.Rebuild(
				new[]
				{
					Command("Economy/Coins", "Add"),
					Command("Economy", "Reset"),
					Command("Economy/Shop", "Open"),
					Command("World", "Pause"),
				},
				order);

			Assert.That(model.GetChildren(string.Empty), Is.EqualTo(new[] { "Economy", "World" }));
			Assert.That(model.GetChildren("Economy"), Is.EqualTo(new[] { "Economy/Shop", "Economy/Coins" }),
				"subgroups follow their own order too");
			Assert.That(Names(model.GetCommands("Economy")), Is.EqualTo(new[] { "Reset" }));
			Assert.That(Names(model.GetSubtree("Economy")), Is.EqualTo(new[] { "Reset", "Open", "Add" }),
				"a group's own commands come before its subgroups'");
			Assert.That(model.Groups, Is.EqualTo(new[] { "Economy", "Economy/Shop", "Economy/Coins", "World" }));
			Assert.That(model.HasGroup("Economy"), Is.True, "a group holding only subgroups still exists");
		}

		[Test]
		public void GetCommands_ReturnsNothingForAGroupThatIsGone()
		{
			PanelModel model = new PanelModel();
			model.Rebuild(new[] { Command("Cheats", "GodMode") }, new FakeGroupOrder());

			Assert.That(model.HasGroup("Cheats"), Is.True);
			Assert.That(model.HasGroup("Removed"), Is.False);
			Assert.That(model.GetCommands("Removed"), Is.Empty);
		}

		[Test]
		public void Rebuild_ReadsEachGroupOrderOnceRatherThanFromInsideTheComparison()
		{
			PanelModel model = new PanelModel();
			FakeGroupOrder order = new FakeGroupOrder();

			model.Rebuild(
				new[]
				{
					Command("A", "One"),
					Command("A", "Two"),
					Command("B", "Three"),
				},
				order);

			Assert.That(order.Reads, Is.EqualTo(2), "one read per group, not one per comparison");
		}

		private static CommandDefinition Command(string group, string name, int sortOrder = 100) =>
			new CommandDefinition(name, group, CommandKind.Action, sortOrder);

		private static string[] Names(IReadOnlyList<CommandDefinition> commands)
		{
			string[] names = new string[commands.Count];

			for (int i = 0; i < commands.Count; i++)
			{
				names[i] = commands[i].Name;
			}

			return names;
		}

		private sealed class FakeGroupOrder : IGroupOrder
		{
			private readonly Dictionary<string, int> _orders = new Dictionary<string, int>();

			public int Reads { get; private set; }

			public int DefaultOrder => CommandDefinition.DefaultSortOrder;

			public void Set(string groupName, int order) => _orders[groupName] = order;

			public void SetOrder(string groupName, int order) => Set(groupName, order);

			public int GetOrder(string groupName)
			{
				Reads++;
				return _orders.TryGetValue(groupName, out int order) ? order : DefaultOrder;
			}
		}
	}
}
