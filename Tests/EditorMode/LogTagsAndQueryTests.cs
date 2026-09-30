using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class LogTagsAndQueryTests
	{
		[Test]
		public void ParseTags_ReadsLeadingTagsOnly()
		{
			Assert.That(Tags("  [Net] [Auth]failed [Later]"), Is.EqualTo(new[] { "Net", "Auth" }));
		}

		[Test]
		public void ParseTags_StopsAtAnUnclosedOrEmptyTag()
		{
			Assert.That(Tags("[Net] [broken"), Is.EqualTo(new[] { "Net" }));
			Assert.That(Tags("[] [Net]"), Is.Empty);
		}

		[Test]
		public void ParseTags_IgnoresATagOverTheLimitAndDuplicates()
		{
			string longTag = "[" + new string('t', LogStore.MaxTagLength + 1) + "] message";

			Assert.That(Tags(longTag), Is.Empty);
			Assert.That(Tags("[Net][net] ping"), Is.EqualTo(new[] { "Net" }), "duplicates ignore case");
		}

		[Test]
		public void ParseTags_ReadsTheUnityLoggerFormat()
		{
			Assert.That(Tags("Net: connected"), Is.EqualTo(new[] { "Net" }));
			Assert.That(Tags("Game.Loop: tick"), Is.EqualTo(new[] { "Game.Loop" }));
			Assert.That(Tags("[Net] Auth: failed"), Is.EqualTo(new[] { "Net" }), "bracket tags win");
		}

		[Test]
		public void ParseTags_LeavesTextThatOnlyLooksLikeTheLoggerFormatAlone()
		{
			Assert.That(Tags("NullReferenceException: Object reference", LogType.Exception), Is.Empty,
				"an exception's message starts with its type, which is not a tag");
			Assert.That(Tags("two words: text"), Is.Empty);
			Assert.That(Tags("Time:12"), Is.Empty, "the colon has to be followed by a space");
			Assert.That(Tags("1st: place"), Is.Empty);
		}

		[Test]
		public void Parse_SplitsTheFilterSyntaxIntoItsParts()
		{
			LogQuery query = LogQuery.Parse("tag:Net -tag:Ads type:error type:warning timeout \"no route\"", limit: 7);

			Assert.That(query.Tags, Is.EquivalentTo(new[] { "Net" }));
			Assert.That(query.ExcludedTags, Is.EquivalentTo(new[] { "Ads" }));
			Assert.That(query.Types, Is.EqualTo(LogTypeMask.Error | LogTypeMask.Warning));
			Assert.That(query.Text, Is.EqualTo("timeout \"no route\""));
			Assert.That(query.Limit, Is.EqualTo(7));
		}

		[Test]
		public void Parse_ReadsNegatedTypesQuotedValuesAndUnknownKeysAsText()
		{
			LogQuery query = LogQuery.Parse("-type:log tag:\"Game Loop\" type:bogus url:http");

			Assert.That(query.Types, Is.EqualTo(LogTypeMask.Warning | LogTypeMask.Error));
			Assert.That(query.Tags, Is.EquivalentTo(new[] { "Game Loop" }));
			Assert.That(query.Text, Is.EqualTo("type:bogus url:http"));
		}

		[Test]
		public void Parse_OfNothingMatchesEverything()
		{
			LogQuery query = LogQuery.Parse("   ");

			Assert.That(query.Text, Is.Null);
			Assert.That(query.Tags, Is.Null);
			Assert.That(query.ExcludedTags, Is.Null);
			Assert.That(query.Types, Is.EqualTo(LogTypeMask.All));
		}

		[Test]
		public void Parse_FiltersAStoreTheWayTheLogsTabDoes()
		{
			LogStore store = new LogStore();
			store.Add("[Net] timeout on login", string.Empty, LogType.Error, default);
			store.Add("[Net] ping", string.Empty, LogType.Log, default);
			store.Add("[Ads] timeout", string.Empty, LogType.Error, default);

			List<LogRecord> records = new List<LogRecord>();
			store.Query(LogQuery.Parse("type:error -tag:ads timeout"), records);

			Assert.That(records.Select(record => record.Message), Is.EqualTo(new[] { "[Net] timeout on login" }));
		}

		private static IReadOnlyList<string> Tags(string message, LogType type = LogType.Log) =>
			LogStore.ParseTags(message, type);
	}
}
