using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class CommandPathTests
	{
		[Test]
		public void Normalize_TrimsSegmentsAndDropsEmptyOnes()
		{
			Assert.That(CommandPath.Normalize(" Economy // Coins /"), Is.EqualTo("Economy/Coins"));
			Assert.That(CommandPath.Normalize("Economy/Coins"), Is.EqualTo("Economy/Coins"));
			Assert.That(CommandPath.Normalize("  "), Is.Empty);
			Assert.That(CommandPath.Normalize(null), Is.Empty);
		}

		[Test]
		public void Combine_JoinsAGroupAndAName()
		{
			Assert.That(CommandPath.Combine("Economy / Coins", " Add "), Is.EqualTo("Economy/Coins/Add"));
			Assert.Throws<ArgumentException>(() => CommandPath.Combine(" ", "Add"));
			Assert.Throws<ArgumentException>(() => CommandPath.Combine("Economy", " "));
			Assert.Throws<ArgumentException>(() => CommandPath.Combine("Economy", "a/b"));
		}

		[Test]
		public void Parts_ReadBackFromAPath()
		{
			const string path = "Economy/Coins/Add";

			Assert.That(CommandPath.GetName(path), Is.EqualTo("Add"));
			Assert.That(CommandPath.GetParent(path), Is.EqualTo("Economy/Coins"));
			Assert.That(CommandPath.GetRoot(path), Is.EqualTo("Economy"));
			Assert.That(CommandPath.Split(path), Is.EqualTo(new[] { "Economy", "Coins", "Add" }));
			Assert.That(CommandPath.GetParent("Economy"), Is.Empty);
		}

		[Test]
		public void CollectAncestors_ListsEveryGroupOutermostFirst()
		{
			List<string> ancestors = new List<string>();
			CommandPath.CollectAncestors("A/B/C", ancestors);

			Assert.That(ancestors, Is.EqualTo(new[] { "A", "A/B", "A/B/C" }));
		}

		[Test]
		public void IsWithin_MatchesWholeSegmentsOnly()
		{
			Assert.That(CommandPath.IsWithin("Economy/Coins/Add", "Economy"), Is.True);
			Assert.That(CommandPath.IsWithin("Economy", "Economy"), Is.True);
			Assert.That(CommandPath.IsWithin("EconomyPlus/Add", "Economy"), Is.False);
			Assert.That(CommandPath.IsWithin("Anything", string.Empty), Is.True);
		}

		[Test]
		public void Definition_IndexesEveryGroupSegmentForSearch()
		{
			CommandDefinition definition = new CommandDefinition("Add", "Economy/Coins", CommandKind.Action, tags: new[] { "money" });
			List<string> terms = new List<string>();

			definition.CollectIndexTerms(terms);

			Assert.That(terms, Is.EquivalentTo(new[] { "Add", "Economy", "Coins", "money" }));
			Assert.That(definition.Path, Is.EqualTo("Economy/Coins/Add"));
			Assert.That(definition.GroupPath, Is.EqualTo("Economy/Coins"));
		}
	}
}
