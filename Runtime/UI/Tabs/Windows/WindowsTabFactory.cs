namespace DTech.OmniDebugger.UI
{
	internal sealed class WindowsTabFactory : IOmniDebuggerTabFactory, IBuiltInTabIcon
	{
		private const string TabId = "windows";

		public string Id => TabId;

		public string DisplayName => "Windows";

		public int Order => 40;

		public CommandIcon Icon => default;

		public IconGlyph Glyph => IconGlyph.Window;

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new WindowsTab(context.Debugger.Windows);
	}
}