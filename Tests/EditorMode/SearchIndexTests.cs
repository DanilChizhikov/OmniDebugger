using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class SearchIndexTests
	{
		private OmniDebugger _debugger;
		private SearchIndex<CommandDefinition> _index;

		[SetUp]
		public void SetUp()
		{
			_debugger = new OmniDebugger(new RecordingLogSink());
			_debugger.Catalog.AddSource(new SampleCommands());

			_index = new SearchIndex<CommandDefinition>();
			_index.Rebuild(_debugger.Catalog.Commands);
		}

		[TearDown]
		public void TearDown()
		{
			_index.Dispose();
			_debugger.Dispose();
		}

		[Test]
		public void Query_FindsACommandByItsName()
		{
			Assert.That(Search("coins"), Is.EqualTo(new[] { SampleCommands.AddCoinsKey }));
		}

		[Test]
		public void Query_FindsNothingForAnEmptyTerm()
		{
			SearchSession<CommandDefinition> session = _index.BeginQuery("   ", default);

			Assert.That(session.IsComplete, Is.True);
			Assert.That(session.Hits, Is.Empty);
		}

		[Test]
		public void Query_FindsNothingForPunctuationOnly()
		{
			SearchSession<CommandDefinition> session = _index.BeginQuery("--- ???", default);

			Assert.That(session.IsComplete, Is.True);
			Assert.That(session.Hits, Is.Empty);
		}

		[Test]
		public void Query_FindsEveryCommandByItsGroupName()
		{
			Assert.That(Search(SampleCommands.Group), Has.Count.EqualTo(SampleCommands.ExpectedCommandCount));
		}

		[Test]
		public void Query_FindsACommandByADeclaredTag()
		{
			Assert.That(Search("wallet"), Is.EqualTo(new[] { SampleCommands.AddCoinsKey }));
		}

		[Test]
		public void Query_FindsACommandByOneWordOfItsName()
		{
			Assert.That(Search("version"), Contains.Item(SampleCommands.BuildVersionKey));
		}

		[Test]
		public void Query_MatchesAMultiWordNameTypedWithoutTheSpace()
		{
			Assert.That(Search("addcoins"), Is.EqualTo(new[] { SampleCommands.AddCoinsKey }));
		}

		[Test]
		public void Query_MatchesTheInitialsOfAMultiWordName()
		{
			Assert.That(Search("ac"), Contains.Item(SampleCommands.AddCoinsKey));
		}

		[Test]
		public void Query_NarrowsRatherThanWidensWithEveryExtraWord()
		{
			Assert.That(Search("economy coins"), Is.EqualTo(new[] { SampleCommands.AddCoinsKey }));
		}

		[Test]
		public void Query_FindsNothingWhenOnlyOneOfTwoWordsMatches()
		{
			Assert.That(Search("economy godmode"), Is.Empty);
		}

		[Test]
		public void Query_StillFindsACommandWhenTheTermIsMistyped()
		{
			Assert.That(Search("coibs"), Contains.Item(SampleCommands.AddCoinsKey));
		}

		[Test]
		public void Query_StillFindsACommandWhenTheTermIsCutShort()
		{
			Assert.That(Search("versio"), Contains.Item(SampleCommands.BuildVersionKey));
		}

		[Test]
		public void Query_NeverReturnsTheSameItemTwiceAcrossStages()
		{
			Assert.That(Search("sett"), Is.Unique);
		}

		[Test]
		public void Query_RanksAWholeWordAboveAPrefix()
		{
			List<string> found = Search("set");

			Assert.That(found, Contains.Item(SampleCommands.SetSpeedKey));
			Assert.That(found, Contains.Item(SampleCommands.PrivateSetterKey));
			Assert.That(
				found.IndexOf(SampleCommands.PrivateSetterKey),
				Is.GreaterThan(found.IndexOf(SampleCommands.SetSpeedKey)),
				"'Set Speed' owns the word, 'Private Setter' only starts with it");
		}

		[Test]
		public void Query_HasItsCheapestResultsBeforeItFinishes()
		{
			SearchSession<CommandDefinition> session = _index.BeginQuery("set", default);

			while (session.Stage == SearchStage.Exact)
			{
				session.Advance(0);
			}

			Assert.That(session.IsComplete, Is.False, "prefix, infix and fuzzy are still outstanding");
			Assert.That(
				KeysOf(session),
				Is.EquivalentTo(new[] { SampleCommands.SetSpeedKey, SampleCommands.SetSeverityKey }));
		}

		[Test]
		public void Query_ReplacesTheResultsOfThePreviousQuery()
		{
			Assert.That(Search("god"), Contains.Item(SampleCommands.GodModeKey));

			List<string> found = Search("coins");

			Assert.That(found, Contains.Item(SampleCommands.AddCoinsKey));
			Assert.That(found, Has.None.EqualTo(SampleCommands.GodModeKey));
		}

		[Test]
		public void Query_KeepsOnlyTheBestResultsWhenCapped()
		{
			SearchSession<CommandDefinition> session = _index.BeginQuery(
				SampleCommands.Group,
				new QuerySettings(2));

			Drain(session);

			Assert.That(session.Hits, Has.Count.EqualTo(2));
		}

		[Test]
		public void Query_HonoursTheCaseSensitiveOption()
		{
			QuerySettings sensitive = new QuerySettings(SearchOptions.CaseSensitive);

			Assert.That(Search("COINS", sensitive), Is.Empty);
			Assert.That(Search("Coins", sensitive), Contains.Item(SampleCommands.AddCoinsKey));
		}

		[Test]
		public void Query_SkipsFuzzyWhenAskedTo()
		{
			QuerySettings strict = new QuerySettings(SearchOptions.NoFuzzy);

			Assert.That(Search("coibs", strict), Is.Empty);
			Assert.That(Search("coins", strict), Contains.Item(SampleCommands.AddCoinsKey));
		}

		[Test]
		public void Add_MakesAnItemFindableWithoutARebuild()
		{
			using SearchIndex<Phrase> index = new SearchIndex<Phrase>();
			index.Rebuild(new[] { new Phrase("Reload Scene") });

			int id = index.Add(new Phrase("Teleport Player"));

			SearchSession<Phrase> session = index.BeginQuery("teleport", default);
			Drain(session);

			Assert.That(session.Hits, Has.Count.EqualTo(1));
			Assert.That(session.Hits[0].Id, Is.EqualTo(id));
		}

		[Test]
		public void Query_SplitsCamelCaseSoAWordInsideARunTogetherNameMatches()
		{
			using SearchIndex<Phrase> index = new SearchIndex<Phrase>();
			index.Rebuild(new[] { new Phrase("ReloadCurrentScene") });

			SearchSession<Phrase> session = index.BeginQuery("current", default);
			Drain(session);

			Assert.That(session.Hits, Has.Count.EqualTo(1));
		}

		[Test]
		public void Remove_StopsAnItemBeingFound()
		{
			List<string> before = Search("coins");
			Assert.That(before, Is.Not.Empty);

			int id = FirstIdOf("coins");
			Assert.That(_index.Remove(id), Is.True);

			Assert.That(Search("coins"), Is.Empty);
			Assert.That(_index.Remove(id), Is.False, "removing twice does nothing");
		}

		[Test]
		public void Rebuild_ReindexesSoRemovedCommandsStopMatching()
		{
			_index.Rebuild(Array.Empty<CommandDefinition>());

			Assert.That(Search("coins"), Is.Empty);
		}

		[Test]
		public void Rebuild_CanRunRepeatedlyWithoutLeavingDuplicates()
		{
			for (int i = 0; i < 5; i++)
			{
				_index.Rebuild(_debugger.Catalog.Commands);
			}

			Assert.That(Search("coins"), Is.EqualTo(new[] { SampleCommands.AddCoinsKey }));
		}

		[Test]
		public void Rebuild_RejectsNull() =>
			Assert.Throws<ArgumentNullException>(() => _index.Rebuild(null));

		[Test]
		public void Dispose_StopsTheIndexBeingUsedAgain()
		{
			_index.Dispose();

			Assert.Throws<ObjectDisposedException>(() => _index.BeginQuery("coins", default));
			Assert.Throws<ObjectDisposedException>(() => _index.Rebuild(Array.Empty<CommandDefinition>()));
		}

		[Test]
		public void Dispose_IsIdempotent()
		{
			_index.Dispose();
			Assert.DoesNotThrow(() => _index.Dispose());
		}

		[Test]
		public void Query_SlicesItsWorkOverALargeSet()
		{
			const int itemCount = 50_000;

			List<Phrase> items = new List<Phrase>(itemCount);
			for (int i = 0; i < itemCount; i++)
			{
				items.Add(new Phrase($"Command {i} {Zone(i % 100)}"));
			}

			using SearchIndex<Phrase> index = new SearchIndex<Phrase>();
			index.Rebuild(items);

			SearchSession<Phrase> session = index.BeginQuery(Zone(28), default);

			bool sliced = false;
			while (session.Advance(0))
			{
				sliced = true;
			}

			Assert.That(sliced, Is.True, "a set this size cannot finish in one unit of work");
			Assert.That(session.Hits, Is.Not.Empty);
			Assert.That(index[session.Hits[0].Id].Text, Does.Contain(Zone(28)));
		}
		
		private static string Zone(int value) =>
			$"zon{(char)('a' + value / 26)}{(char)('a' + value % 26)}";

		private int FirstIdOf(string term)
		{
			SearchSession<CommandDefinition> session = _index.BeginQuery(term, default);
			Drain(session);

			return session.Hits[0].Id;
		}

		private List<string> Search(string term) => Search(term, default);

		private List<string> Search(string term, in QuerySettings settings)
		{
			SearchSession<CommandDefinition> session = _index.BeginQuery(term, settings);
			Drain(session);

			return KeysOf(session);
		}

		private List<string> KeysOf(SearchSession<CommandDefinition> session)
		{
			List<string> keys = new List<string>(session.Hits.Count);

			for (int i = 0; i < session.Hits.Count; i++)
			{
				keys.Add(_index[session.Hits[i].Id].Key);
			}

			return keys;
		}

		private static void Drain<T>(SearchSession<T> session)
			where T : class, ISearchIndexable
		{
			while (session.Advance(long.MaxValue))
			{
			}
		}

		private sealed class Phrase : ISearchIndexable
		{
			public string Text { get; }

			public Phrase(string text)
			{
				Text = text;
			}

			public void CollectIndexTerms(ICollection<string> terms) => terms.Add(Text);
		}
	}
}