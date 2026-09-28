using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class NumericArgumentFieldHandler : IArgumentFieldHandler
	{
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
	}
}