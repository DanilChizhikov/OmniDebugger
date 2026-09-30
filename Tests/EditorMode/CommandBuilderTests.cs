using System;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class CommandBuilderTests
	{
		private ICommandRegistry Commands => _debugger.Commands;

		private RecordingLogSink _log;
		private OmniDebuggerHost _debugger;

		[SetUp]
		public void SetUp()
		{
			_log = new RecordingLogSink();
			_debugger = new OmniDebuggerHost(_log);
		}

		[TearDown]
		public void TearDown() => _debugger.Dispose();

		[Test]
		public void Build_RegistersEveryKindOfCommandInOneBatch()
		{
			int coins = 0;
			bool infinite = false;
			float multiplier = 1.0f;
			int level = 1;
			SampleSeverity severity = SampleSeverity.One;
			int changed = 0;
			Commands.OnChanged += () => changed++;

			Commands.Build()
				.Group("Economy/Coins")
					.Button("Add 1000", () => coins += 1000)
					.Button<int>("Add", amount => coins += amount, a => a.Name("amount").Default(100).Range(0, 10_000))
					.Toggle("Infinite", () => infinite, value => infinite = value)
					.Slider("Multiplier", () => multiplier, value => multiplier = value, 0.0f, 10.0f, step: 0.5f)
					.Value("Balance", () => coins)
				.Group("World")
					.Slider("Level", () => level, value => level = value, 1, 10)
					.Dropdown("Severity", () => severity, value => severity = value)
				.Register();

			Assert.That(changed, Is.EqualTo(1));
			Assert.That(Commands.All.Count, Is.EqualTo(7));

			Assert.That(Commands.TryExecute("Economy/Coins/Add 1000"), Is.True);
			Assert.That(Commands.TryExecute("Economy/Coins/Add"), Is.True, "the default fills in");
			Assert.That(Commands.TryExecute("Economy/Coins/Add", "5"), Is.True, "text is coerced");
			Assert.That(coins, Is.EqualTo(1105));

			Assert.That(Commands.TryExecute("Economy/Coins/Infinite", true), Is.True);
			Assert.That(infinite, Is.True);

			Assert.That(Commands.TryExecute("Economy/Coins/Multiplier", 2.5), Is.True);
			Assert.That(multiplier, Is.EqualTo(2.5f));

			Assert.That(Commands.TryExecute("World/Severity", "Two"), Is.True);
			Assert.That(severity, Is.EqualTo(SampleSeverity.Two));

			Assert.That(Commands.TryGetValue("Economy/Coins/Balance", out object balance), Is.True);
			Assert.That(balance, Is.EqualTo(1105));
			Assert.That(Commands.TryExecute("Economy/Coins/Balance"), Is.False, "a read-only value cannot be run");
		}

		[Test]
		public void Build_DescribesArgumentsAndTheLastCommandsDetails()
		{
			Commands.Build()
				.Group("Economy")
					.Button<int, string>("Give", (_, _) => { }, a => a.Name("amount").Range(1, 50, 5), b => b.Name("reason").Optional())
					.Icon("coin").Tags("money").Description("Gives coins.").Order(3)
				.Register();

			Assert.That(Commands.TryGet("Economy/Give", out CommandDefinition give), Is.True);
			Assert.That(give.IconKey, Is.EqualTo("coin"));
			Assert.That(give.Tags, Is.EqualTo(new[] { "money" }));
			Assert.That(give.Description, Is.EqualTo("Gives coins."));
			Assert.That(give.SortOrder, Is.EqualTo(3));

			Assert.That(give.Arguments[0].Name, Is.EqualTo("amount"));
			Assert.That(give.Arguments[0].IsOptional, Is.False);
			Assert.That(give.Arguments[0].Range.Step, Is.EqualTo(5.0));
			Assert.That(give.Arguments[1].IsOptional, Is.True);
			Assert.That(give.Arguments[1].DefaultValue, Is.Null);
		}

		[Test]
		public void Register_ReturnsAHandleThatRemovesTheBatch()
		{
			IDisposable handle = Commands.Build().Group("A").Button("One", () => { }).Button("Two", () => { }).Register();

			handle.Dispose();

			Assert.That(Commands.All, Is.Empty);
		}

		[Test]
		public void Builder_RefusesMisuse()
		{
			CommandBuilder builder = Commands.Build();

			Assert.Throws<InvalidOperationException>(() => builder.Button("No group", () => { }));
			Assert.Throws<InvalidOperationException>(() => builder.Icon("coin"), "a modifier needs a command before it");
			Assert.Throws<ArgumentException>(() => builder.Group(" / "));
			Assert.Throws<ArgumentException>(() => builder.Group("G").Button("a/b", () => { }), "a name cannot nest");
			Assert.Throws<ArgumentException>(() => Commands.Build().Group("G").Button<int>("X", _ => { }, a => a.Default("not a number")));

			builder.Group("G").Button("Once", () => { }).Register();
			Assert.Throws<InvalidOperationException>(() => builder.Register(), "a builder registers once");
		}
	}
}
