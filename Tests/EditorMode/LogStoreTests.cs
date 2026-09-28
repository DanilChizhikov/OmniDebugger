using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class LogStoreTests
	{
		private static readonly DateTime _time = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		[Test]
		public void Query_WithoutPaging_ReturnsTheNewestPageOldestFirst()
		{
			LogStore store = new LogStore();

			for (int i = 0; i < 5; i++)
			{
				store.Add($"message {i}", string.Empty, LogType.Log, _time);
			}

			List<LogRecord> page = new List<LogRecord>();
			bool hasOlder = store.Query(new LogQuery(limit: 3), page);

			Assert.That(hasOlder, Is.True);
			Assert.That(page.ConvertAll(record => record.Message), Is.EqualTo(new[] { "message 2", "message 3", "message 4" }));
		}

		[Test]
		public void Query_BeforeAndAfter_PageAroundAnId()
		{
			LogStore store = new LogStore();

			for (int i = 0; i < 6; i++)
			{
				store.Add($"message {i}", string.Empty, LogType.Log, _time);
			}

			List<LogRecord> newest = new List<LogRecord>();
			store.Query(new LogQuery(limit: 2), newest);

			List<LogRecord> older = new List<LogRecord>();
			bool moreOlder = store.Query(new LogQuery(limit: 2).Before(newest[0].Id), older);

			List<LogRecord> newer = new List<LogRecord>();
			bool moreNewer = store.Query(new LogQuery(limit: 10).After(older[0].Id), newer);

			Assert.That(older.ConvertAll(record => record.Message), Is.EqualTo(new[] { "message 2", "message 3" }));
			Assert.That(moreOlder, Is.True);
			Assert.That(newer.Count, Is.EqualTo(3));
			Assert.That(moreNewer, Is.False);
		}

		[Test]
		public void Add_BeyondCapacity_DropsTheOldestAndItsCounts()
		{
			LogStore store = new LogStore(capacity: 2);

			store.Add("[Net] first", string.Empty, LogType.Error, _time);
			store.Add("second", string.Empty, LogType.Log, _time);
			store.Add("third", string.Empty, LogType.Warning, _time);

			store.CountByType(out int logs, out int warnings, out int errors);
			List<string> tags = new List<string>();
			store.GetKnownTags(tags);

			Assert.That(store.Count, Is.EqualTo(2));
			Assert.That((logs, warnings, errors), Is.EqualTo((1, 1, 0)));
			Assert.That(tags, Is.Empty, "a tag only the dropped record carried must be forgotten");
			Assert.That(store.ErrorCount, Is.EqualTo(1), "the error count never goes down");
		}

		[Test]
		public void Query_FiltersByTypeTextAndTags()
		{
			LogStore store = new LogStore();
			store.Add("[Net] [Auth] login failed", string.Empty, LogType.Error, _time);
			store.Add("[Net] ping", string.Empty, LogType.Log, _time);
			store.Add("[Auth] token refreshed", "at Refresh()", LogType.Warning, _time);

			Assert.That(Messages(store, new LogQuery(types: LogTypeMask.Error)), Is.EqualTo(new[] { "[Net] [Auth] login failed" }));
			Assert.That(Messages(store, new LogQuery(text: "REFRESH()")), Is.EqualTo(new[] { "[Auth] token refreshed" }), "the stack trace is searched too");
			Assert.That(Messages(store, new LogQuery(tags: new[] { "Net", "Auth" })).Count, Is.EqualTo(1));
			Assert.That(Messages(store, new LogQuery(tags: new[] { "Net", "Auth" }, tagMode: LogTagMode.Any)).Count, Is.EqualTo(3));
		}

		[Test]
		public void Add_CutsLongTextAndFlagsIt()
		{
			LogStore store = new LogStore();
			store.Add(new string('x', LogStore.MaxMessageLength + 10), string.Empty, LogType.Log, _time);

			List<LogRecord> records = new List<LogRecord>();
			store.Query(default, records);

			Assert.That(records[0].Message.Length, Is.EqualTo(LogStore.MaxMessageLength));
			Assert.That(records[0].IsMessageTruncated, Is.True);
			Assert.That(records[0].IsStackTraceTruncated, Is.False);
		}

		[Test]
		public void Clear_EmptiesTheStoreButMovesTheVersion()
		{
			LogStore store = new LogStore();
			store.Add("message", string.Empty, LogType.Exception, _time);
			long version = store.Version;

			store.Clear();

			Assert.That(store.Count, Is.Zero);
			Assert.That(store.Version, Is.GreaterThan(version));
			Assert.That(store.ErrorCount, Is.EqualTo(1));
		}

		private static List<string> Messages(LogStore store, LogQuery query)
		{
			List<LogRecord> records = new List<LogRecord>();
			store.Query(query, records);
			return records.ConvertAll(record => record.Message);
		}
	}
}
