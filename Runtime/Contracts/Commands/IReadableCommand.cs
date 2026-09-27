namespace DTech.OmniDebugger
{
	/// <summary>
	/// A command that exposes a current value.
	/// </summary>
	public interface IReadableCommand : IDebugCommand
	{
		/// <summary>
		/// Reads the value now. A writable value command implements this as well as
		/// <see cref="IExecutableCommand"/>, where executing it writes the value.
		/// </summary>
		object GetValue();
	}
}