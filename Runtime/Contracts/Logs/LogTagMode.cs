namespace DTech.OmniDebugger
{
	/// <summary>How the tags of a <see cref="LogQuery"/> combine.</summary>
	public enum LogTagMode : byte
	{
		/// <summary>A record needs every tag.</summary>
		All = 0,

		/// <summary>A record needs at least one tag.</summary>
		Any = 1,
	}
}