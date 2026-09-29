namespace DTech.OmniDebugger
{
	/// <summary>
	/// A registered command. What it can actually do is expressed by also implementing
	/// <see cref="IExecutableCommand"/>, <see cref="IReadableCommand"/>, or both —
	/// <see cref="CommandDefinition.Kind"/> is a hint for the UI, not the contract.
	/// </summary>
	public interface IDebugCommand
	{
		/// <summary>Metadata for this command. Never null, never changes.</summary>
		CommandDefinition Definition { get; }
	}
}