using System;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	internal sealed class SampleCommands
	{
		public const string Group = "Tests";

		public const string AddCoinsKey = Group + "/Add Coins";
		public const string GodModeKey = Group + "/God Mode";
		public const string BuildVersionKey = Group + "/Build Version";
		public const string BoomKey = Group + "/Boom";
		public const string SetSpeedKey = Group + "/Set Speed";
		public const string SetSeverityKey = Group + "/Set Severity";
		public const string PrivateSetterKey = Group + "/Private Setter";
		public const string BoomMessage = "boom";

		public const int ExpectedCommandCount = 8;

		public const int ExpectedSkippedCount = 3;

		public int Coins { get; private set; }

		public bool BoomCalled { get; private set; }

		public float Speed { get; private set; }

		public SampleSeverity Severity { get; private set; } = SampleSeverity.One;

		[DebugCommand(Group, "God Mode", 20)]
		public bool GodMode { get; set; }

		[DebugCommand(Group, "Build Version", 30)]
		public string BuildVersion => "1.0.0";

		[DebugCommand(Group, "Private Setter", 70)]
		public int PrivateSetter { get; private set; } = 7;

		[DebugCommand(Group, "Static", 80)]
		public static void StaticCommand()
		{
		}

		[DebugCommand(Group, "Add Coins", 10, Description = "Adds coins to the wallet.")]
		[DebugTags("economy", "wallet")]
		public void AddCoins(int amount = 100) => Coins += amount;

		[DebugCommand(Group, "Boom", 40)]
		public void Boom()
		{
			BoomCalled = true;
			throw new InvalidOperationException(BoomMessage);
		}

		[DebugCommand(Group, "Set Speed", 50)]
		public void SetSpeed(float speed) => Speed = speed;

		[DebugCommand(Group, "Set Severity", 60)]
		public void SetSeverity(SampleSeverity severity) => Severity = severity;

		[DebugCommand(Group)]
		public void Unnamed()
		{
		}

		[DebugCommand(Group, "Returns", 90)]
		public int ReturnsValue() => 1;

		public void NotACommand()
		{
		}

		[DebugCommand(Group, "Hidden", 100)]
		private void HiddenCommand()
		{
		}
	}
}