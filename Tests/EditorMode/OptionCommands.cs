using System;
using System.Collections;
using System.Collections.Generic;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	internal sealed class OptionCommands
	{
		public const string Group = "Options";

		public const string LoadPath = Group + "/Load";
		public const string CurrentPath = Group + "/Current";
		public const string PickPath = Group + "/Pick";
		public const string MixedPath = Group + "/Mixed";

		public static readonly string[] Themes = { "dark", "light" };

		[DebugCommand(Group, Name = "Current")]
		[DebugOptions(nameof(Themes))]
		public string Current { get; set; } = "dark";

		public List<string> Levels { get; } = new List<string> { "level_01", "level_02" };

		public string LastLevel { get; private set; }

		public int LastNumber { get; private set; }

		public Func<IEnumerable<int>> Numbers;

		public ArrayList Untyped = new ArrayList { 1, "two", null, 3L };

		[DebugCommand(Group, Name = "Load")]
		public void Load([DebugOptions(nameof(Levels))] string level, int times = 1) => LastLevel = level;

		[DebugCommand(Group, Name = "Pick")]
		public void Pick([DebugOptions(nameof(Numbers))] int number) => LastNumber = number;

		[DebugCommand(Group, Name = "Mixed")]
		public void Mixed([DebugOptions(nameof(Untyped))] int number) => LastNumber = number;
	}
}
