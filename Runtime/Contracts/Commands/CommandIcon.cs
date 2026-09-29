namespace DTech.OmniDebugger
{
	/// <summary>Which icon a command shows. The default value means none.</summary>
	public readonly struct CommandIcon
	{
		/// <summary>Where <see cref="Key"/> is looked up.</summary>
		public DebugIconSource Source { get; }

		/// <summary>Null when the command has no icon.</summary>
		public string Key { get; }

		/// <summary>True when there is no key, so there is no icon to show.</summary>
		public bool IsEmpty => string.IsNullOrWhiteSpace(Key);

		public CommandIcon(DebugIconSource source, string key)
		{
			Source = source;
			Key = key;
		}

		public override string ToString() => IsEmpty ? "none" : $"{Source}:{Key}";
	}
}