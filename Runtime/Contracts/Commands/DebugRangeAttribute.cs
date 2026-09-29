using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The range a numeric command property or parameter is meant to stay in. The panel edits it with a
	/// slider next to a value box. Supported for <c>int</c>, <c>short</c>, <c>ushort</c>, <c>byte</c>,
	/// <c>sbyte</c>, <c>float</c> and <c>double</c>; other types keep a plain field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
	public sealed class DebugRangeAttribute : Attribute
	{
		private readonly double _min;
		private readonly double _max;

		/// <summary>The declared range, <see cref="Step"/> included.</summary>
		public ArgumentRange Range => new ArgumentRange(_min, _max, Step);

		/// <summary>
		/// Distance between two values the slider stops at. Zero, the default, moves freely; integer
		/// types always move by whole numbers.
		/// </summary>
		public double Step { get; set; }

		public DebugRangeAttribute(double min, double max)
		{
			ArgumentRange validated = new ArgumentRange(min, max);

			_min = validated.Min;
			_max = validated.Max;
		}
	}
}
