using System;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Decides which control edits which type. Register one to support your own type — a
	/// <c>Vector3</c>, an asset id, a colour — without touching anything else in the panel.
	/// </summary>
	public interface IArgumentFieldHandler
	{
		/// <summary>
		/// Higher wins. The built-in handlers all sit at zero, so any positive value overrides them —
		/// including for <c>float</c> or <c>bool</c>.
		/// </summary>
		int Priority { get; }

		/// <summary>
		/// Whether this handler edits that type. The type is already unwrapped, so return <c>true</c>
		/// for <c>T</c> and nullability is handled for you.
		/// </summary>
		bool CanHandle(Type valueType);

		/// <summary>Builds the field. Called once per row, and again after a row is rebuilt.</summary>
		IArgumentField Create(in ArgumentFieldRequest request);
	}
}