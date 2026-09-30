using System;
using System.Collections.Generic;
using System.Linq;
using DTech.OmniDebugger.UI;
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
			ArgumentRange range = Scan()[RangedCommands.SpeedPath].Arguments[0].Range;

			Assert.That(range.IsEmpty, Is.False);
			Assert.That(range.Min, Is.EqualTo(0.5));
			Assert.That(range.Max, Is.EqualTo(3.0));
			Assert.That(range.Step, Is.EqualTo(0.25));
		}

		[Test]
		public void Scan_CarriesParameterRangesArgumentByArgument()
		{
			IReadOnlyList<ArgumentDefinition> arguments = Scan()[RangedCommands.TeleportPath].Arguments;

			Assert.That(arguments[0].Range.Min, Is.EqualTo(-100.0));
			Assert.That(arguments[0].Range.Max, Is.EqualTo(100.0));
			Assert.That(arguments[0].Range.Step, Is.EqualTo(0.0));
			Assert.That(arguments[1].Range.IsEmpty, Is.True, "a range belongs to the parameter it is declared on");
		}

		[Test]
		public void Scan_LeavesUndeclaredRangesEmpty()
		{
			Assert.That(Scan()[RangedCommands.NamePath].Arguments[0].Range.IsEmpty, Is.True);
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

		[Test]
		public void RangedField_KeepsTheDecimalsOfAStepWrittenInExponentForm()
		{
			ArgumentDefinition argument = new ArgumentDefinition(
				"precision",
				typeof(float),
				range: new ArgumentRange(0.0, 0.001, 0.00001));

			IArgumentField field = new NumericArgumentFieldHandler().Create(
				ArgumentFieldRequest.For(argument, null, showLabel: false));

			try
			{
				field.SetValue(0.00003f);

				Assert.That(field.TryGetValue(out object value), Is.True);
				Assert.That((float)value, Is.EqualTo(0.00003f).Within(1e-9f), "a 1E-05 step keeps five decimals");
			}
			finally
			{
				field.Dispose();
			}
		}

		private Dictionary<string, CommandDefinition> Scan()
		{
			using OmniDebuggerHost debugger = new OmniDebuggerHost(_log);
			debugger.Commands.Register(new RangedCommands());
			return debugger.Commands.All.ToDictionary(definition => definition.Path);
		}
	}
}
