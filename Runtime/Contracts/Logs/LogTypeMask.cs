using System;

namespace DTech.OmniDebugger
{
	/// <summary>Kinds of record an <see cref="LogQuery"/> matches.</summary>
	[Flags]
	public enum LogTypeMask : byte
	{
		None = 0,
		Log = 1 << 0,
		Warning = 1 << 1,

		/// <summary>Errors, asserts and exceptions.</summary>
		Error = 1 << 2,

		All = Log | Warning | Error,
	}
}