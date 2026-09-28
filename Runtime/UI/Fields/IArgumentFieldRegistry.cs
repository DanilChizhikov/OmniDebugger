namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Which control edits which argument type. The built-ins cover <c>bool</c>, enums, every numeric
	/// type, <c>char</c>, <c>string</c> and anything else convertible from text; register a handler
	/// through <see cref="IOmniDebugger.Fields"/> to add your own type or to replace a built-in.
	/// </summary>
	public interface IArgumentFieldRegistry
	{
		/// <summary>
		/// Adds a handler. A <see cref="IArgumentFieldHandler.Priority"/> above zero wins against the
		/// built-ins; equal priorities are tried in registration order.
		/// </summary>
		void Register(IArgumentFieldHandler handler);

		/// <summary>Removes a handler.</summary>
		/// <returns><c>false</c> when it was not registered.</returns>
		bool Unregister(IArgumentFieldHandler handler);

		/// <summary>
		/// Builds the field for an argument, wrapping it so a <c>Nullable&lt;T&gt;</c> can also be left
		/// unset. Never returns null: an unclaimed type gets a disabled field that explains itself.
		/// </summary>
		IArgumentField Create(in ArgumentFieldRequest request);
	}
}