namespace DTech.OmniDebugger
{
	internal readonly struct LogQueryToken
	{
		public readonly LogQueryTokenKind Kind;
		public readonly string Value;
		public readonly bool Negated;
		public readonly LogTypeMask Type;
		public readonly int Start;
		public readonly int Length;

		public LogQueryToken(LogQueryTokenKind kind, string value, bool negated, LogTypeMask type, int start, int length)
		{
			Kind = kind;
			Value = value;
			Negated = negated;
			Type = type;
			Start = start;
			Length = length;
		}
	}
}
