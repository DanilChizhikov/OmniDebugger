using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Owns which commands exist. Registration is always explicit — nothing scans assemblies
	/// on its own — so startup cost is paid only for the objects you hand over.
	/// </summary>
	public interface ICommandCatalog
	{
		/// <summary>
		/// Raised after any change to <see cref="Commands"/>. Fired once per successful
		/// <see cref="AddSource"/> or <see cref="RemoveSource"/>, not once per command.
		/// </summary>
		event Action OnChanged;

		/// <summary>
		/// Snapshot of every registered command, in no particular order. Replaced wholesale
		/// on change rather than mutated, so a list captured here stays valid and unchanged
		/// even while <see cref="OnChanged"/> handlers run.
		/// </summary>
		IReadOnlyList<CommandDefinition> Commands { get; }

		/// <summary>Looks up the live command behind a key. See <see cref="CommandKey"/>.</summary>
		bool TryGetCommand(string key, out IDebugCommand command);

		/// <summary>Looks up only the metadata behind a key.</summary>
		bool TryGetDefinition(string key, out CommandDefinition definition);

		/// <summary>
		/// Registers every command an object contributes. The object may implement
		/// <see cref="ICommandSource"/>; otherwise its <see cref="DebugCommandAttribute"/>
		/// members are scanned. The catalog holds a strong reference until the source is
		/// removed or the owning <see cref="IOmniDebugger"/> is disposed.
		/// </summary>
		/// <returns><c>false</c> when the object was already registered or contributed nothing.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		bool AddSource(object source);

		/// <summary>
		/// Removes exactly the commands this source contributed, without re-scanning it.
		/// </summary>
		/// <returns><c>false</c> when the object was not registered.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		bool RemoveSource(object source);

		/// <summary>Registers one command directly, bypassing sources entirely.</summary>
		/// <returns><c>false</c> when its key is already taken.</returns>
		bool AddCommand(IDebugCommand command);

		/// <summary>Removes one command registered through <see cref="AddCommand"/>.</summary>
		/// <returns><c>false</c> when it was not registered.</returns>
		bool RemoveCommand(IDebugCommand command);
	}
}