namespace DTech.OmniDebugger.UI
{
	internal sealed class WindowsTabFactory : IOmniDebuggerTabFactory
	{
		private const string TabId = "windows";

		public string Id => TabId;
		public string DisplayName => "Windows";
		public int Order => 40;

		public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new WindowsTab(context.Debugger.Windows);
	}
}