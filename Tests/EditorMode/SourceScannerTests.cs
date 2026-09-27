using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class SourceScannerTests
	{
		private RecordingLogSink _log;

		[SetUp]
		public void SetUp() => _log = new RecordingLogSink();

		[Test]
		public void Scan_FindsEveryAcceptedMember()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out ScanResult result);

			Assert.That(result.Commands, Has.Count.EqualTo(SampleCommands.ExpectedCommandCount));
			Assert.That(definitions.Keys, Contains.Item(SampleCommands.AddCoinsKey));
			Assert.That(definitions.Keys, Contains.Item(SampleCommands.GodModeKey));
			Assert.That(definitions.Keys, Contains.Item(SampleCommands.BuildVersionKey));
		}

		[Test]
		public void Scan_MapsMemberShapeToKind()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out _);

			Assert.That(definitions[SampleCommands.AddCoinsKey].Kind, Is.EqualTo(CommandKind.Action));
			Assert.That(definitions[SampleCommands.GodModeKey].Kind, Is.EqualTo(CommandKind.Value));
			Assert.That(definitions[SampleCommands.BuildVersionKey].Kind, Is.EqualTo(CommandKind.ReadonlyValue));
		}

		[Test]
		public void Scan_TreatsPrivateSetterAsReadonly()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out _);

			Assert.That(
				definitions[SampleCommands.Group + "/Private Setter"].Kind,
				Is.EqualTo(CommandKind.ReadonlyValue));
		}

		[Test]
		public void Scan_FallsBackToMemberNameWhenAttributeGivesNone()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out _);

			Assert.That(definitions.Keys, Contains.Item(SampleCommands.Group + "/Unnamed"));
		}

		[Test]
		public void Scan_CarriesArgumentTypeDefaultAndOptionality()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out _);

			IReadOnlyList<ArgumentDefinition> arguments = definitions[SampleCommands.AddCoinsKey].Arguments;

			Assert.That(arguments.Count, Is.EqualTo(1));
			Assert.That(arguments[0].Name, Is.EqualTo("amount"));
			Assert.That(arguments[0].Type, Is.EqualTo(typeof(int)));
			Assert.That(arguments[0].IsOptional, Is.True);
			Assert.That(arguments[0].DefaultValue, Is.EqualTo(100));
		}

		[Test]
		public void Scan_GivesZeroArgumentCommandAnEmptyArgumentList()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out _);

			IReadOnlyList<ArgumentDefinition> arguments = definitions[SampleCommands.BoomKey].Arguments;

			Assert.That(arguments, Is.Not.Null);
			Assert.That(arguments, Is.Empty);
		}

		[Test]
		public void Scan_SeedsIndexTermsWithNameGroupAndDeclaredTags()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out _);

			List<string> tags = new List<string>();
			definitions[SampleCommands.AddCoinsKey].CollectIndexTerms(tags);

			Assert.That(tags, Contains.Item("Add Coins"));
			Assert.That(tags, Contains.Item(SampleCommands.Group));
			Assert.That(tags, Contains.Item("economy"));
			Assert.That(tags, Contains.Item("wallet"));
		}

		[Test]
		public void Scan_KeepsDescriptionFromTheAttribute()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out _);

			Assert.That(
				definitions[SampleCommands.AddCoinsKey].Description,
				Is.EqualTo("Adds coins to the wallet."));

			Assert.That(definitions[SampleCommands.BoomKey].Description, Is.Null);
		}

		[Test]
		public void Scan_WarnsOncePerRejectedMemberWithTheReason()
		{
			SourceScanner.Scan(new SampleCommands(), _log);

			Assert.That(_log.Warnings, Has.Count.EqualTo(SampleCommands.ExpectedSkippedCount));
			Assert.That(_log.Warnings, Has.Some.Contains("StaticCommand"));
			Assert.That(_log.Warnings, Has.Some.Contains("this member is static"));
			Assert.That(_log.Warnings, Has.Some.Contains("ReturnsValue"));
			Assert.That(_log.Warnings, Has.Some.Contains("has to return void"));
			Assert.That(_log.Warnings, Has.Some.Contains("HiddenCommand"));
			Assert.That(_log.Warnings, Has.Some.Contains("this member is not public"));
			Assert.That(_log.Errors, Is.Empty);
		}

		[Test]
		public void Scan_IgnoresUnattributedMembersSilently()
		{
			Dictionary<string, CommandDefinition> definitions = ScanSample(out _);

			Assert.That(definitions.Keys, Has.None.Contains("NotACommand"));
			Assert.That(_log.Warnings, Has.None.Contains("NotACommand"));
		}

		[Test]
		public void Scan_ReflectsATypeOnlyOnce()
		{
			MemberBinding[] first = TypeBindingCache.GetBindings(typeof(SampleCommands));
			MemberBinding[] second = TypeBindingCache.GetBindings(typeof(SampleCommands));

			Assert.That(second, Is.SameAs(first), "the cache must hand back the same array");
		}

		[Test]
		public void Scan_SharesOneDefinitionAcrossInstancesOfTheSameType()
		{
			CommandDefinition fromFirst = FindDefinition(new SampleCommands(), SampleCommands.AddCoinsKey);
			CommandDefinition fromSecond = FindDefinition(new SampleCommands(), SampleCommands.AddCoinsKey);

			Assert.That(fromSecond, Is.SameAs(fromFirst), "definitions are target independent and cached");
		}

		[Test]
		public void Scan_UsesSuppliedCommandsWhenTheSourceOptsOutOfReflection()
		{
			CustomSource source = new CustomSource();

			ScanResult result = SourceScanner.Scan(source, _log);

			Assert.That(result.Commands, Has.Count.EqualTo(1));
			Assert.That(result.Commands[0], Is.SameAs(source.Command));
			Assert.That(_log.Warnings, Is.Empty);
		}

		[Test]
		public void Scan_IgnoresNullCommandsFromASourceWithAWarning()
		{
			ScanResult result = SourceScanner.Scan(new NullSuppliedSource(), _log);

			Assert.That(result.IsEmpty, Is.True);
			Assert.That(_log.Warnings, Has.Some.Contains("null command"));
		}
		
		private Dictionary<string, CommandDefinition> ScanSample(out ScanResult result)
		{
			result = SourceScanner.Scan(new SampleCommands(), _log);
			return result.Commands.ToDictionary(command => command.Definition.Key, command => command.Definition);
		}

		private CommandDefinition FindDefinition(object source, string key)
		{
			ScanResult result = SourceScanner.Scan(source, _log);

			foreach (IDebugCommand command in result.Commands)
			{
				if (command.Definition.Key == key)
				{
					return command.Definition;
				}
			}

			Assert.Fail($"'{key}' was not produced by the scan.");
			return null;
		}

		private sealed class CustomSource : ICommandSource
		{
			public ActionCommand Command { get; } = new ActionCommand(
				new CommandDefinition("Handmade", "Custom", CommandKind.Action),
				() => { });

			[DebugCommand("Custom", "Attributed")]
			public void Attributed()
			{
			}

			public IEnumerable<IDebugCommand> GetCommands()
			{
				yield return Command;
			}
		}

		private sealed class NullSuppliedSource : ICommandSource
		{
			public IEnumerable<IDebugCommand> GetCommands()
			{
				yield return null;
			}
		}
	}
}