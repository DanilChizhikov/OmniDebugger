namespace DTech.OmniDebugger.UI
{
	/// <summary>How the runtime panel maps its units to screen pixels.</summary>
	public enum OmniDebuggerScaleMode : byte
	{
		/// <summary>
		/// <see cref="ScreenSize"/> on mobile platforms, <see cref="PhysicalSize"/> everywhere else.
		/// </summary>
		Auto = 0,

		/// <summary>
		/// Scales with the screen: a portrait phone is always 360 units wide, so the panel looks the
		/// same on every phone whatever its resolution.
		/// </summary>
		ScreenSize = 1,

		/// <summary>
		/// One unit is one pixel at 96 DPI. Compact, desktop-style; the panel does not grow with a
		/// bigger window.
		/// </summary>
		PhysicalSize = 2,
	}
}