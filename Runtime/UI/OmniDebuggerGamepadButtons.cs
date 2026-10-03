using System;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Gamepad buttons, combined into a chord for <see cref="OmniDebuggerOpenOptions.GamepadCombo"/>.
	/// Named by position, so <see cref="South"/> is A on Xbox and Cross on PlayStation.
	/// </summary>
	[Flags]
	public enum OmniDebuggerGamepadButtons
	{
		None = 0,
		South = 1 << 0,
		East = 1 << 1,
		West = 1 << 2,
		North = 1 << 3,
		LeftShoulder = 1 << 4,
		RightShoulder = 1 << 5,
		LeftTrigger = 1 << 6,
		RightTrigger = 1 << 7,
		Select = 1 << 8,
		Start = 1 << 9,
		LeftStickPress = 1 << 10,
		RightStickPress = 1 << 11,
	}
}
