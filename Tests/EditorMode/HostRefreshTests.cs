using System;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class HostRefreshTests
	{
		private OmniDebuggerHost _debugger;

		[SetUp]
		public void SetUp() => _debugger = new OmniDebuggerHost(new RecordingLogSink());

		[TearDown]
		public void TearDown() => _debugger.Dispose();

		[Test]
		public void Refresh_RaisesOnRefreshRequestedOncePerCall()
		{
			int raised = 0;
			_debugger.OnRefreshRequested += () => raised++;

			_debugger.Refresh();
			_debugger.Refresh();

			Assert.That(raised, Is.EqualTo(2));
		}

		[Test]
		public void Refresh_WithNoSubscribers_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _debugger.Refresh());
		}

		[Test]
		public void Refresh_AfterDispose_Throws()
		{
			_debugger.Dispose();

			Assert.Throws<ObjectDisposedException>(() => _debugger.Refresh());
		}
	}
}
