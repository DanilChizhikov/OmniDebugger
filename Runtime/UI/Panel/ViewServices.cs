namespace DTech.OmniDebugger.UI
{
	internal sealed class ViewServices
	{
		public IOmniDebuggerHost Debugger { get; }

		public string Origin { get; }

		public ArgumentMemory Arguments { get; }

		public CommandKeySet Favorites { get; }

		public CommandKeySet Pins { get; }

		public PopupLayer Popups { get; }

		public ViewServices(
			IOmniDebuggerHost debugger,
			string origin,
			ArgumentMemory arguments,
			CommandKeySet favorites,
			CommandKeySet pins,
			PopupLayer popups)
		{
			Debugger = debugger;
			Origin = origin;
			Arguments = arguments;
			Favorites = favorites;
			Pins = pins;
			Popups = popups;
		}
	}
}