namespace DTech.OmniDebugger
{
	/// <summary>Which icon a command shows. The default value means none.</summary>
	public readonly struct CommandIcon
	{
		public DebugIconSource Source { get; }

		/// <summary>Null when the command has no icon.</summary>
		public string Key { get; }

		public bool IsEmpty => string.IsNullOrWhiteSpace(Key);

		public CommandIcon(DebugIconSource source, string key)
		{
			Source = source;
			Key = key;
		}

		public override string ToString() => IsEmpty ? "none" : $"{Source}:{Key}";
	}
}