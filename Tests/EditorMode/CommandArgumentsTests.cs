using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class CommandArgumentsTests
	{
		[Test]
		public void TryBind_ReadsDecimalTextTheSameWayUnderAnyLocale()
		{
			CultureInfo original = Thread.CurrentThread.CurrentCulture;

			try
			{
				Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
				CommandDefinition definition = Definition(new ArgumentDefinition("speed", typeof(float)));
				BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { "1.5" });

				Assert.That(response.Success, Is.True, response.Error);
				Assert.That(response.Bound[0], Is.EqualTo(1.5f));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = original;
			}
		}

		[Test]
		public void TryBind_ReadsAnEnumByNameIgnoringCase()
		{
			CommandDefinition definition = Definition(new ArgumentDefinition("severity", typeof(SampleSeverity)));
			BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { "tWo" });

			Assert.That(response.Success, Is.True, response.Error);
			Assert.That(response.Bound[0], Is.EqualTo(SampleSeverity.Two));
		}

		[Test]
		public void TryBind_FailsCleanlyOnAnUnknownEnumName()
		{
			CommandDefinition definition = Definition(new ArgumentDefinition("severity", typeof(SampleSeverity)));
			BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { "Three" });

			Assert.That(response.Success, Is.False);
			Assert.That(response.Error, Does.Contain("severity"));
		}

		[Test]
		public void TryBind_ReturnsTheInputArrayWhenNothingNeedsConverting()
		{
			object[] values = { 5 };
			CommandDefinition definition = Definition(new ArgumentDefinition("amount", typeof(int)));
			BindCommandResponse response = CommandArguments.TryBind(definition, values);

			Assert.That(response.Success, Is.True, response.Error);
			Assert.That(response.Bound, Is.SameAs(values), "the common case must not allocate");
		}

		[Test]
		public void TryBind_ReadsBlankTextAsNullForANullableArgument()
		{
			CommandDefinition definition = Definition(new ArgumentDefinition("amount", typeof(int?)));
			BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { "  " });

			Assert.That(response.Success, Is.True, response.Error);
			Assert.That(response.Bound[0], Is.Null);
		}

		[Test]
		public void TryBind_RejectsBlankTextForANonNullableArgument()
		{
			CommandDefinition definition = Definition(new ArgumentDefinition("amount", typeof(int)));
			BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { string.Empty });

			Assert.That(response.Success, Is.False);
			Assert.That(response.Error, Does.Contain("amount"));
		}

		[Test]
		public void TryBind_ReadsASingleCharacterAsChar()
		{
			CommandDefinition definition = Definition(new ArgumentDefinition("key", typeof(char)));
			BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { "x" });

			Assert.That(response.Success, Is.True, response.Error);
			Assert.That(response.Bound[0], Is.EqualTo('x'));
		}

		[Test]
		public void TryBind_RejectsMultipleCharactersForChar()
		{
			CommandDefinition definition = Definition(new ArgumentDefinition("key", typeof(char)));
			BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { "xy" });

			Assert.That(response.Success, Is.False);
			Assert.That(response.Error, Does.Contain("key"));
		}

		[Test]
		public void TryBind_MixesSuppliedValuesWithOmittedDefaults()
		{
			var firstArgument = new ArgumentDefinition("first", typeof(int));
			var secondArgument = new ArgumentDefinition("second", typeof(string), "fallback", isOptional: true);
			CommandDefinition definition = Definition(firstArgument, secondArgument);
			BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { "7" });

			Assert.That(response.Success, Is.True, response.Error);
			Assert.That(response.Bound, Has.Length.EqualTo(2));
			Assert.That(response.Bound[0], Is.EqualTo(7));
			Assert.That(response.Bound[1], Is.EqualTo("fallback"));
		}

		[Test]
		public void TryBind_GivesAnEmptyArrayToAZeroArgumentCommand()
		{
			BindCommandResponse response = CommandArguments.TryBind(Definition(), null);

			Assert.That(response.Success, Is.True, response.Error);
			Assert.That(response.Bound, Is.Not.Null.And.Empty);
		}

		[Test]
		public void TryBind_RejectsMoreValuesThanArguments()
		{
			CommandDefinition definition = Definition(new ArgumentDefinition("amount", typeof(int)));
			BindCommandResponse response = CommandArguments.TryBind(definition, new object[] { 1, 2 });

			Assert.That(response.Success, Is.False);
			Assert.That(response.Error, Does.Contain("at most 1"));
		}

		[Test]
		public void TryConvert_FormatsAValueAsTextWithoutLocaleDrift()
		{
			CultureInfo original = Thread.CurrentThread.CurrentCulture;

			try
			{
				Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

				Assert.That(CommandArguments.TryConvert(1.5f, typeof(string), out object result), Is.True);
				Assert.That(result, Is.EqualTo("1.5"));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = original;
			}
		}
		
		private static CommandDefinition Definition(params ArgumentDefinition[] arguments) =>
			new CommandDefinition(
				name: "Bind",
				groupName: "Tests",
				kind: CommandKind.Action,
				arguments: arguments);
	}
}