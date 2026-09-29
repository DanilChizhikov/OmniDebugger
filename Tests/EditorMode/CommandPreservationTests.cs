using DTech.OmniDebugger.Editor;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class CommandPreservationTests
	{
		[Test]
		public void Collect_KeepsTypesWithCommandsAndTheEnumsTheirSignaturesUse()
		{
			CommandPreservation preservation = CommandPreservation.Collect(new[] { typeof(SampleCommands), typeof(NoCommands) });
			string xml = preservation.ToXml();

			Assert.That(preservation.TypeCount, Is.EqualTo(2));
			Assert.That(preservation.AssemblyCount, Is.EqualTo(1));
			Assert.That(xml, Does.Contain($"<type fullname=\"{typeof(SampleCommands).FullName}\" preserve=\"all\" />"));
			Assert.That(xml, Does.Contain($"<type fullname=\"{typeof(SampleSeverity).FullName}\" preserve=\"all\" />"));
			Assert.That(xml, Does.Not.Contain(nameof(NoCommands)));
		}

		[Test]
		public void Collect_SkipsATypeWhoseCommandsAreAllRejected()
		{
			CommandPreservation preservation = CommandPreservation.Collect(new[] { typeof(OnlyRejected) });

			Assert.That(preservation.IsEmpty, Is.True);
			Assert.That(preservation.ToXml(), Does.Not.Contain("<assembly"));
		}

		[Test]
		public void ToXml_WritesNestedTypesWithASlashAndIgnoresTheInputOrder()
		{
			string first = CommandPreservation.Collect(new[] { typeof(Outer.Inner), typeof(SampleCommands) }).ToXml();
			string second = CommandPreservation.Collect(new[] { typeof(SampleCommands), typeof(Outer.Inner) }).ToXml();

			Assert.That(first, Does.Contain($"{nameof(CommandPreservationTests)}/{nameof(Outer)}/{nameof(Outer.Inner)}\""));
			Assert.That(first, Is.EqualTo(second));
		}

		private sealed class NoCommands
		{
			public void Run()
			{
			}
		}

		private sealed class OnlyRejected
		{
			[DebugCommand("Tests", "Static")]
			public static void Static()
			{
			}

			[DebugCommand("Tests", "Hidden")]
			private void Hidden()
			{
			}
		}

		private sealed class Outer
		{
			public sealed class Inner
			{
				[DebugCommand("Tests", "Nested")]
				public void Run()
				{
				}
			}
		}
	}
}
