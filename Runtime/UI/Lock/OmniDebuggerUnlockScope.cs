namespace DTech.OmniDebugger.UI
{
	/// <summary>How long a correct PIN or password keeps the panel unlocked.</summary>
	public enum OmniDebuggerUnlockScope : byte
	{
		/// <summary>Asked again every time the panel opens.</summary>
		EveryOpen = 0,

		/// <summary>Asked once until the application restarts.</summary>
		Session = 1,

		/// <summary>Asked once on this device, until the PIN or password is changed.</summary>
		Device = 2,
	}
}