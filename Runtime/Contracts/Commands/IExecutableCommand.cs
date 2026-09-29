namespace DTech.OmniDebugger
{
	/// <summary>
	/// A command that can be run.
	/// </summary>
	public interface IExecutableCommand : IDebugCommand
	{
		/// <summary>
		/// Runs the command. Arguments arrive already coerced to the types named in
		/// <see cref="CommandDefinition.Arguments"/>, and the array length always matches.
		/// Implementations may throw; callers going through <see cref="ICommandInvoker"/>
		/// have their exceptions caught and logged.
		/// </summary>
		void Execute(object[] arguments);
	}
}