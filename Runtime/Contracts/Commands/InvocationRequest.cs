using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// One request to run a command: who is asking, and with what.
	/// A struct so that invoking a command allocates nothing beyond the argument array.
	/// Every member is safe to read on <c>default(InvocationRequest)</c>.
	/// </summary>
	public readonly struct InvocationRequest
	{
		/// <summary>Used when a caller does not name itself.</summary>
		public const string UnknownOrigin = "Unknown";

		private readonly string _origin;
		private readonly object[] _arguments;

		/// <summary>
		/// Who asked for this, for the audit log: <c>"Panel"</c>, <c>"Console"</c>, a test name.
		/// Never null.
		/// </summary>
		public string Origin => string.IsNullOrWhiteSpace(_origin) ? UnknownOrigin : _origin;

		/// <summary>
		/// Values to pass, in argument order. Never null. Values may be loosely typed
		/// (strings from a text field, for example) and are coerced before the call.
		/// </summary>
		public object[] Arguments => _arguments ?? Array.Empty<object>();

		public InvocationRequest(string origin, object[] arguments = null)
		{
			_origin = origin;
			_arguments = arguments;
		}

		/// <summary>
		/// Shorthand for the common call site: <c>InvocationRequest.From("Console", 500)</c>.
		/// </summary>
		public static InvocationRequest From(string origin, params object[] arguments) =>
			new InvocationRequest(origin, arguments);
	}
}