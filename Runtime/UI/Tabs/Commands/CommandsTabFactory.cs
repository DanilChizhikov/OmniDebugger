namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandsTabFactory : IOmniDebuggerTabFactory
	{
		public const string TabId = "commands";

		public string Id => TabId;

		public string DisplayName => "Commands";

		public int Order => 0;

		public string Icon => "sliders";

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new CommandsTab(context);
	}
}