using System;
using System.Globalization;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class InfoSamples
	{
		public const int Capacity = 120;

		private readonly Func<float> _sample;
		private readonly float[] _values = new float[Capacity];

		public float Min { get; }
		public float Max { get; }
		public int Count { get; private set; }

		public float this[int index] => _values[(_head - Count + index + Capacity) % Capacity];

		public float Top
		{
			get
			{
				if (Max > Min)
				{
					return Max;
				}

				float top = Min;
				for (int i = 0; i < Count; i++)
				{
					top = Mathf.Max(top, this[i]);
				}

				return top > Min ? top * 1.1f : Min + 1.0f;
			}
		}

		private int _head;
		private float _windowSum;
		private int _windowCount;
		private float _average = float.NaN;
		private bool _failed;

		public InfoSamples(Func<float> sample, float min, float max)
		{
			_sample = sample;
			Min = min;
			Max = max;
		}

		public void Sample()
		{
			if (_failed)
			{
				return;
			}

			float value;
			try
			{
				value = _sample();
			}
			catch (Exception exception)
			{
				_failed = true;
				UnityLogSink.Default.Exception("An Info chart failed to sample and was stopped.", exception);
				return;
			}

			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				return;
			}

			_values[_head] = value;
			_head = (_head + 1) % Capacity;
			Count = Math.Min(Count + 1, Capacity);

			_windowSum += value;
			_windowCount++;
		}

		public string TakeAverage(string unit)
		{
			if (_windowCount > 0)
			{
				_average = _windowSum / _windowCount;
				_windowSum = 0.0f;
				_windowCount = 0;
			}

			if (float.IsNaN(_average))
			{
				return InfoSectionModel.Unknown;
			}

			string number = _average.ToString(Mathf.Abs(_average) >= 100.0f ? "0" : "0.0", CultureInfo.InvariantCulture);
			return string.IsNullOrEmpty(unit) ? number : number + " " + unit;
		}
	}
}
