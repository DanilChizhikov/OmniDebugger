namespace DTech.OmniDebugger.UI
{
	/// <summary>What the runtime panel asks for before it shows. Set in <see cref="OmniDebuggerLockOptions.Mode"/>.</summary>
	public enum OmniDebuggerLockMode : byte
	{
		/// <summary>The panel opens without asking.</summary>
		None = 0,

		/// <summary>A PIN of digits only, typed on the panel's own keypad.</summary>
		Pin = 1,

		/// <summary>A password of any characters, typed into a masked text field.</summary>
		Password = 2,
	}
}