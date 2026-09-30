namespace DTech.OmniDebugger.UI
{
	internal sealed class InfoTabFactory : IOmniDebuggerTabFactory
	{
		public const string TabId = "info";

		public string Id => TabId;

		public string DisplayName => "Info";

		public int Order => 20;

		public string Icon => "info";

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new InfoTab(context.Debugger.Info, context.Services);
	}
}