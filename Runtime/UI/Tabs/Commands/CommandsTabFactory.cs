namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandsTabFactory : IOmniDebuggerTabFactory, IBuiltInTabIcon
	{
		public const string TabId = "commands";

		public string Id => TabId;

		public string DisplayName => "Commands";

		public int Order => 10;

		public CommandIcon Icon => default;

		public IconGlyph Glyph => IconGlyph.Sliders;

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new CommandsTab(context);
	}
}