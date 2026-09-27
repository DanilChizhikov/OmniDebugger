namespace DTech.OmniDebugger
{
	/// <summary>
	/// How a command behaves from a caller's point of view. This is deliberately not a
	/// description of the underlying C# member: a command may be backed by reflection,
	/// by a delegate, or by anything else.
	/// </summary>
	public enum CommandKind : byte
	{
		/// <summary>Takes arguments and runs. Produces no value.</summary>
		Action = 0,

		/// <summary>Holds a single value that can be both read and written.</summary>
		Value = 1,

		/// <summary>Holds a single value that can only be read.</summary>
		ReadonlyValue = 2,
	}
}