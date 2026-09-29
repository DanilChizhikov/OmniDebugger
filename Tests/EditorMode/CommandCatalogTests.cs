using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class CommandCatalogTests
	{
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
		public void AddSource_RegistersEveryCommandAndRaisesChangedOnce()
		{
			int changed = 0;
			_debugger.Catalog.OnChanged += () => changed++;

			bool added = _debugger.Catalog.AddSource(new SampleCommands());

			Assert.That(added, Is.True);
			Assert.That(_debugger.Catalog.Commands.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(changed, Is.EqualTo(1), "one change per source, not one per command");
		}

		[Test]
		public void AddSource_IgnoresTheSameInstanceTwice()
		{
			SampleCommands source = new SampleCommands();
			_debugger.Catalog.AddSource(source);

			int changed = 0;
			_debugger.Catalog.OnChanged += () => changed++;
			_log.Clear();

			bool added = _debugger.Catalog.AddSource(source);

			Assert.That(added, Is.False);
			Assert.That(changed, Is.Zero);
			Assert.That(_debugger.Catalog.Commands.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(_log.Warnings, Has.Some.Contains("already registered"));
		}

		[Test]
		public void AddSource_RejectsASecondInstanceOfTheSameTypeAsDuplicateKeys()
		{
			_debugger.Catalog.AddSource(new SampleCommands());
			_log.Clear();

			bool added = _debugger.Catalog.AddSource(new SampleCommands());

			Assert.That(added, Is.False);
			Assert.That(_debugger.Catalog.Commands.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(_log.Errors, Has.Count.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(_log.Errors, Has.All.Contains("Keys have to be unique"));
		}

		[Test]
		public void AddSource_WarnsWithoutErrorWhenASourceHasNoCommands()
		{
			bool added = _debugger.Catalog.AddSource(new object());

			Assert.That(added, Is.False);
			Assert.That(_debugger.Catalog.Commands, Is.Empty);
			Assert.That(_log.Warnings, Has.Some.Contains("produced no commands"));
			Assert.That(_log.Errors, Is.Empty, "an empty source is legal, not an error");
		}

		[Test]
		public void AddSource_RejectsNull() =>
			Assert.Throws<ArgumentNullException>(() => _debugger.Catalog.AddSource(null));

		[Test]
		public void RemoveSource_RemovesExactlyWhatItRegistered()
		{
			SampleCommands source = new SampleCommands();
			_debugger.Catalog.AddSource(source);

			int changed = 0;
			_debugger.Catalog.OnChanged += () => changed++;

			bool removed = _debugger.Catalog.RemoveSource(source);

			Assert.That(removed, Is.True);
			Assert.That(_debugger.Catalog.Commands, Is.Empty);
			Assert.That(changed, Is.EqualTo(1));
		}

		[Test]
		public void RemoveSource_LeavesAnotherOwnersWinningEntryAlone()
		{
			SampleCommands winner = new SampleCommands();
			SampleCommands loser = new SampleCommands();

			_debugger.Catalog.AddSource(winner);
			_debugger.Catalog.AddSource(loser);

			_debugger.Catalog.RemoveSource(loser);

			Assert.That(
				_debugger.Catalog.Commands.Count,
				Is.EqualTo(SampleCommands.ExpectedCommandCount),
				"removing the rejected source must not evict the winner's commands");
		}

		[Test]
		public void RemoveSource_ReturnsFalseForAnUnregisteredObject()
		{
			bool removed = _debugger.Catalog.RemoveSource(new SampleCommands());

			Assert.That(removed, Is.False);
			Assert.That(_log.Warnings, Has.Some.Contains("not registered"));
		}

		[Test]
		public void AddAndRemoveCommand_RoundTripsADelegateCommand()
		{
			ActionCommand command = new ActionCommand(
				new CommandDefinition("Handmade", "Custom", CommandKind.Action),
				() => { });

			Assert.That(_debugger.Catalog.AddCommand(command), Is.True);
			Assert.That(_debugger.Catalog.TryGetCommand("Custom/Handmade", out IDebugCommand found), Is.True);
			Assert.That(found, Is.SameAs(command));

			Assert.That(_debugger.Catalog.RemoveCommand(command), Is.True);
			Assert.That(_debugger.Catalog.Commands, Is.Empty);
		}

		[Test]
		public void Commands_SnapshotTakenBeforeAChangeIsUnaffectedByIt()
		{
			_debugger.Catalog.AddSource(new SampleCommands());
			IReadOnlyList<CommandDefinition> before = _debugger.Catalog.Commands;

			_debugger.Catalog.AddCommand(new ActionCommand(
				new CommandDefinition("Handmade", "Custom", CommandKind.Action),
				() => { }));

			Assert.That(before.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(_debugger.Catalog.Commands.Count, Is.EqualTo(SampleCommands.ExpectedCommandCount + 1));
			Assert.That(_debugger.Catalog.Commands, Is.Not.SameAs(before));
		}

		[Test]
		public void TryGetDefinition_FindsARegisteredKeyAndMissesAnUnknownOne()
		{
			_debugger.Catalog.AddSource(new SampleCommands());

			Assert.That(
				_debugger.Catalog.TryGetDefinition(SampleCommands.AddCoinsKey, out CommandDefinition found),
				Is.True);

			Assert.That(found.Key, Is.EqualTo(SampleCommands.AddCoinsKey));
			Assert.That(_debugger.Catalog.TryGetDefinition("Nope/Missing", out _), Is.False);
		}

		[Test]
		public void Dispose_ReleasesSourcesAndBlocksFurtherUse()
		{
			_debugger.Catalog.AddSource(new SampleCommands());

			_debugger.Dispose();

			Assert.Throws<ObjectDisposedException>(() => _ = _debugger.Catalog);
			Assert.Throws<ObjectDisposedException>(() => _ = _debugger.Commands);
			Assert.Throws<ObjectDisposedException>(() => _ = _debugger.Groups);
		}

		[Test]
		public void Dispose_IsIdempotent()
		{
			_debugger.Dispose();
			Assert.DoesNotThrow(() => _debugger.Dispose());
		}

		[Test]
		public void AddSource_FromAnotherThreadThrowsRatherThanSilentlyDoingNothing()
		{
			ICommandCatalog catalog = _debugger.Catalog;
			Exception captured = null;

			Task task = Task.Run(() =>
			{
				try
				{
					catalog.AddSource(new SampleCommands());
				}
				catch (Exception exception)
				{
					captured = exception;
				}
			});

			task.Wait();

			Assert.That(captured, Is.TypeOf<InvalidOperationException>());
			Assert.That(catalog.Commands, Is.Empty);
		}
	}
}