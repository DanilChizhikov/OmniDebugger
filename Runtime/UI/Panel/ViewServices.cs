namespace DTech.OmniDebugger.UI
{
	internal sealed class ViewServices
	{
		public IOmniDebuggerHost Debugger { get; }

		public string Origin { get; }

		public CommandStateStore Commands { get; }

		public PopupLayer Popups { get; }

		public bool HostsOverlays { get; }

		public ViewServices(
			IOmniDebuggerHost debugger,
			string origin,
			CommandStateStore commands,
			PopupLayer popups,
			bool hostsOverlays = false)
		{
			Debugger = debugger;
			Origin = origin;
			Commands = commands;
			Popups = popups;
			HostsOverlays = hostsOverlays;
		}
	}
}
