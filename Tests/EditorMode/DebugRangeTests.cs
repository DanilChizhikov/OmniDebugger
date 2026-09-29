using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class DebugRangeTests
	{
		private RecordingLogSink _log;

		[SetUp]
		public void SetUp() => _log = new RecordingLogSink();

		[Test]
		public void Scan_CarriesThePropertyRangeOnItsArgument()
		{
			ArgumentRange range = Scan()[RangedCommands.SpeedKey].Arguments[0].Range;

			Assert.That(range.IsEmpty, Is.False);
			Assert.That(range.Min, Is.EqualTo(0.5));
			Assert.That(range.Max, Is.EqualTo(3.0));
			Assert.That(range.Step, Is.EqualTo(0.25));
		}

		[Test]
		public void Scan_CarriesParameterRangesArgumentByArgument()
		{
			IReadOnlyList<ArgumentDefinition> arguments = Scan()[RangedCommands.TeleportKey].Arguments;

			Assert.That(arguments[0].Range.Min, Is.EqualTo(-100.0));
			Assert.That(arguments[0].Range.Max, Is.EqualTo(100.0));
			Assert.That(arguments[0].Range.Step, Is.EqualTo(0.0));
			Assert.That(arguments[1].Range.IsEmpty, Is.True, "a range belongs to the parameter it is declared on");
		}

		[Test]
		public void Scan_LeavesUndeclaredRangesEmpty()
		{
			Assert.That(Scan()[RangedCommands.NameKey].Arguments[0].Range.IsEmpty, Is.True);
		}

		[Test]
		public void Scan_SkipsAMemberWhoseRangeIsInvalid()
		{
			Dictionary<string, CommandDefinition> definitions = Scan();

			Assert.That(definitions.Keys, Has.None.EqualTo(RangedCommands.BrokenKey));
			Assert.That(_log.Warnings, Has.Some.Contains("Broken"));
			Assert.That(_log.Warnings, Has.Some.Contains("could not be read"));
		}

		[Test]
		public void Range_RejectsReversedBoundsAndNegativeSteps()
		{
			Assert.Throws<ArgumentException>(() => new ArgumentRange(1.0, 1.0));
			Assert.Throws<ArgumentException>(() => new ArgumentRange(2.0, 1.0));
			Assert.Throws<ArgumentException>(() => new ArgumentRange(double.NaN, 1.0));
			Assert.Throws<ArgumentOutOfRangeException>(() => new ArgumentRange(0.0, 1.0, -0.5));
		}

		[Test]
		public void Range_DefaultsToEmpty()
		{
			Assert.That(default(ArgumentRange).IsEmpty, Is.True);
			Assert.That(new ArgumentDefinition("value", typeof(int)).Range.IsEmpty, Is.True);
		}

		[Test]
		public void Definition_MentionsItsRange()
		{
			ArgumentDefinition ranged = new ArgumentDefinition("speed", typeof(float), range: new ArgumentRange(0.0, 2.0));
			ArgumentDefinition stepped = new ArgumentDefinition("level", typeof(int), range: new ArgumentRange(1.0, 10.0, 1.0));

			Assert.That(ranged.ToString(), Is.EqualTo("Single speed [0..2]"));
			Assert.That(stepped.ToString(), Is.EqualTo("Int32 level [1..10] step 1"));
		}

		private Dictionary<string, CommandDefinition> Scan() =>
			SourceScanner.Scan(new RangedCommands(), _log)
				.Commands
				.ToDictionary(command => command.Definition.Key, command => command.Definition);
	}
}
