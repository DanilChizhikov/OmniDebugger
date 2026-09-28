namespace DTech.OmniDebugger.UI
{
	internal readonly struct ArgumentSlot
	{
		public bool HasValue { get; }
		public object Value { get; }

		public ArgumentSlot(bool hasValue, object value)
		{
			HasValue = hasValue;
			Value = value;
		}

		public static ArgumentSlot Empty() => new ArgumentSlot(false, null);

		public static ArgumentSlot From(object value) => new ArgumentSlot(true, value);
	}
}