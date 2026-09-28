namespace DTech.OmniDebugger.UI
{
	internal sealed class SearchTabFactory : IOmniDebuggerTabFactory
	{
		private const string TabId = "search";

		public string Id => TabId;
		public string DisplayName => "Search";
		public int Order => 20;

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new SearchTab(context);
	}
}