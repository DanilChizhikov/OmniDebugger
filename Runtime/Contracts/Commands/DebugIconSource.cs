namespace DTech.OmniDebugger
{
	/// <summary>Where the key of a <see cref="DebugIconAttribute"/> is looked up.</summary>
	public enum DebugIconSource : byte
	{
		/// <summary>A path under a <c>Resources</c> folder, to a texture or a sprite.</summary>
		Resources = 0,

		/// <summary>A key of an icon catalog asset or of a provider registered in code.</summary>
		Catalog = 1,
	}
}