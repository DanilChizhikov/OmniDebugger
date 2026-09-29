using System;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class SharedDebuggerTests
	{
		[SetUp]
		public void SetUp()
		{
			OmniDebugger.ReleaseShared();
			Assert.That(OmniDebugger.TryGetShared(out _), Is.False, "a debugger from another test is still alive");
		}

		[TearDown]
		public void TearDown() => OmniDebugger.ReleaseShared();

		[Test]
		public void Shared_BuildsOneDebuggerAndKeepsHandingItOut()
		{
			OmniDebugger shared = OmniDebugger.Shared;

			Assert.That(shared, Is.Not.Null);
			Assert.That(OmniDebugger.Shared, Is.SameAs(shared));
			Assert.That(OmniDebugger.TryGetShared(out OmniDebugger found), Is.True);
			Assert.That(found, Is.SameAs(shared));
		}

		[Test]
		public void Constructor_TakesAnEmptySlotButNeverReplacesTheShared()
		{
			OmniDebugger first = new OmniDebugger(new RecordingLogSink());
			OmniDebugger second = new OmniDebugger(new RecordingLogSink());

			try
			{
				Assert.That(OmniDebugger.Shared, Is.SameAs(first));
			}
			finally
			{
				second.Dispose();
				first.Dispose();
			}
		}

		[Test]
		public void Dispose_EmptiesTheSlotAndTheNextReadBuildsAFreshOne()
		{
			OmniDebugger first = OmniDebugger.Shared;
			first.Dispose();

			Assert.That(OmniDebugger.TryGetShared(out _), Is.False);
			Assert.That(OmniDebugger.Shared, Is.Not.SameAs(first));
		}

		[Test]
		public void ReleaseShared_DisposesOnlyADebuggerThePackageBuilt()
		{
			OmniDebugger own = new OmniDebugger(new RecordingLogSink());

			try
			{
				OmniDebugger.ReleaseShared();

				Assert.That(OmniDebugger.TryGetShared(out OmniDebugger found), Is.True);
				Assert.That(found, Is.SameAs(own));
				Assert.DoesNotThrow(() => _ = own.Catalog, "the game's own debugger stays alive");
			}
			finally
			{
				own.Dispose();
			}

			OmniDebugger built = OmniDebugger.Shared;
			OmniDebugger.ReleaseShared();

			Assert.That(OmniDebugger.TryGetShared(out _), Is.False);
			Assert.Throws<ObjectDisposedException>(() => _ = built.Catalog);
		}
	}
}
