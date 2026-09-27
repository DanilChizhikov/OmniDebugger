namespace DTech.OmniDebugger
{
	/// <summary>
	/// Runs and reads registered commands. Loosely typed arguments are coerced to the
	/// declared types, omitted optional arguments fall back to their defaults, and a command
	/// that throws is caught and logged rather than escaping into the caller.
	/// </summary>
	public interface ICommandInvoker
	{
		/// <summary>
		/// Reads a value command.
		/// </summary>
		/// <returns><c>false</c> when no such command exists or it is not readable.</returns>
		bool TryGetValue(string key, out object value);

		/// <summary>
		/// Runs a command by key.
		/// </summary>
		/// <returns>
		/// <c>false</c> when no such command exists, it is not executable, the arguments
		/// cannot be bound, or the command threw. Every failure is logged with the reason.
		/// </returns>
		bool TryExecute(string key, in InvocationRequest request);

		/// <summary>
		/// Runs a command whose definition you already hold, skipping the key lookup.
		/// </summary>
		bool TryExecute(CommandDefinition definition, in InvocationRequest request);
	}
}