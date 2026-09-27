using System;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class CommandInvokerTests
	{
		private const string Origin = "Test";

		private RecordingLogSink _log;
		private OmniDebugger _debugger;
		private SampleCommands _source;

		[SetUp]
		public void SetUp()
		{
			_log = new RecordingLogSink();
			_debugger = new OmniDebugger(_log);
			_source = new SampleCommands();
			_debugger.Catalog.AddSource(_source);
			_log.Clear();
		}

		[TearDown]
		public void TearDown() => _debugger.Dispose();

		[Test]
		public void TryExecute_AppliesTheDefaultOfAnOmittedOptionalArgument()
		{
			bool executed = _debugger.Commands.TryExecute(
				SampleCommands.AddCoinsKey,
				InvocationRequest.From(Origin));

			Assert.That(executed, Is.True);
			Assert.That(_source.Coins, Is.EqualTo(100));
		}

		[Test]
		public void TryExecute_PassesAnAlreadyTypedArgumentThrough()
		{
			_debugger.Commands.TryExecute(SampleCommands.AddCoinsKey, InvocationRequest.From(Origin, 250));

			Assert.That(_source.Coins, Is.EqualTo(250));
		}

		[Test]
		public void TryExecute_CoercesTextToTheDeclaredArgumentType()
		{
			bool executed = _debugger.Commands.TryExecute(
				SampleCommands.AddCoinsKey,
				InvocationRequest.From(Origin, "250"));

			Assert.That(executed, Is.True);
			Assert.That(_source.Coins, Is.EqualTo(250));
		}

		[Test]
		public void TryExecute_CoercesTextToAnEnumByName()
		{
			bool executed = _debugger.Commands.TryExecute(
				SampleCommands.Group + "/Set Severity",
				InvocationRequest.From(Origin, "two"));

			Assert.That(executed, Is.True);
			Assert.That(_source.Severity, Is.EqualTo(SampleSeverity.Two));
		}

		[Test]
		public void TryExecute_WritesAValueCommand()
		{
			bool executed = _debugger.Commands.TryExecute(
				SampleCommands.GodModeKey,
				InvocationRequest.From(Origin, true));

			Assert.That(executed, Is.True);
			Assert.That(_source.GodMode, Is.True);
		}

		[Test]
		public void TryExecute_ReportsAThrowingCommandWithTheRealException()
		{
			bool executed = _debugger.Commands.TryExecute(SampleCommands.BoomKey, InvocationRequest.From(Origin));

			Assert.That(executed, Is.False);
			Assert.That(_source.BoomCalled, Is.True, "the command did run");
			Assert.That(_log.Exceptions, Has.Count.EqualTo(1));
			Assert.That(
				_log.Exceptions[0],
				Is.TypeOf<InvalidOperationException>(),
				"reflection's TargetInvocationException must be unwrapped");

			Assert.That(_log.Exceptions[0].Message, Is.EqualTo(SampleCommands.BoomMessage));
		}

		[Test]
		public void TryExecute_LogsTheInvocationBeforeRunningIt()
		{
			_debugger.Commands.TryExecute(SampleCommands.BoomKey, InvocationRequest.From(Origin));

			Assert.That(_log.Infos, Has.Some.Contains(SampleCommands.BoomKey));
			Assert.That(_log.Infos, Has.Some.Contains(Origin));
		}

		[Test]
		public void TryExecute_RefusesAReadonlyValueCommand()
		{
			bool executed = _debugger.Commands.TryExecute(
				SampleCommands.BuildVersionKey,
				InvocationRequest.From(Origin));

			Assert.That(executed, Is.False);
			Assert.That(_log.Errors, Has.Some.Contains("cannot be executed"));
		}

		[Test]
		public void TryExecute_ReportsAnUnknownKey()
		{
			bool executed = _debugger.Commands.TryExecute("Nope/Missing", InvocationRequest.From(Origin));

			Assert.That(executed, Is.False);
			Assert.That(_log.Errors, Has.Some.Contains("was not found"));
		}

		[Test]
		public void TryExecute_RejectsTooManyArgumentsWithoutCallingTheCommand()
		{
			bool executed = _debugger.Commands.TryExecute(
				SampleCommands.AddCoinsKey,
				InvocationRequest.From(Origin, 1, 2));

			Assert.That(executed, Is.False);
			Assert.That(_source.Coins, Is.Zero);
			Assert.That(_log.Errors, Has.Some.Contains("arguments are invalid"));
			Assert.That(_log.Infos, Is.Empty, "nothing was executed, so nothing is audited");
		}

		[Test]
		public void TryExecute_RejectsAMissingRequiredArgument()
		{
			bool executed = _debugger.Commands.TryExecute(
				SampleCommands.Group + "/Set Speed",
				InvocationRequest.From(Origin));

			Assert.That(executed, Is.False);
			Assert.That(_log.Errors, Has.Some.Contains("is required"));
		}

		[Test]
		public void TryExecute_RejectsUnreadableText()
		{
			bool executed = _debugger.Commands.TryExecute(
				SampleCommands.AddCoinsKey,
				InvocationRequest.From(Origin, "not a number"));

			Assert.That(executed, Is.False);
			Assert.That(_source.Coins, Is.Zero);
			Assert.That(_log.Errors, Has.Some.Contains("cannot be read as Int32"));
		}

		[Test]
		public void TryExecute_AcceptsADefinitionInPlaceOfAKey()
		{
			Assert.That(
				_debugger.Catalog.TryGetDefinition(SampleCommands.AddCoinsKey, out CommandDefinition definition),
				Is.True);

			bool executed = _debugger.Commands.TryExecute(definition, InvocationRequest.From(Origin, 5));

			Assert.That(executed, Is.True);
			Assert.That(_source.Coins, Is.EqualTo(5));
		}

		[Test]
		public void TryGetValue_ReadsAReadonlyValueCommand()
		{
			bool read = _debugger.Commands.TryGetValue(SampleCommands.BuildVersionKey, out object value);

			Assert.That(read, Is.True);
			Assert.That(value, Is.EqualTo("1.0.0"));
		}

		[Test]
		public void TryGetValue_ReadsAWritableValueCommand()
		{
			_source.GodMode = true;

			bool read = _debugger.Commands.TryGetValue(SampleCommands.GodModeKey, out object value);

			Assert.That(read, Is.True);
			Assert.That(value, Is.EqualTo(true));
		}

		[Test]
		public void TryGetValue_RefusesAnActionCommand()
		{
			bool read = _debugger.Commands.TryGetValue(SampleCommands.BoomKey, out object value);

			Assert.That(read, Is.False);
			Assert.That(value, Is.Null);
			Assert.That(_log.Errors, Has.Some.Contains("holds no value"));
		}

		[Test]
		public void TryExecute_RejectsABlankKey() =>
			Assert.Throws<ArgumentException>(
				() => _debugger.Commands.TryExecute(" ", InvocationRequest.From(Origin)));

		[Test]
		public void InvocationRequest_IsSafeToReadWhenDefaultConstructed()
		{
			InvocationRequest request = default;

			Assert.That(request.Origin, Is.EqualTo(InvocationRequest.UnknownOrigin));
			Assert.That(request.Arguments, Is.Not.Null.And.Empty);
		}
	}
}