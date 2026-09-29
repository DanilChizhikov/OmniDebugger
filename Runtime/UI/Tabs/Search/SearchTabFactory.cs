namespace DTech.OmniDebugger.UI
{
	internal sealed class SearchTabFactory : IOmniDebuggerTabFactory, IBuiltInTabIcon
	{
		private const string TabId = "search";

		public string Id => TabId;

		public string DisplayName => "Search";

		public int Order => 20;

		public CommandIcon Icon => default;

		public IconGlyph Glyph => IconGlyph.Search;

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new SearchTab(context);
	}
}