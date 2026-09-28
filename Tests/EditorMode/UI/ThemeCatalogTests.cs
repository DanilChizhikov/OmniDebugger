using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using NUnit.Framework;
using UnityEngine;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ThemeCatalogTests
	{
		private RecordingLogSink _log;
		private ThemeCatalog _catalog;

		[SetUp]
		public void SetUp()
		{
			_log = new RecordingLogSink();
			_catalog = new ThemeCatalog(_log);
		}

		[TearDown]
		public void TearDown() => _catalog.Clear();

		[Test]
		public void All_OffersTheTwoBuiltInThemesWithoutAnyAssets()
		{
			IReadOnlyList<OmniDebuggerTheme> themes = _catalog.All;

			Assert.That(Ids(themes), Contains.Item(ThemeCatalog.DarkThemeId));
			Assert.That(Ids(themes), Contains.Item(ThemeCatalog.LightThemeId));
		}

		[Test]
		public void Default_IsTheDarkThemeAndNeverNull()
		{
			OmniDebuggerTheme theme = _catalog.Default;

			Assert.That(theme, Is.Not.Null);
			Assert.That(theme.Id, Is.EqualTo(ThemeCatalog.DarkThemeId));
		}

		[Test]
		public void TryGet_FindsARegisteredThemeAndRejectsAnUnknownId()
		{
			OmniDebuggerTheme theme = Theme("ocean", "Ocean");

			try
			{
				Assert.That(_catalog.Register(theme), Is.True);
				Assert.That(_catalog.TryGet("ocean", out OmniDebuggerTheme found), Is.True);
				Assert.That(found, Is.SameAs(theme));
				Assert.That(_catalog.TryGet("nope", out _), Is.False);
				Assert.That(_catalog.TryGet(null, out _), Is.False, "a missing selection is not an error");
			}
			finally
			{
				Object.DestroyImmediate(theme);
			}
		}

		[Test]
		public void Register_TheSameThemeTwiceChangesNothing()
		{
			OmniDebuggerTheme theme = Theme("ocean", "Ocean");

			try
			{
				Assert.That(_catalog.Register(theme), Is.True);
				Assert.That(_catalog.Register(theme), Is.False);
				Assert.That(_catalog.Unregister(theme), Is.True);
				Assert.That(_catalog.Unregister(theme), Is.False);
			}
			finally
			{
				Object.DestroyImmediate(theme);
			}
		}

		[Test]
		public void Register_AThemeWithABuiltInIdReplacesItSilently()
		{
			OmniDebuggerTheme theme = Theme(ThemeCatalog.DarkThemeId, "My Dark");

			try
			{
				_catalog.Register(theme);

				Assert.That(_catalog.TryGet(ThemeCatalog.DarkThemeId, out OmniDebuggerTheme found), Is.True);
				Assert.That(found, Is.SameAs(theme), "shipping your own dark theme is a replacement");
				Assert.That(_log.Warnings, Is.Empty, "replacing a built-in is not a duplicate-id mistake");
			}
			finally
			{
				Object.DestroyImmediate(theme);
			}
		}

		[Test]
		public void Register_TwoThemesSharingAnIdKeepsTheFirstAndWarns()
		{
			OmniDebuggerTheme first = Theme("ocean", "Ocean");
			OmniDebuggerTheme second = Theme("ocean", "Ocean Too");

			try
			{
				_catalog.Register(first);
				_catalog.Register(second);

				Assert.That(_catalog.TryGet("ocean", out OmniDebuggerTheme found), Is.True);
				Assert.That(found, Is.SameAs(first));
				Assert.That(_log.Warnings, Has.Some.Contains("share an id"));
			}
			finally
			{
				Object.DestroyImmediate(first);
				Object.DestroyImmediate(second);
			}
		}

		[Test]
		public void All_IsSortedBySortOrderAndThenByDisplayName()
		{
			OmniDebuggerTheme late = Theme("late", "Aaa", sortOrder: 500);
			OmniDebuggerTheme early = Theme("early", "Zzz", sortOrder: 1);

			try
			{
				_catalog.Register(late);
				_catalog.Register(early);

				List<string> ids = new List<string>(Ids(_catalog.All));

				Assert.That(ids.IndexOf("early"), Is.LessThan(ids.IndexOf("late")),
					"sort order wins over the name");
				Assert.That(ids.IndexOf("late"), Is.GreaterThan(ids.IndexOf(ThemeCatalog.DarkThemeId)),
					"the built-ins sit at order zero");
			}
			finally
			{
				Object.DestroyImmediate(late);
				Object.DestroyImmediate(early);
			}
		}

		private static OmniDebuggerTheme Theme(string id, string displayName, int sortOrder = 100) =>
			OmniDebuggerTheme.CreateBuiltIn(id, displayName, sortOrder, null);

		private static string[] Ids(IReadOnlyList<OmniDebuggerTheme> themes)
		{
			string[] ids = new string[themes.Count];

			for (int i = 0; i < themes.Count; i++)
			{
				ids[i] = themes[i].Id;
			}

			return ids;
		}
	}
}
