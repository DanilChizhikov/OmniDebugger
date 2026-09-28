using DTech.OmniDebugger.UI;
using NUnit.Framework;
using UnityEngine;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class PanelStateTests
	{
		private OmniDebugger _debugger;

		[SetUp]
		public void SetUp() => _debugger = new OmniDebugger(new RecordingLogSink());

		[TearDown]
		public void TearDown() => _debugger.Dispose();

		[Test]
		public void CommandKeySet_KeepsInsertionOrder()
		{
			CommandKeySet favorites = new CommandKeySet();

			favorites.Add("Economy/Coins");
			favorites.Add("Logs/TestLog");
			favorites.Toggle("Economy/Coins");
			favorites.Toggle("Economy/Coins");

			Assert.That(favorites.Keys, Is.EqualTo(new[] { "Logs/TestLog", "Economy/Coins" }));
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
		public void ViewState_ClearForgetsFavoritesAndPins()
		{
			OmniDebuggerViewState state = new OmniDebuggerViewState();
			state.Favorites.Add("Economy/Coins");
			state.Pins.Add("Logs/TestLog");

			state.Clear();

			Assert.That(state.Favorites.Count, Is.Zero);
			Assert.That(state.Pins.Count, Is.Zero);
		}

		[Test]
		public void CommandKeySet_RaisesChangedOnlyForRealChanges()
		{
			CommandKeySet pins = new CommandKeySet();
			int changes = 0;
			pins.OnChanged += () => changes++;

			pins.Add("A/B");
			pins.Add("A/B");
			pins.Remove("missing");
			pins.Clear();
			pins.Clear();

			Assert.That(changes, Is.EqualTo(2));
		}

		[Test]
		public void Windows_RegisteringAnIdAgainReplacesTheWindow()
		{
			IOmniDebuggerWindow first = _debugger.Windows.RegisterCommands("test", "First", new[] { "A/B", "A/B", " " });
			IOmniDebuggerWindow second = _debugger.Windows.RegisterCustom("test", "Second", _ => { }, open: true);

			Assert.That(_debugger.Windows.All.Count, Is.EqualTo(1));
			Assert.That(_debugger.Windows.TryGet("test", out IOmniDebuggerWindow found), Is.True);
			Assert.That(found, Is.SameAs(second));
			Assert.That(first, Is.Not.SameAs(second));
			Assert.That(second.IsOpen, Is.True);
		}

		[Test]
		public void Windows_OpenCloseAndCollapseRaiseChanged()
		{
			IOmniDebuggerWindow window = _debugger.Windows.RegisterCustom("test", "Title", _ => { });
			int changes = 0;
			_debugger.Windows.OnChanged += Count;

			try
			{
				_debugger.Windows.Open("test");
				window.SetCollapsed(true);
				_debugger.Windows.CloseAll();
				_debugger.Windows.CloseAll();
			}
			finally
			{
				_debugger.Windows.OnChanged -= Count;
			}

			Assert.That(changes, Is.EqualTo(3));
			Assert.That(_debugger.Windows.Unregister("test"), Is.True);
			Assert.That(_debugger.Windows.Unregister("test"), Is.False);

			void Count() => changes++;
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
			Assert.That(PanelScaling.ResolveMatch(1920.0f, 1080.0f), Is.EqualTo(0.7f).Within(0.01f));
			Assert.That(PanelScaling.ResolveMatch(0.0f, 1080.0f), Is.Zero);
		}

		[Test]
		public void Options_DefaultToAPanelAndAVisibleButton()
		{
			OmniDebuggerOptions options = OmniDebuggerOptions.Default;

			Assert.That(options.CreatePanel, Is.True);
			Assert.That(options.Panel.ScaleMode, Is.EqualTo(OmniDebuggerScaleMode.Auto));
			Assert.That(options.Panel.Open.ShowButton, Is.True);
			Assert.That(options.Panel.Open.Shortcuts, Is.Empty);

			options.Panel.Scale = -1.0f;
			Assert.That(options.Panel.Scale, Is.EqualTo(1.0f));
		}

		[Test]
		public void Debugger_BuiltInEditMode_CreatesNoPanelButRegistersForTheEditorWindow()
		{
			OmniDebugger debugger = new OmniDebugger(new OmniDebuggerOptions());

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
	}
}