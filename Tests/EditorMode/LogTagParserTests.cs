using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class LogTagParserTests
	{
		[Test]
		public void Parse_ReadsLeadingTagsOnly()
		{
			Assert.That(LogTagParser.Parse("  [Net] [Auth]failed [Later]"), Is.EqualTo(new[] { "Net", "Auth" }));
		}

		[Test]
		public void Parse_StopsAtAnUnclosedOrEmptyTag()
		{
			Assert.That(LogTagParser.Parse("[Net] [broken"), Is.EqualTo(new[] { "Net" }));
			Assert.That(LogTagParser.Parse("[] [Net]"), Is.Empty);
		}

		[Test]
		public void Parse_IgnoresATagOverTheLimitAndDuplicates()
		{
			string longTag = "[" + new string('t', LogTagParser.MaxTagLength + 1) + "] message";

			Assert.That(LogTagParser.Parse(longTag), Is.Empty);
			Assert.That(LogTagParser.Parse("[Net][Net] ping"), Is.EqualTo(new[] { "Net" }));
		}
	}
}
