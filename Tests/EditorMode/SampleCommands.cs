using System;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	internal class SampleCommands
	{
		public const string Group = "Tests";
		public const string NestedGroup = Group + "/Nested";

		public const string AddCoinsPath = Group + "/Add Coins";
		public const string GodModePath = Group + "/God Mode";
		public const string BuildVersionPath = Group + "/Build Version";
		public const string BoomPath = Group + "/Boom";
		public const string SetSpeedPath = Group + "/Set Speed";
		public const string SetSeverityPath = Group + "/Set Severity";
		public const string PrivateSetterPath = Group + "/Private Setter";
		public const string UnnamedPath = Group + "/Unnamed";
		public const string DeepPath = NestedGroup + "/Deep";
		public const string BoomMessage = "boom";

		public const int ExpectedCommandCount = 9;

		public int Coins { get; private set; }

		public bool BoomCalled { get; private set; }

		public float Speed { get; private set; }

		public int DeepCalls { get; private set; }

		public SampleSeverity Severity { get; private set; } = SampleSeverity.One;

		[DebugCommand(Group, Name = "God Mode", Order = 20)]
		public bool GodMode { get; set; }

		[DebugCommand(Group, Name = "Build Version", Order = 30)]
		public string BuildVersion => "1.0.0";

		[DebugCommand(Group, Name = "Private Setter", Order = 70)]
		public int PrivateSetter { get; private set; } = 7;

		[DebugCommand(Group, Name = "Add Coins", Order = 10, Description = "Adds coins to the wallet.")]
		[DebugTags("economy", "wallet")]
		[DebugIcon("coin")]
		public void AddCoins(int amount = 100) => Coins += amount;

		[DebugCommand(Group, Name = "Boom", Order = 40)]
		public void Boom()
		{
			BoomCalled = true;
			throw new InvalidOperationException(BoomMessage);
		}

		[DebugCommand(Group, Name = "Set Speed", Order = 50)]
		public void SetSpeed(float speed) => Speed = speed;

		[DebugCommand(Group)]
		public void Unnamed()
		{
		}

		[DebugCommand(NestedGroup, Name = "Deep")]
		public void Deep() => DeepCalls++;

		public void NotACommand()
		{
		}

		[DebugCommand(Group, Name = "Set Severity", Order = 60)]
		internal void SetSeverity(SampleSeverity severity) => Severity = severity;
	}
}
