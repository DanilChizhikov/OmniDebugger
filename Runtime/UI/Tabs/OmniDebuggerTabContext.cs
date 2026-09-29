namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// What a tab is handed when it is built. Everything a tab needs comes from here, so a tab never
	/// reaches for a singleton and can be built twice — once per mount — without interference.
	/// </summary>
	public readonly struct OmniDebuggerTabContext
	{
		/// <summary>The debugger whose commands are on show. Never null, never disposed by a tab.</summary>
		public IOmniDebuggerHost Debugger { get; }

		/// <summary>
		/// The tab's own scratch pad, kept by the panel's owner. Anything a tab wants to survive a
		/// rebuild goes here rather than into a field.
		/// </summary>
		public OmniDebuggerTabState State { get; }

		/// <summary>
		/// What to pass as <see cref="InvocationRequest.Origin"/>, so the log names the panel that ran
		/// a command.
		/// </summary>
		public string Origin { get; }

		internal ViewServices Services { get; }

		public OmniDebuggerTabContext(
			IOmniDebuggerHost debugger,
			OmniDebuggerTabState state,
			string origin)
			: this(debugger, state, origin, null)
		{
		}

		internal OmniDebuggerTabContext(
			IOmniDebuggerHost debugger,
			OmniDebuggerTabState state,
			string origin,
			ViewServices services)
		{
			Debugger = debugger;
			State = state;
			Origin = origin;
			Services = services;
		}
	}
}
