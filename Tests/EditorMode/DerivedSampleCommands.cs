namespace DTech.OmniDebugger.Tests.EditorMode
{
	internal sealed class DerivedSampleCommands : SampleCommands
	{
		public const string ExtraPath = Group + "/Extra";

		[DebugCommand(Group, Name = "Extra")]
		public void Extra()
		{
		}
	}
}
