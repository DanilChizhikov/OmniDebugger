using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class CommandRegistryTests
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
		public void Register_AddsEveryGeneratedCommandAndRaisesChangedOnce()
		{
			int changed = 0;
			Commands.OnChanged += () => changed++;

			Commands.Register(new SampleCommands());

			Assert.That(Commands.All.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(changed, Is.EqualTo(1), "one change per registration, not one per command");
		}

		[Test]
		public void Register_ReadsTheAttributesIntoTheDefinition()
		{
			Commands.Register(new SampleCommands());

			Assert.That(Commands.TryGet(SampleCommands.AddCoinsPath, out CommandDefinition addCoins), Is.True);
			Assert.That(addCoins.Kind, Is.EqualTo(CommandKind.Action));
			Assert.That(addCoins.SortOrder, Is.EqualTo(10));
			Assert.That(addCoins.Description, Is.EqualTo("Adds coins to the wallet."));
			Assert.That(addCoins.Tags, Is.EqualTo(new[] { "economy", "wallet" }));
			Assert.That(addCoins.IconKey, Is.EqualTo("coin"));
			Assert.That(addCoins.Arguments[0].IsOptional, Is.True);
			Assert.That(addCoins.Arguments[0].DefaultValue, Is.EqualTo(100));

			Assert.That(Commands.TryGet(SampleCommands.GodModePath, out CommandDefinition godMode), Is.True);
			Assert.That(godMode.Kind, Is.EqualTo(CommandKind.Value));

			Assert.That(Commands.TryGet(SampleCommands.PrivateSetterPath, out CommandDefinition privateSetter), Is.True);
			Assert.That(privateSetter.Kind, Is.EqualTo(CommandKind.ReadonlyValue), "a private setter makes a read-only value");

			Assert.That(Commands.TryGet(SampleCommands.UnnamedPath, out _), Is.True, "the name falls back to the member name");
			Assert.That(Commands.TryGet(SampleCommands.SetSeverityPath, out _), Is.True, "internal members are commands too");
		}

		[Test]
		public void Register_NestsGroupsByPath()
		{
			SampleCommands sample = new SampleCommands();
			Commands.Register(sample);

			Assert.That(Commands.TryGet(SampleCommands.DeepPath, out CommandDefinition deep), Is.True);
			Assert.That(deep.GroupPath, Is.EqualTo(SampleCommands.NestedGroup));
			Assert.That(deep.Name, Is.EqualTo("Deep"));

			Assert.That(Commands.TryExecute(SampleCommands.DeepPath), Is.True);
			Assert.That(sample.DeepCalls, Is.EqualTo(1));
		}

		[Test]
		public void Register_IncludesTheCommandsOfBaseTypes()
		{
			Commands.Register(new DerivedSampleCommands());

			Assert.That(Commands.All.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount + 1));
			Assert.That(Commands.TryGet(DerivedSampleCommands.ExtraPath, out _), Is.True);
			Assert.That(Commands.TryGet(SampleCommands.AddCoinsPath, out _), Is.True);
		}

		[Test]
		public void Register_IgnoresTheSameInstanceTwice()
		{
			SampleCommands sample = new SampleCommands();
			Commands.Register(sample);
			_log.Clear();

			int changed = 0;
			Commands.OnChanged += () => changed++;

			Commands.Register(sample);

			Assert.That(changed, Is.Zero);
			Assert.That(Commands.All.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(_log.Warnings, Has.Some.Contains("already registered"));
		}

		[Test]
		public void Register_RejectsASecondInstanceOfTheSameTypeAsDuplicatePaths()
		{
			Commands.Register(new SampleCommands());
			_log.Clear();

			Commands.Register(new SampleCommands());

			Assert.That(Commands.All.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(_log.Errors, Has.Count.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(_log.Errors, Has.All.Contains("Paths have to be unique"));
		}

		[Test]
		public void Register_WarnsWithoutErrorWhenAnObjectHasNoCommands()
		{
			IDisposable handle = Commands.Register(new PlainObject());

			Assert.That(handle, Is.Not.Null);
			Assert.That(Commands.All, Is.Empty);
			Assert.That(_log.Warnings, Has.Some.Contains("declares no commands"));
			Assert.That(_log.Errors, Is.Empty, "an object without commands is legal, not an error");
		}

		[Test]
		public void Register_RejectsNull() =>
			Assert.Throws<ArgumentNullException>(() => Commands.Register(null));

		[Test]
		public void Dispose_OfTheHandleRemovesExactlyWhatItRegistered()
		{
			IDisposable handle = Commands.Register(new SampleCommands());
			Commands.Build().Group("Custom").Button("Handmade", () => { }).Register();

			int changed = 0;
			Commands.OnChanged += () => changed++;

			handle.Dispose();
			handle.Dispose();

			Assert.That(Commands.All.Select(command => command.Path), Is.EqualTo(new[] { "Custom/Handmade" }));
			Assert.That(changed, Is.EqualTo(1), "a second dispose does nothing");
		}

		[Test]
		public void Unregister_RemovesWhatRegisterAddedForTheObject()
		{
			SampleCommands sample = new SampleCommands();
			Commands.Register(sample);

			Assert.That(Commands.Unregister(sample), Is.True);
			Assert.That(Commands.All, Is.Empty);
			Assert.That(Commands.Unregister(sample), Is.False);
			Assert.That(Commands.Unregister(new SampleCommands()), Is.False);
		}

		[Test]
		public void Dispose_OfARejectedRegistrationLeavesTheWinnerAlone()
		{
			Commands.Register(new SampleCommands());
			IDisposable loser = Commands.Register(new SampleCommands());

			loser.Dispose();

			Assert.That(Commands.All.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount),
				"removing the rejected registration must not evict the winner's commands");
		}

		[Test]
		public void Add_RegistersHandmadeCommandsAndSkipsNulls()
		{
			DebugCommand command = new DebugCommand(
				new CommandDefinition("Handmade", "Custom", CommandKind.Action),
				invoke: _ => { });

			IDisposable handle = Commands.Add(new[] { command, null });

			Assert.That(Commands.TryGet("Custom/Handmade", out CommandDefinition found), Is.True);
			Assert.That(found, Is.SameAs(command.Definition));
			Assert.That(_log.Warnings, Has.Some.Contains("null command"));

			handle.Dispose();
			Assert.That(Commands.All, Is.Empty);
		}

		[Test]
		public void All_SnapshotTakenBeforeAChangeIsUnaffectedByIt()
		{
			Commands.Register(new SampleCommands());
			IReadOnlyList<CommandDefinition> before = Commands.All;

			Commands.Build().Group("Custom").Button("Handmade", () => { }).Register();

			Assert.That(before.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(Commands.All.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount + 1));
			Assert.That(Commands.All, Is.Not.SameAs(before));
		}

		[Test]
		public void TryGet_NormalizesThePathItIsGiven()
		{
			Commands.Register(new SampleCommands());

			Assert.That(Commands.TryGet(" Tests / Nested /Deep ", out CommandDefinition deep), Is.True);
			Assert.That(deep.Path, Is.EqualTo(SampleCommands.DeepPath));
			Assert.That(Commands.TryGet("Nope/Missing", out _), Is.False);
		}

		[Test]
		public void DebugCommand_RejectsDelegatesThatDoNotMatchTheKind()
		{
			CommandDefinition action = new CommandDefinition("A", "G", CommandKind.Action);
			CommandDefinition value = new CommandDefinition("V", "G", CommandKind.Value,
				arguments: new[] { new ArgumentDefinition("v", typeof(int)) });

			Assert.Throws<ArgumentException>(() => new DebugCommand(action));
			Assert.Throws<ArgumentException>(() => new DebugCommand(action, _ => { }, get: () => 1));
			Assert.Throws<ArgumentException>(() => new DebugCommand(value, get: () => 1));
			Assert.DoesNotThrow(() => new DebugCommand(value, get: () => 1, set: _ => { }));
		}

		[Test]
		public void Dispose_ReleasesEverythingAndBlocksFurtherUse()
		{
			Commands.Register(new SampleCommands());

			_debugger.Dispose();

			Assert.Throws<ObjectDisposedException>(() => _ = _debugger.Commands);
			Assert.Throws<ObjectDisposedException>(() => _ = _debugger.Groups);
			Assert.Throws<ObjectDisposedException>(() => _ = _debugger.Hotbar);
			Assert.Throws<ObjectDisposedException>(() => _ = _debugger.Info);
		}

		[Test]
		public void Dispose_IsIdempotent()
		{
			_debugger.Dispose();
			Assert.DoesNotThrow(() => _debugger.Dispose());
		}

		[Test]
		public void Register_FromAnotherThreadThrowsRatherThanSilentlyDoingNothing()
		{
			ICommandRegistry commands = Commands;
			Exception captured = null;

			Task task = Task.Run(() =>
			{
				try
				{
					commands.Register(new SampleCommands());
				}
				catch (Exception exception)
				{
					captured = exception;
				}
			});

			task.Wait();

			Assert.That(captured, Is.TypeOf<InvalidOperationException>());
			Assert.That(commands.All, Is.Empty);
		}
	}
}
