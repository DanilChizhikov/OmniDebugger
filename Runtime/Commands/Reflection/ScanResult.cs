using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal readonly struct ScanResult
	{
		public static readonly ScanResult Empty = new (Array.Empty<IDebugCommand>());

		public IReadOnlyList<IDebugCommand> Commands { get; }

		public bool IsEmpty => Commands == null || Commands.Count == 0;

		public ScanResult(IReadOnlyList<IDebugCommand> commands)
		{
			Commands = commands;
		}
	}
}