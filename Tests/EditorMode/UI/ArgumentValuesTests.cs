using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ArgumentValuesTests
	{
		[Test]
		public void TryBuild_FillsInAnOmittedOptionalArgumentInsteadOfLeavingAHole()
		{
			IReadOnlyList<ArgumentDefinition> arguments = new[]
			{
				new ArgumentDefinition("first", typeof(int), 1, isOptional: true),
				new ArgumentDefinition("second", typeof(int), 2, isOptional: true),
			};

			bool built = ArgumentValues.TryBuild(
				arguments,
				new[] { ArgumentSlot.Empty(), ArgumentSlot.From(5) },
				out object[] values,
				out int invalidIndex);

			Assert.That(built, Is.True, "both arguments are optional, so nothing is missing");
			Assert.That(invalidIndex, Is.EqualTo(-1));
			Assert.That(values, Is.EqualTo(new object[] { 1, 5 }),
				"the invoker only defaults trailing arguments, so the hole has to be filled here");
		}

		[Test]
		public void TryBuild_LeavesTrailingEmptyOptionalArgumentsOffTheArray()
		{
			IReadOnlyList<ArgumentDefinition> arguments = new[]
			{
				new ArgumentDefinition("first", typeof(int)),
				new ArgumentDefinition("second", typeof(int), 2, isOptional: true),
			};

			bool built = ArgumentValues.TryBuild(
				arguments,
				new[] { ArgumentSlot.From(7), ArgumentSlot.Empty() },
				out object[] values,
				out int invalidIndex);

			Assert.That(built, Is.True);
			Assert.That(invalidIndex, Is.EqualTo(-1));
			Assert.That(values, Is.EqualTo(new object[] { 7 }), "the tail is left to the invoker's own defaults");
		}

		[Test]
		public void TryBuild_RefusesWhenARequiredArgumentIsEmptyAndSaysWhichOne()
		{
			IReadOnlyList<ArgumentDefinition> arguments = new[]
			{
				new ArgumentDefinition("first", typeof(int), 1, isOptional: true),
				new ArgumentDefinition("second", typeof(int)),
			};

			bool built = ArgumentValues.TryBuild(
				arguments,
				new[] { ArgumentSlot.From(1), ArgumentSlot.Empty() },
				out object[] values,
				out int invalidIndex);

			Assert.That(built, Is.False);
			Assert.That(values, Is.Null);
			Assert.That(invalidIndex, Is.EqualTo(1), "the row marks exactly that field");
		}

		[Test]
		public void TryBuild_KeepsAnExplicitNullFromANullableArgument()
		{
			IReadOnlyList<ArgumentDefinition> arguments = new[]
			{
				new ArgumentDefinition("amount", typeof(int?)),
				new ArgumentDefinition("tag", typeof(string)),
			};

			bool built = ArgumentValues.TryBuild(
				arguments,
				new[] { ArgumentSlot.From(null), ArgumentSlot.From("x") },
				out object[] values,
				out int invalidIndex);

			Assert.That(built, Is.True);
			Assert.That(invalidIndex, Is.EqualTo(-1));
			Assert.That(values[0], Is.Null, "an unset nullable field supplies null rather than nothing");
			Assert.That(values[1], Is.EqualTo("x"));
		}

		[Test]
		public void TryBuild_ReturnsAnEmptyArrayForACommandWithoutArguments()
		{
			bool built = ArgumentValues.TryBuild(
				new ArgumentDefinition[0],
				new ArgumentSlot[0],
				out object[] values,
				out int invalidIndex);

			Assert.That(built, Is.True);
			Assert.That(invalidIndex, Is.EqualTo(-1));
			Assert.That(values, Is.Empty);
		}

		[Test]
		public void TryBuild_ReturnsAnEmptyArrayWhenEveryOptionalArgumentWasLeftEmpty()
		{
			IReadOnlyList<ArgumentDefinition> arguments = new[]
			{
				new ArgumentDefinition("first", typeof(int), 1, isOptional: true),
				new ArgumentDefinition("second", typeof(int), 2, isOptional: true),
			};

			bool built = ArgumentValues.TryBuild(
				arguments,
				new[] { ArgumentSlot.Empty(), ArgumentSlot.Empty() },
				out object[] values,
				out int invalidIndex);

			Assert.That(built, Is.True);
			Assert.That(invalidIndex, Is.EqualTo(-1));
			Assert.That(values, Is.Empty, "a no-entry call must look exactly like calling it from code");
		}

		[Test]
		public void TryBuild_RejectsMismatchedSlotCounts()
		{
			IReadOnlyList<ArgumentDefinition> arguments = new[]
			{
				new ArgumentDefinition("first", typeof(int)),
			};

			Assert.That(
				() => ArgumentValues.TryBuild(arguments, new ArgumentSlot[0], out _, out _),
				Throws.ArgumentException,
				"a slot per argument is the contract the row has to honour");
		}
	}
}
