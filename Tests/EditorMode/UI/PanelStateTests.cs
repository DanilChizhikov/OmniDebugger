using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using NUnit.Framework;
using UnityEngine;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class PanelStateTests
	{
		private OmniDebuggerHost _debugger;

		[SetUp]
		public void SetUp() => _debugger = new OmniDebuggerHost(new RecordingLogSink());

		[TearDown]
		public void TearDown() => _debugger.Dispose();

		[Test]
		public void Hotbar_KeepsPinOrderAndNormalizesPaths()
		{
			IHotbar hotbar = _debugger.Hotbar;

			hotbar.Pin("Economy/Coins");
			hotbar.Pin(" Logs / TestLog ");
			hotbar.Toggle("Economy/Coins");
			hotbar.Toggle("Economy/Coins");

			Assert.That(hotbar.Paths, Is.EqualTo(new[] { "Logs/TestLog", "Economy/Coins" }));
			Assert.That(hotbar.Contains("Logs/TestLog"), Is.True);

			hotbar.Move(1, 0);
			Assert.That(hotbar.Paths, Is.EqualTo(new[] { "Economy/Coins", "Logs/TestLog" }));
		}

		[Test]
		public void KeyListFormat_RoundTripsInOrderAndSkipsBlanksAndDuplicates()
		{
			string stored = KeyListFormat.Join(new[] { "Logs/TestLog", "Economy/Coins" });

			Assert.That(KeyListFormat.Split(stored), Is.EqualTo(new[] { "Logs/TestLog", "Economy/Coins" }));
			Assert.That(KeyListFormat.Split("A/B\n\n \nA/B\nC/D"), Is.EqualTo(new[] { "A/B", "C/D" }));
			Assert.That(KeyListFormat.Split(null), Is.Empty);
			Assert.That(KeyListFormat.Join(new string[0]), Is.Empty);
		}

		[Test]
		public void ViewState_ClearForgetsTypedArguments()
		{
			OmniDebuggerViewState state = new OmniDebuggerViewState();
			state.Commands.Get("Economy/Add").SetArgument("amount", 5);

			state.Clear();

			Assert.That(state.Commands.TryGet("Economy/Add", out _), Is.False);
		}

		[Test]
		public void Hotbar_RaisesChangedOnlyForRealChanges()
		{
			IHotbar hotbar = _debugger.Hotbar;
			int changes = 0;
			hotbar.OnChanged += () => changes++;

			hotbar.Pin("A/B");
			hotbar.Pin("A/B");
			hotbar.Unpin("missing");
			hotbar.Clear();
			hotbar.Clear();

			Assert.That(changes, Is.EqualTo(2));
		}

		[Test]
		public void CommandStateStore_RoundTripsArgumentsAsInvariantText()
		{
			CommandStateStore store = new CommandStateStore();
			CommandState state = store.Get("World/Teleport");
			state.SetArgument("x", 1.5f);
			state.SetArgument("name", "tab\there\nnext");
			state.SetArgument("flag", true);
			state.SetArgument("severity", SampleSeverity.Two);
			state.SetArgument("gone", 3);
			state.SetArgument("gone", null);

			CommandStateStore restored = new CommandStateStore();
			restored.Restore(store.Serialize());
			CommandState read = restored.Get("World/Teleport");

			Assert.That(read.TryGetArgument(new ArgumentDefinition("x", typeof(float)), out object x), Is.True);
			Assert.That(x, Is.EqualTo(1.5f));
			Assert.That(read.TryGetArgument(new ArgumentDefinition("name", typeof(string)), out object name), Is.True);
			Assert.That(name, Is.EqualTo("tab\there\nnext"));
			Assert.That(read.TryGetArgument(new ArgumentDefinition("flag", typeof(bool)), out object flag), Is.True);
			Assert.That(flag, Is.EqualTo(true));
			Assert.That(read.TryGetArgument(new ArgumentDefinition("severity", typeof(SampleSeverity)), out object severity), Is.True);
			Assert.That(severity, Is.EqualTo(SampleSeverity.Two));
			Assert.That(read.TryGetArgument(new ArgumentDefinition("gone", typeof(int)), out _), Is.False);
		}

		[Test]
		public void CommandState_DropsAStoredValueItsArgumentCannotRead()
		{
			CommandStateStore store = new CommandStateStore();
			store.Restore("A/B\tcount\tnot a number\n");

			CommandState state = store.Get("A/B");

			Assert.That(state.TryGetArgument(new ArgumentDefinition("count", typeof(int)), out _), Is.False);
			Assert.That(state.IsEmpty, Is.True);
		}

		[Test]
		public void CommandStateStore_ReportsOnlyRealChanges()
		{
			CommandStateStore store = new CommandStateStore();
			int changes = 0;
			store.OnChanged += () => changes++;

			CommandState state = store.Get("A/B");
			state.SetArgument("x", 1);
			state.SetArgument("x", 1);
			state.SetArgument("y", null);
			state.SetArgument("x", null);

			Assert.That(changes, Is.EqualTo(2));
		}

		[Test]
		public void Info_FloatAndDockReportRealChangesOnly()
		{
			TitledInfo stats = new TitledInfo("Stats");
			_debugger.Info.Register(stats);
			int changes = 0;
			_debugger.Info.OnFloatingChanged += Count;

			try
			{
				Assert.That(_debugger.Info.Float(stats), Is.True);
				Assert.That(_debugger.Info.Float(stats), Is.False);
				Assert.That(_debugger.Info.IsFloating(stats), Is.True);
				Assert.That(_debugger.Info.Dock(stats), Is.True);
				Assert.That(_debugger.Info.Dock(stats), Is.False);
				Assert.That(_debugger.Info.Float(null), Is.False);
			}
			finally
			{
				_debugger.Info.OnFloatingChanged -= Count;
			}

			Assert.That(changes, Is.EqualTo(2));
			Assert.That(_debugger.Info.IsFloating(stats), Is.False);

			void Count() => changes++;
		}

		[Test]
		public void Info_FloatingFollowsTheTitleAcrossRegistrations()
		{
			TitledInfo first = new TitledInfo("Stats");
			_debugger.Info.Register(first);
			_debugger.Info.Float(first);
			_debugger.Info.Unregister(first);

			TitledInfo second = new TitledInfo("Stats");
			_debugger.Info.Register(second);

			Assert.That(_debugger.Info.IsFloating(second), Is.True);
			Assert.That(_debugger.Info.IsFloating(new TitledInfo("Other")), Is.False);
		}

		[Test]
		public void Info_RestoredFloatingKeepsCodeOrderAndSkipsDuplicates()
		{
			InfoRegistry registry = (InfoRegistry)_debugger.Info;
			registry.Float(new TitledInfo("A"));

			registry.RestoreFloating(new[] { "B", "A", " ", "B" });

			Assert.That(registry.FloatingKeys, Is.EqualTo(new[] { "A", "B" }));
		}

		[Test]
		public void InfoSection_CommandRowNormalizesItsPathAndRejectsABlankOne()
		{
			InfoSectionModel model = InfoSectionModel.Describe(new CommandInfo(" Player / God Mode "));

			Assert.That(model.Items, Has.Count.EqualTo(1));
			Assert.That(model.Items[0].Kind, Is.EqualTo(InfoItemKind.Command));
			Assert.That(model.Items[0].Path, Is.EqualTo("Player/God Mode"));
			Assert.That(model.Items[0].Label, Is.EqualTo("God Mode"));

			InfoSectionModel blank = InfoSectionModel.Describe(new CommandInfo(" "));
			Assert.That(blank.Error, Is.InstanceOf<System.ArgumentException>());
		}

		[Test]
		public void FloatingSection_GripScalesAlongTheDiagonalInStepsWithinLimits()
		{
			Vector2 size = new Vector2(200.0f, 100.0f);

			Assert.That(FloatingSection.ResizeScale(1.0f, size, Vector2.zero), Is.EqualTo(1.0f).Within(0.0001f));
			Assert.That(FloatingSection.ResizeScale(1.0f, size, new Vector2(150.0f, 0.0f)), Is.EqualTo(1.5f).Within(0.0001f));
			Assert.That(FloatingSection.ResizeScale(1.0f, size, new Vector2(4.0f, 4.0f)), Is.EqualTo(1.05f).Within(0.0001f));
			Assert.That(FloatingSection.ResizeScale(1.0f, size, new Vector2(-1000.0f, 0.0f)), Is.EqualTo(FloatingSection.MinScale));
			Assert.That(FloatingSection.ResizeScale(1.0f, size, new Vector2(1000.0f, 1000.0f)), Is.EqualTo(FloatingSection.MaxScale));
			Assert.That(FloatingSection.ResizeScale(1.2f, Vector2.zero, new Vector2(50.0f, 50.0f)), Is.EqualTo(1.2f).Within(0.0001f));
		}

		[Test]
		public void ScaleListFormat_RoundTripsAndSkipsBrokenLines()
		{
			Dictionary<string, float> scales = new Dictionary<string, float> { ["Stats"] = 1.25f, ["Quick"] = 0.5f };

			IReadOnlyDictionary<string, float> read = ScaleListFormat.Parse(ScaleListFormat.Format(scales));

			Assert.That(read["Stats"], Is.EqualTo(1.25f));
			Assert.That(read["Quick"], Is.EqualTo(0.5f));
			Assert.That(ScaleListFormat.Parse("x\tStats\n0\tZero\n1.5\t\n2\tWide").Count, Is.EqualTo(1));
			Assert.That(ScaleListFormat.Parse(null), Is.Empty);
		}

		[Test]
		public void Scaling_AutoPicksScreenSizeOnMobileOnly()
		{
			Assert.That(PanelScaling.Resolve(OmniDebuggerScaleMode.Auto, isMobilePlatform: true), Is.EqualTo(OmniDebuggerScaleMode.ScreenSize));
			Assert.That(PanelScaling.Resolve(OmniDebuggerScaleMode.Auto, isMobilePlatform: false), Is.EqualTo(OmniDebuggerScaleMode.PhysicalSize));
			Assert.That(PanelScaling.Resolve(OmniDebuggerScaleMode.PhysicalSize, isMobilePlatform: true), Is.EqualTo(OmniDebuggerScaleMode.PhysicalSize));
		}

		[Test]
		public void Scaling_MatchesWidthInPortraitAndLeansToHeightInLandscape()
		{
			Assert.That(PanelScaling.ResolveMatch(1080.0f, 1920.0f), Is.EqualTo(0.0f).Within(0.01f));
			Assert.That(PanelScaling.ResolveMatch(1920.0f, 1080.0f), Is.EqualTo(0.75f).Within(0.01f));
			Assert.That(PanelScaling.ResolveMatch(0.0f, 1080.0f), Is.Zero);
		}

		[Test]
		public void Options_DefaultToAPanelAndAVisibleButton()
		{
			OmniDebuggerOptions options = new OmniDebuggerOptions();

			Assert.That(options.CreateOnStartup, Is.False);
			Assert.That(options.CreatePanel, Is.True);
			Assert.That(options.Panel.ScaleMode, Is.EqualTo(OmniDebuggerScaleMode.Auto));
			Assert.That(options.Panel.LandscapeLayout, Is.EqualTo(OmniDebuggerLandscapeLayout.Floating));
			Assert.That(options.Panel.FloatingScale, Is.EqualTo(1.0f));
			Assert.That(options.Panel.Open.ButtonEnabled, Is.True);
			Assert.That(options.Panel.Open.ButtonOpacity, Is.EqualTo(0.5f));
			Assert.That(options.Panel.Open.Shortcuts, Is.Empty);

			options.Panel.Scale = -1.0f;
			Assert.That(options.Panel.Scale, Is.EqualTo(1.0f));

			options.Panel.FloatingScale = 0.0f;
			Assert.That(options.Panel.FloatingScale, Is.EqualTo(1.0f));
		}

		[Test]
		public void Debugger_BuiltInEditMode_CreatesNoPanelButRegistersForTheEditorWindow()
		{
			OmniDebuggerHost debugger = new OmniDebuggerHost(new OmniDebuggerOptions());

			try
			{
				Assert.That(debugger.Panel, Is.Null, "nothing is built outside play mode");
				Assert.That(OmniDebuggerViews.Current, Is.SameAs(debugger));
				Assert.That(debugger.Logs, Is.Not.Null);
			}
			finally
			{
				debugger.Dispose();
			}

			Assert.That(OmniDebuggerViews.Current, Is.Not.SameAs(debugger));
		}

		private sealed class TitledInfo : IInfoProvider
		{
			public TitledInfo(string title) => Title = title;

			public string Title { get; }

			public int Order => 100;

			public void Describe(IInfoSection section) => section.Text("Key", "Value");
		}

		private sealed class CommandInfo : IInfoProvider
		{
			private readonly string _path;

			public string Title => "Commands";

			public int Order => 100;

			public CommandInfo(string path) => _path = path;

			public void Describe(IInfoSection section) => section.Command(_path);
		}
	}
}