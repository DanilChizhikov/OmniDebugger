namespace DTech.OmniDebugger.Tests.EditorMode
{
	internal sealed class RangedCommands
	{
		public const string Group = "Ranged";

		public const string SpeedPath = Group + "/Speed";
		public const string NamePath = Group + "/Name";
		public const string TeleportPath = Group + "/Teleport";

		[DebugCommand(Group, Name = "Speed")]
		[DebugRange(0.5, 3.0, Step = 0.25)]
		public float Speed { get; set; } = 1.0f;

		[DebugCommand(Group, Name = "Name")]
		public string Name { get; set; }

		[DebugCommand(Group, Name = "Teleport")]
		public void Teleport([DebugRange(-100, 100)] int x, int y)
		{
		}
	}
}
