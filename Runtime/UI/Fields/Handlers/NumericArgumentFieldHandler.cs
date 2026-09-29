using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class NumericArgumentFieldHandler : IArgumentFieldHandler
	{
		private const int MaxStepDecimals = 6;
		private const double DecimalTolerance = 1e-9;

		public int Priority => 0;

		public bool CanHandle(Type valueType) =>
			valueType == typeof(int) ||
			valueType == typeof(short) ||
			valueType == typeof(ushort) ||
			valueType == typeof(byte) ||
			valueType == typeof(sbyte) ||
			valueType == typeof(uint) ||
			valueType == typeof(long) ||
			valueType == typeof(ulong) ||
			valueType == typeof(float) ||
			valueType == typeof(double);

		public IArgumentField Create(in ArgumentFieldRequest request)
		{
			Type type = request.ValueType;

			if (!request.Argument.Range.IsEmpty)
			{
				IArgumentField ranged = CreateRanged(request);

				if (ranged != null)
				{
					return ranged;
				}
			}

			if (type == typeof(float))
			{
				return new NumericArgumentField<float>(new FloatField(), request);
			}

			if (type == typeof(double))
			{
				return new NumericArgumentField<double>(new DoubleField(), request);
			}

			if (type == typeof(long))
			{
				return new NumericArgumentField<long>(new LongField(), request);
			}

			if (type == typeof(ulong))
			{
				return new NumericArgumentField<ulong>(new UnsignedLongField(), request);
			}

			if (type == typeof(uint))
			{
				return new NumericArgumentField<uint>(new UnsignedIntegerField(), request);
			}

			return new NumericArgumentField<int>(new IntegerField(), request);
		}

		private static IArgumentField CreateRanged(in ArgumentFieldRequest request)
		{
			Type type = request.ValueType;
			ArgumentRange range = request.Argument.Range;

			if (type == typeof(float) || type == typeof(double))
			{
				return CreateFractional(request, range, type == typeof(double));
			}

			return TryGetIntegerBounds(type, out long typeMin, out long typeMax)
				? CreateInteger(request, range, type, typeMin, typeMax)
				: null;
		}

		private static IArgumentField CreateFractional(in ArgumentFieldRequest request, ArgumentRange range, bool isDouble)
		{
			float low = (float)range.Min;
			float high = (float)range.Max;
			float step = (float)range.Step;
			int decimals = CountDecimals(range.Step);

			if (!(high > low))
			{
				return null;
			}

			return new RangeArgumentField<float>(
				new Slider(low, high) { fill = true },
				new FloatField(),
				value => SnapFractional(value, low, high, step, decimals),
				value => ParseFractional(value, low),
				value => isDouble ? ToDouble(value) : value,
				request);
		}

		private static IArgumentField CreateInteger(
			in ArgumentFieldRequest request,
			ArgumentRange range,
			Type type,
			long typeMin,
			long typeMax)
		{
			double lowBound = Math.Max(typeMin, Math.Ceiling(range.Min));
			double highBound = Math.Min(typeMax, Math.Floor(range.Max));

			if (!(highBound > lowBound))
			{
				return null;
			}

			int low = (int)lowBound;
			int high = (int)highBound;
			int step = Math.Max(1, (int)Math.Round(range.Step));

			return new RangeArgumentField<int>(
				new SliderInt(low, high) { fill = true },
				new IntegerField(),
				value => SnapInteger(value, low, high, step),
				value => ParseInteger(value, low),
				value => type == typeof(int) ? value : Convert.ChangeType(value, type, CultureInfo.InvariantCulture),
				request);
		}

		private static bool TryGetIntegerBounds(Type type, out long min, out long max)
		{
			if (type == typeof(int))
			{
				min = int.MinValue;
				max = int.MaxValue;
				return true;
			}

			if (type == typeof(short))
			{
				min = short.MinValue;
				max = short.MaxValue;
				return true;
			}

			if (type == typeof(ushort))
			{
				min = ushort.MinValue;
				max = ushort.MaxValue;
				return true;
			}

			if (type == typeof(byte))
			{
				min = byte.MinValue;
				max = byte.MaxValue;
				return true;
			}

			if (type == typeof(sbyte))
			{
				min = sbyte.MinValue;
				max = sbyte.MaxValue;
				return true;
			}

			min = 0;
			max = 0;
			return false;
		}

		private static float SnapFractional(float value, float low, float high, float step, int decimals)
		{
			if (float.IsNaN(value))
			{
				return low;
			}

			if (step > 0.0f)
			{
				value = low + Mathf.Round((value - low) / step) * step;
			}

			value = (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
			return Mathf.Clamp(value, low, high);
		}

		private static int SnapInteger(int value, int low, int high, int step)
		{
			long snapped = low + (long)Math.Round(((long)value - low) / (double)step, MidpointRounding.AwayFromZero) * step;
			return (int)Math.Max(low, Math.Min(high, snapped));
		}

		private static float ParseFractional(object value, float fallback)
		{
			if (value == null)
			{
				return fallback;
			}

			try
			{
				return Convert.ToSingle(value, CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				return fallback;
			}
		}

		private static int ParseInteger(object value, int fallback)
		{
			if (value == null)
			{
				return fallback;
			}

			try
			{
				return Convert.ToInt32(value, CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				return fallback;
			}
		}

		private static object ToDouble(float value) =>
			double.Parse(value.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

		private static int CountDecimals(double step)
		{
			if (!(step > 0.0))
			{
				return MaxStepDecimals;
			}

			int decimals = 0;
			double scaled = step;

			while (decimals < MaxStepDecimals &&
				Math.Abs(scaled - Math.Round(scaled)) > DecimalTolerance * Math.Max(1.0, scaled))
			{
				scaled *= 10.0;
				decimals++;
			}

			return decimals;
		}
	}
}
