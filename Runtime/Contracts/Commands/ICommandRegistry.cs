using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Owns which commands exist, and runs them. Registration is always explicit — nothing scans assemblies
	/// on its own — so startup costs only what you hand over, and never any reflection.
	/// </summary>
	public interface ICommandRegistry
	{
		/// <summary>Raised after every registration and every removal, once per batch.</summary>
		event Action OnChanged;

		/// <summary>
		/// Snapshot of every registered command, in no particular order. Replaced wholesale on change
		/// rather than mutated, so a list captured here stays valid while <see cref="OnChanged"/> runs.
		/// </summary>
		IReadOnlyList<CommandDefinition> All { get; }

		/// <summary>Looks up a command by its path. See <see cref="CommandPath"/>.</summary>
		bool TryGet(string path, out CommandDefinition definition);

		/// <summary>
		/// Registers every <see cref="DebugCommandAttribute"/> member of an object, through the code the
		/// source generator wrote for its type and its base types. The registry holds a strong reference to
		/// the object until the returned handle is disposed, <see cref="Unregister"/> is called, or the
		/// debugger is disposed.
		/// </summary>
		/// <returns>
		/// A handle whose <c>Dispose</c> removes exactly what this call added. When the object is already
		/// registered or declares no commands, the reason is logged and the handle does nothing.
		/// </returns>
		IDisposable Register(object target);

		/// <summary>Removes what <see cref="Register"/> added for this object.</summary>
		/// <returns><c>false</c> when the object was not registered.</returns>
		bool Unregister(object target);

		/// <summary>Registers commands built by hand. A path already taken is logged and left out.</summary>
		/// <returns>A handle whose <c>Dispose</c> removes exactly the commands that were accepted.</returns>
		IDisposable Add(IEnumerable<DebugCommand> commands);

		/// <summary>
		/// Starts a fluent description of commands built from delegates:
		/// <c>Build().Group("Economy").Button("Add 100", …).Register()</c>.
		/// </summary>
		CommandBuilder Build();

		/// <summary>Runs a command by path with loosely typed arguments.</summary>
		/// <returns>
		/// <c>false</c> when no such command exists, it cannot run, the arguments cannot be bound, or the
		/// command threw. Every failure is logged with the reason.
		/// </returns>
		bool TryExecute(string path, params object[] arguments);

		/// <summary>Runs a command by path, naming who asked for the audit log.</summary>
		bool TryExecute(string path, in InvocationRequest request);

		/// <summary>Runs a command whose definition you already hold.</summary>
		bool TryExecute(CommandDefinition definition, in InvocationRequest request);

		/// <summary>Reads a value command.</summary>
		/// <returns><c>false</c> when no such command exists, it holds no value, or its getter threw.</returns>
		bool TryGetValue(string path, out object value);

		/// <summary>
		/// Reads the values an argument declared with <see cref="DebugOptionsAttribute"/> is picked from, as
		/// they are right now, and appends them to <paramref name="options"/>. Null entries and values that
		/// cannot be read as the argument's type are left out; a source that yields null adds nothing.
		/// </summary>
		/// <returns>
		/// <c>false</c> when no such command exists, the argument has no options, or the source threw.
		/// </returns>
		bool TryGetOptions(string path, int argumentIndex, ICollection<object> options);
	}
}
