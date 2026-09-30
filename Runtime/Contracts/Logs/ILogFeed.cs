using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Unity's console, captured by the debugger from the moment it is constructed. Safe to read from
	/// any thread; messages arrive from any thread too.
	/// </summary>
	public interface ILogFeed
	{
		/// <summary>Grows whenever a record is added or repeated, or the feed is cleared.</summary>
		long Version { get; }

		/// <summary>
		/// Errors, asserts and exceptions received since the debugger was built, repeats included. Never goes
		/// down, not even on <see cref="Clear"/>, so a reader can tell a new error from an old one.
		/// </summary>
		long ErrorCount { get; }

		/// <summary>
		/// Records currently kept. The oldest are dropped once the record capacity or the text budget is
		/// reached. A message repeated back to back is one record (see <see cref="LogRecord.RepeatCount"/>), and
		/// one repeated anywhere else costs a record but no text: its text is stored once.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Appends one page of matching records to <paramref name="results"/>, oldest first.
		/// </summary>
		/// <returns><c>true</c> when more matching records lie beyond the page, in the direction it grew.</returns>
		bool Query(in LogQuery query, List<LogRecord> results);

		/// <summary>How many kept records are plain logs, warnings and errors.</summary>
		void CountByType(out int logs, out int warnings, out int errors);

		/// <summary>Appends every tag used by a kept record, sorted, ignoring case.</summary>
		void GetKnownTags(List<string> results);

		/// <summary>Drops every record.</summary>
		void Clear();
	}
}