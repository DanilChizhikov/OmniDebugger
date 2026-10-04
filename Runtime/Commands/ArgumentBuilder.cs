using System;
using System.Collections;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Describes one argument of a command built with <see cref="CommandBuilder"/>:
	/// <c>a => a.Name("amount").Default(100).Range(0, 10_000)</c>, or
	/// <c>a => a.Options(() => _levelIds)</c> to pick it from a list.
	/// </summary>
	public sealed class ArgumentBuilder
	{
		private readonly Type _type;
		private readonly object _typeDefault;

		private string _name;
		private object _defaultValue;
		private bool _isOptional;
		private double _min;
		private double _max;
		private double _step;
		private bool _hasRange;

		internal Func<IEnumerable> OptionsSource { get; private set; }

		internal ArgumentBuilder(string name, Type type, object typeDefault)
		{
			_name = name;
			_type = type;
			_typeDefault = typeDefault;
		}

		/// <summary>The name shown next to the field.</summary>
		public ArgumentBuilder Name(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException("Argument name cannot be null or whitespace.", nameof(name));
			}

			_name = name;
			return this;
		}

		/// <summary>Makes the argument optional, falling back to <paramref name="value"/> when left out.</summary>
		public ArgumentBuilder Default(object value)
		{
			if (!CommandArguments.TryConvert(value, _type, out object converted))
			{
				throw new ArgumentException($"'{value}' cannot be read as {_type.Name}.", nameof(value));
			}

			_defaultValue = converted;
			_isOptional = true;
			return this;
		}

		/// <summary>Makes the argument optional, falling back to the default of its type.</summary>
		public ArgumentBuilder Optional()
		{
			_isOptional = true;
			return this;
		}

		/// <summary>Edits a number with a slider between <paramref name="min"/> and <paramref name="max"/>.</summary>
		public ArgumentBuilder Range(double min, double max, double step = 0.0)
		{
			ArgumentRange validated = new ArgumentRange(min, max, step);

			_min = validated.Min;
			_max = validated.Max;
			_step = validated.Step;
			_hasRange = true;
			return this;
		}

		/// <summary>Snaps the slider set by <see cref="Range"/> to multiples of <paramref name="step"/>.</summary>
		public ArgumentBuilder Step(double step)
		{
			if (!_hasRange)
			{
				throw new InvalidOperationException("Call Range(min, max) before Step(step).");
			}

			_step = new ArgumentRange(_min, _max, step).Step;
			return this;
		}

		/// <summary>
		/// Edits the argument with a dropdown of the values <paramref name="source"/> yields. It is called every
		/// time the dropdown opens; values that cannot be read as the argument's type are left out.
		/// </summary>
		public ArgumentBuilder Options(Func<IEnumerable> source)
		{
			OptionsSource = source ?? throw new ArgumentNullException(nameof(source));
			return this;
		}

		internal ArgumentDefinition Build()
		{
			return new ArgumentDefinition(
				_name,
				_type,
				_isOptional ? _defaultValue ?? _typeDefault : null,
				_isOptional,
				_hasRange ? new ArgumentRange(_min, _max, _step) : default,
				OptionsSource != null);
		}
	}
}
