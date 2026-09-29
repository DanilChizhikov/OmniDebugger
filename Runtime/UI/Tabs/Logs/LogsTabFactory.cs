namespace DTech.OmniDebugger.UI
{
	internal sealed class LogsTabFactory : IOmniDebuggerTabFactory, IBuiltInTabIcon
	{
		private const string TabId = "logs";

		public string Id => TabId;

		public string DisplayName => "Logs";

		public int Order => 30;

		public CommandIcon Icon => default;

		public IconGlyph Glyph => IconGlyph.Terminal;

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new LogsTab(context);
	}
}