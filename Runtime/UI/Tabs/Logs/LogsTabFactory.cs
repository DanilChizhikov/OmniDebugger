namespace DTech.OmniDebugger.UI
{
	internal sealed class LogsTabFactory : IOmniDebuggerTabFactory
	{
		private const string TabId = "logs";

		public string Id => TabId;
		public string DisplayName => "Logs";
		public int Order => 30;

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new LogsTab(context);
	}
}