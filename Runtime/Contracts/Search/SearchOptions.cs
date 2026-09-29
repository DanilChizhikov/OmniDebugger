using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Knobs for one query. <see cref="None"/> runs every stage and ignores letter case,
	/// which is what a search box usually wants.
	/// </summary>
	[Flags]
	public enum SearchOptions
	{
		/// <summary>Case-insensitive, all stages.</summary>
		None = 0,

		/// <summary>
		/// A hit only counts when the typed text appears with the same letter case.
		/// Verified on the few items that survive ranking, so it costs almost nothing.
		/// </summary>
		CaseSensitive = 1 << 0,

		/// <summary>Skips the fuzzy stage, so typos no longer match.</summary>
		NoFuzzy = 1 << 1,

		/// <summary>
		/// Runs the exact stage only: no prefix, no substring, no fuzzy. Useful when the
		/// caller already knows the term it wants.
		/// </summary>
		ExactOnly = 1 << 2,
	}
}