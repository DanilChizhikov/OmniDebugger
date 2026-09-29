namespace DTech.OmniDebugger.UI
{
	internal sealed class InfoTabFactory : IOmniDebuggerTabFactory, IBuiltInTabIcon
	{
		public const string TabId = "info";

		public string Id => TabId;

		public string DisplayName => "Info";

		public int Order => 0;

		public CommandIcon Icon => default;

		public IconGlyph Glyph => IconGlyph.Info;

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new InfoTab();
	}
}