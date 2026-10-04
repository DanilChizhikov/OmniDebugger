using System;
using System.Collections;
using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class DebugOptionsTests
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
		public void Register_MarksOnlyTheArgumentsThatDeclareOptions()
		{
			Commands.Register(new OptionCommands());

			Assert.That(Commands.TryGet(OptionCommands.LoadPath, out CommandDefinition load), Is.True);
			Assert.That(load.Arguments[0].HasOptions, Is.True);
			Assert.That(load.Arguments[1].HasOptions, Is.False);

			Assert.That(Commands.TryGet(OptionCommands.CurrentPath, out CommandDefinition current), Is.True);
			Assert.That(current.Kind, Is.EqualTo(CommandKind.Value));
			Assert.That(current.Arguments[0].HasOptions, Is.True);
		}

		[Test]
		public void TryGetOptions_ReadsTheSourceAsItIsNow()
		{
			OptionCommands sample = new OptionCommands();
			Commands.Register(sample);

			Assert.That(Read(OptionCommands.LoadPath, 0), Is.EqualTo(new object[] { "level_01", "level_02" }));

			sample.Levels.Add("level_03");

			Assert.That(Read(OptionCommands.LoadPath, 0), Is.EqualTo(new object[] { "level_01", "level_02", "level_03" }));
			Assert.That(Read(OptionCommands.CurrentPath, 0), Is.EqualTo(new object[] { "dark", "light" }));
		}

		[Test]
		public void TryGetOptions_FollowsAReassignedFunc()
		{
			OptionCommands sample = new OptionCommands();
			Commands.Register(sample);

			Assert.That(Read(OptionCommands.PickPath, 0), Is.Empty, "an unassigned Func offers nothing");
			Assert.That(_log.Errors, Is.Empty, "and is not an error");

			sample.Numbers = () => new[] { 3, 5 };
			Assert.That(Read(OptionCommands.PickPath, 0), Is.EqualTo(new object[] { 3, 5 }));

			sample.Numbers = () => new[] { 8 };
			Assert.That(Read(OptionCommands.PickPath, 0), Is.EqualTo(new object[] { 8 }));
		}

		[Test]
		public void TryGetOptions_LeavesOutNullsAndValuesOfTheWrongType()
		{
			Commands.Register(new OptionCommands());

			Assert.That(Read(OptionCommands.MixedPath, 0), Is.EqualTo(new object[] { 1, 3 }));
		}

		[Test]
		public void TryGetOptions_FailsWithAReasonForAnArgumentWithoutOptions()
		{
			Commands.Register(new OptionCommands());
			List<object> options = new List<object>();

			Assert.That(Commands.TryGetOptions(OptionCommands.LoadPath, 1, options), Is.False);
			Assert.That(Commands.TryGetOptions("Options/Missing", 0, options), Is.False);
			Assert.That(_log.Errors.Count, Is.EqualTo(2));
		}

		[Test]
		public void TryGetOptions_LogsASourceThatThrows()
		{
			OptionCommands sample = new OptionCommands();
			Commands.Register(sample);
			sample.Numbers = () => throw new InvalidOperationException("no numbers");

			Assert.That(Commands.TryGetOptions(OptionCommands.PickPath, 0, new List<object>()), Is.False);
			Assert.That(_log.Exceptions.Count, Is.EqualTo(1));
		}

		[Test]
		public void PickedOption_RunsTheCommand()
		{
			OptionCommands sample = new OptionCommands();
			Commands.Register(sample);

			Assert.That(Commands.TryExecute(OptionCommands.LoadPath, Read(OptionCommands.LoadPath, 0)[1]), Is.True);
			Assert.That(sample.LastLevel, Is.EqualTo("level_02"));
		}

		[Test]
		public void Builder_RegistersOptionsWithTheArgument()
		{
			string theme = "dark";
			string loaded = null;
			string[] themes = { "dark", "light", "auto" };

			Commands.Build()
				.Group("Built")
					.Dropdown("Theme", () => themes, () => theme, value => theme = value)
					.Button<string>("Load", level => loaded = level, a => a.Options(() => new[] { "a", "b" }))
				.Register();

			Assert.That(Commands.TryGet("Built/Theme", out CommandDefinition dropdown), Is.True);
			Assert.That(dropdown.Arguments[0].HasOptions, Is.True);
			Assert.That(Read("Built/Theme", 0), Is.EqualTo(new object[] { "dark", "light", "auto" }));
			Assert.That(Read("Built/Load", 0), Is.EqualTo(new object[] { "a", "b" }));

			Assert.That(Commands.TryExecute("Built/Theme", "auto"), Is.True);
			Assert.That(theme, Is.EqualTo("auto"));
			Assert.That(Commands.TryExecute("Built/Load", "b"), Is.True);
			Assert.That(loaded, Is.EqualTo("b"));
		}

		[Test]
		public void DebugCommand_RejectsOptionsThatDoNotMatchTheArguments()
		{
			ArgumentDefinition picked = new ArgumentDefinition("id", typeof(string), hasOptions: true);
			ArgumentDefinition plain = new ArgumentDefinition("id", typeof(string));
			Func<IEnumerable> source = () => new[] { "a" };

			CommandDefinition withOptions = new CommandDefinition("Run", "G", CommandKind.Action, arguments: new[] { picked });
			CommandDefinition withoutOptions = new CommandDefinition("Run", "G", CommandKind.Action, arguments: new[] { plain });

			Assert.Throws<ArgumentException>(() => new DebugCommand(withOptions, invoke: _ => { }));
			Assert.Throws<ArgumentException>(() => new DebugCommand(withOptions, invoke: _ => { }, options: new Func<IEnumerable>[] { null }));
			Assert.Throws<ArgumentException>(() => new DebugCommand(withOptions, invoke: _ => { }, options: new[] { source, source }));
			Assert.Throws<ArgumentException>(() => new DebugCommand(withoutOptions, invoke: _ => { }, options: new[] { source }));
			Assert.DoesNotThrow(() => new DebugCommand(withOptions, invoke: _ => { }, options: new[] { source }));
		}

		[Test]
		public void Definition_MentionsItsOptions()
		{
			Assert.That(new ArgumentDefinition("id", typeof(string), hasOptions: true).ToString(), Is.EqualTo("String id {options}"));
		}

		[Test]
		public void OptionField_StartsOnTheRememberedValueWhenItIsStillOffered()
		{
			IArgumentField field = CreateField("level_02", "level_01", "level_02");

			try
			{
				Assert.That(field.TryGetValue(out object value), Is.True);
				Assert.That(value, Is.EqualTo("level_02"));
			}
			finally
			{
				field.Dispose();
			}
		}

		[Test]
		public void OptionField_StartsOnTheFirstOptionOtherwise()
		{
			IArgumentField field = CreateField("gone", "level_01", "level_02");

			try
			{
				Assert.That(field.TryGetValue(out object value), Is.True);
				Assert.That(value, Is.EqualTo("level_01"));
			}
			finally
			{
				field.Dispose();
			}
		}

		[Test]
		public void OptionField_WithNothingToOfferHasNoValue()
		{
			IArgumentField field = CreateField(null);

			try
			{
				Assert.That(field.TryGetValue(out _), Is.False);
			}
			finally
			{
				field.Dispose();
			}
		}

		[Test]
		public void OptionField_WinsOverTheHandlerForItsType()
		{
			IArgumentField field = CreateField(null, "a");

			try
			{
				Assert.That(field.Root, Is.InstanceOf<ChoicePopupField>());
			}
			finally
			{
				field.Dispose();
			}
		}

		private object[] Read(string path, int argumentIndex)
		{
			List<object> options = new List<object>();
			Assert.That(Commands.TryGetOptions(path, argumentIndex, options), Is.True);
			return options.ToArray();
		}

		private IArgumentField CreateField(object initial, params object[] options)
		{
			ArgumentDefinition argument = new ArgumentDefinition("level", typeof(string), hasOptions: true);
			ArgumentFieldRequest request = ArgumentFieldRequest.For(
				argument,
				initial,
				showLabel: false,
				list =>
				{
					foreach (object option in options)
					{
						list.Add(option);
					}
				});

			return _debugger.Fields.Create(request);
		}
	}
}
