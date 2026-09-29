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
			OmniDebuggerHost.ReleaseShared();
			Assert.That(OmniDebuggerHost.TryGetShared(out _), Is.False, "a debugger from another test is still alive");
		}

		[TearDown]
		public void TearDown() => OmniDebuggerHost.ReleaseShared();

		[Test]
		public void Shared_BuildsOneDebuggerAndKeepsHandingItOut()
		{
			OmniDebuggerHost shared = OmniDebuggerHost.Shared;

			Assert.That(shared, Is.Not.Null);
			Assert.That(OmniDebuggerHost.Shared, Is.SameAs(shared));
			Assert.That(OmniDebuggerHost.TryGetShared(out OmniDebuggerHost found), Is.True);
			Assert.That(found, Is.SameAs(shared));
		}

		[Test]
		public void Constructor_TakesAnEmptySlotButNeverReplacesTheShared()
		{
			OmniDebuggerHost first = new OmniDebuggerHost(new RecordingLogSink());
			OmniDebuggerHost second = new OmniDebuggerHost(new RecordingLogSink());

			try
			{
				Assert.That(OmniDebuggerHost.Shared, Is.SameAs(first));
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
			OmniDebuggerHost first = OmniDebuggerHost.Shared;
			first.Dispose();

			Assert.That(OmniDebuggerHost.TryGetShared(out _), Is.False);
			Assert.That(OmniDebuggerHost.Shared, Is.Not.SameAs(first));
		}

		[Test]
		public void ReleaseShared_DisposesOnlyADebuggerThePackageBuilt()
		{
			OmniDebuggerHost own = new OmniDebuggerHost(new RecordingLogSink());

			try
			{
				OmniDebuggerHost.ReleaseShared();

				Assert.That(OmniDebuggerHost.TryGetShared(out OmniDebuggerHost found), Is.True);
				Assert.That(found, Is.SameAs(own));
				Assert.DoesNotThrow(() => _ = own.Catalog, "the game's own debugger stays alive");
			}
			finally
			{
				own.Dispose();
			}

			OmniDebuggerHost built = OmniDebuggerHost.Shared;
			OmniDebuggerHost.ReleaseShared();

			Assert.That(OmniDebuggerHost.TryGetShared(out _), Is.False);
			Assert.Throws<ObjectDisposedException>(() => _ = built.Catalog);
		}
	}
}
