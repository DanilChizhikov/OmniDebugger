namespace DTech.OmniDebugger.Tests.EditorMode
{
	internal sealed class RangedCommands
	{
		public const string Group = "Ranged";

		public const string SpeedKey = Group + "/Speed";
		public const string NameKey = Group + "/Name";
		public const string TeleportKey = Group + "/Teleport";
		public const string BrokenKey = Group + "/Broken";

		[DebugCommand(Group, "Speed")]
		[DebugRange(0.5, 3.0, Step = 0.25)]
		public float Speed { get; set; } = 1.0f;

		[DebugCommand(Group, "Name")]
		public string Name { get; set; }

		[DebugCommand(Group, "Teleport")]
		public void Teleport([DebugRange(-100, 100)] int x, int y)
		{
		}

		[DebugCommand(Group, "Broken")]
		public void Broken([DebugRange(5, 1)] int value)
		{
		}
	}
}
