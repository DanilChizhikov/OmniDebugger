using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ProjectOptionsTests
	{
		private readonly List<Object> _created = new ();

		private System.Func<OmniDebuggerOptions> _previousSource;

		[SetUp]
		public void SetUp() => _previousSource = ProjectOptions.EditorSource;

		[TearDown]
		public void TearDown()
		{
			ProjectOptions.EditorSource = _previousSource;

			for (int i = 0; i < _created.Count; i++)
			{
				Object.DestroyImmediate(_created[i]);
			}

			_created.Clear();
		}

		[Test]
		public void Clone_CopiesEveryValue()
		{
			OmniDebuggerOptions source = Sample();
			OmniDebuggerOptions copy = source.Clone();

			Assert.That(copy, Is.Not.SameAs(source));
			Assert.That(copy.CreateOnStartup, Is.True);
			Assert.That(copy.CreatePanel, Is.False);
			Assert.That(copy.DefaultTheme, Is.SameAs(source.DefaultTheme));
			Assert.That(copy.Themes, Is.EqualTo(source.Themes));

			Assert.That(copy.Panel.ScaleMode, Is.EqualTo(OmniDebuggerScaleMode.PhysicalSize));
			Assert.That(copy.Panel.Scale, Is.EqualTo(1.5f));
			Assert.That(copy.Panel.SortingOrder, Is.EqualTo(42.0f));
			Assert.That(copy.Panel.OpenOnStart, Is.True);
			Assert.That(copy.Panel.PanelSettings, Is.SameAs(source.Panel.PanelSettings));
			Assert.That(copy.Panel.LandscapeLayout, Is.EqualTo(OmniDebuggerLandscapeLayout.FullScreen));
			Assert.That(copy.Panel.FloatingScale, Is.EqualTo(0.8f));

			Assert.That(copy.Panel.Open.ButtonEnabled, Is.False);
			Assert.That(copy.Panel.Open.ButtonClicks, Is.EqualTo(3));
			Assert.That(copy.Panel.Open.MultiClickWindow, Is.EqualTo(0.25f));
			Assert.That(copy.Panel.Open.ButtonAnchor, Is.EqualTo(OpenButtonAnchor.TopRight));
			Assert.That(copy.Panel.Open.ButtonOpacity, Is.EqualTo(0.7f));
			Assert.That(copy.Panel.Open.Shortcuts, Has.Count.EqualTo(1));
			Assert.That(copy.Panel.Open.Shortcuts[0].Keys, Is.EqualTo(new[] { KeyCode.LeftControl, KeyCode.F1 }));
		}

		[Test]
		public void Clone_SharesNoListOrShortcutWithTheSource()
		{
			OmniDebuggerOptions source = Sample();
			OmniDebuggerOptions copy = source.Clone();

			Assert.That(copy.Panel, Is.Not.SameAs(source.Panel));
			Assert.That(copy.Panel.Open, Is.Not.SameAs(source.Panel.Open));
			Assert.That(copy.Panel.Open.Shortcuts[0], Is.Not.SameAs(source.Panel.Open.Shortcuts[0]));

			copy.Themes.Clear();
			copy.Panel.Open.Shortcuts.Clear();

			Assert.That(source.Themes, Has.Count.EqualTo(1));
			Assert.That(source.Panel.Open.Shortcuts, Has.Count.EqualTo(1));
		}

		[Test]
		public void Default_ReadsTheEditorSourceAndHandsOutACopy()
		{
			OmniDebuggerOptions source = Sample();
			ProjectOptions.EditorSource = () => source;

			OmniDebuggerOptions first = OmniDebuggerOptions.Default;
			first.Panel.Open.ButtonClicks = 7;

			Assert.That(first, Is.Not.SameAs(source));
			Assert.That(OmniDebuggerOptions.Default.Panel.Open.ButtonClicks, Is.EqualTo(3),
				"changing a copy leaves the project settings alone");
		}

		[Test]
		public void Default_WithoutASourceIsTheBuiltInDefaults()
		{
			ProjectOptions.EditorSource = null;

			OmniDebuggerOptions options = OmniDebuggerOptions.Default;

			Assert.That(options.CreatePanel, Is.True);
			Assert.That(options.Panel.Open.ButtonClicks, Is.EqualTo(1));
			Assert.That(options.Themes, Is.Empty);
		}

		[Test]
		public void Debugger_RegistersTheThemesAndTheDefaultThemeItIsGiven()
		{
			OmniDebuggerOptions options = Sample();
			options.Themes.Add(null);

			OmniDebuggerHost debugger = new OmniDebuggerHost(new RecordingLogSink(), options);

			try
			{
				Assert.That(debugger.Themes.TryGet("ocean", out OmniDebuggerTheme listed), Is.True);
				Assert.That(listed, Is.SameAs(options.Themes[0]));
				Assert.That(debugger.Themes.Default, Is.SameAs(options.DefaultTheme));
			}
			finally
			{
				debugger.Dispose();
			}
		}

		[Test]
		public void Debugger_BuiltWithoutOptionsFollowsProjectEdits()
		{
			OmniDebuggerOptions source = Sample();
			ProjectOptions.EditorSource = () => source;

			OmniDebuggerHost debugger = new OmniDebuggerHost(new RecordingLogSink(), null);

			try
			{
				Assert.That(debugger.Themes.TryGet("ocean", out _), Is.True);
				Assert.That(debugger.Themes.Default.Id, Is.EqualTo("sunset"));

				OmniDebuggerTheme reef = Track(Theme("reef"));
				source.Themes.Clear();
				source.Themes.Add(reef);
				source.DefaultTheme = null;
				ProjectOptions.NotifyEditorChanged();

				Assert.That(debugger.Themes.TryGet("ocean", out _), Is.False, "a theme dropped from the settings goes");
				Assert.That(debugger.Themes.TryGet("sunset", out _), Is.False, "so does the former default");
				Assert.That(debugger.Themes.TryGet("reef", out OmniDebuggerTheme added), Is.True);
				Assert.That(added, Is.SameAs(reef));
				Assert.That(debugger.Themes.Default.Id, Is.EqualTo(ThemeCatalog.DarkThemeId));
			}
			finally
			{
				debugger.Dispose();
			}

			Assert.DoesNotThrow(ProjectOptions.NotifyEditorChanged, "a disposed debugger stops listening");
		}

		[Test]
		public void Debugger_GivenOptionsIgnoresProjectEdits()
		{
			OmniDebuggerOptions source = Sample();
			ProjectOptions.EditorSource = () => source;

			OmniDebuggerHost debugger = new OmniDebuggerHost(new RecordingLogSink(), Sample());

			try
			{
				source.Themes.Add(Track(Theme("reef")));
				ProjectOptions.NotifyEditorChanged();

				Assert.That(debugger.Themes.TryGet("reef", out _), Is.False);
			}
			finally
			{
				debugger.Dispose();
			}
		}

		private static OmniDebuggerTheme Theme(string id) => OmniDebuggerTheme.Create(id, id, 100, null);

		private T Track<T>(T created) where T : Object
		{
			_created.Add(created);
			return created;
		}

		private OmniDebuggerOptions Sample()
		{
			OmniDebuggerOptions options = new OmniDebuggerOptions
			{
				CreateOnStartup = true,
				CreatePanel = false,
				DefaultTheme = Track(Theme("sunset")),
			};

			options.Themes.Add(Track(Theme("ocean")));

			options.Panel.ScaleMode = OmniDebuggerScaleMode.PhysicalSize;
			options.Panel.Scale = 1.5f;
			options.Panel.SortingOrder = 42.0f;
			options.Panel.OpenOnStart = true;
			options.Panel.PanelSettings = Track(ScriptableObject.CreateInstance<PanelSettings>());
			options.Panel.LandscapeLayout = OmniDebuggerLandscapeLayout.FullScreen;
			options.Panel.FloatingScale = 0.8f;

			options.Panel.Open.ButtonEnabled = false;
			options.Panel.Open.ButtonClicks = 3;
			options.Panel.Open.MultiClickWindow = 0.25f;
			options.Panel.Open.ButtonAnchor = OpenButtonAnchor.TopRight;
			options.Panel.Open.ButtonOpacity = 0.7f;
			options.Panel.Open.Shortcuts.Add(new OmniDebuggerShortcut(KeyCode.LeftControl, KeyCode.F1));

			return options;
		}
	}
}
