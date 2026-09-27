using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Opt out of reflection: an object that hands the catalog its commands directly.
	/// <see cref="ICommandCatalog.AddSource"/> checks for this first and only falls back
	/// to scanning attributes when the object does not implement it.
	/// </summary>
	public interface ICommandSource
	{
		/// <summary>
		/// The commands this object contributes. May be null or empty, which registers nothing.
		/// </summary>
		IEnumerable<IDebugCommand> GetCommands();
	}
}