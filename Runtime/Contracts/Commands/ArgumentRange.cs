using System;
using System.Globalization;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The span a numeric argument is meant to stay in, declared with <see cref="DebugRangeAttribute"/>.
	/// The panel edits a ranged argument with a slider and keeps what is typed inside it; code that
	/// invokes the command is not held to it.
	/// </summary>
	public readonly struct ArgumentRange
	{
		/// <summary>Lowest value.</summary>
		public double Min { get; }

		/// <summary>Highest value.</summary>
		public double Max { get; }

		/// <summary>Distance between two values the slider stops at. Zero moves freely.</summary>
		public double Step { get; }

		/// <summary>True for the default value, which declares no range.</summary>
		public bool IsEmpty => !(Max > Min);

		public ArgumentRange(double min, double max, double step = 0.0)
		{
			if (double.IsNaN(min) || double.IsInfinity(min) || double.IsNaN(max) || double.IsInfinity(max))
			{
				throw new ArgumentException($"A range needs finite bounds, but got {min}..{max}.", nameof(min));
			}

			if (!(max > min))
			{
				throw new ArgumentException($"A range needs its maximum above its minimum, but got {min}..{max}.", nameof(max));
			}

			if (double.IsNaN(step) || double.IsInfinity(step) || step < 0.0)
			{
				throw new ArgumentOutOfRangeException(nameof(step), step, "A range step has to be zero or positive.");
			}

			Min = min;
			Max = max;
			Step = step;
		}

		public override string ToString()
		{
			if (IsEmpty)
			{
				return string.Empty;
			}

			string span = string.Format(CultureInfo.InvariantCulture, "[{0}..{1}]", Min, Max);
			return Step > 0.0 ? string.Format(CultureInfo.InvariantCulture, "{0} step {1}", span, Step) : span;
		}
	}
}
