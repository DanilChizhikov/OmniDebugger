using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal static class SourceScanner
	{
		public static ScanResult Scan(object source, ILogSink log)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			Type type = source.GetType();

			if (source is ICommandSource custom)
			{
				return ScanCustom(custom, type, log);
			}

			MemberBinding[] bindings = TypeBindingCache.GetBindings(type);
			if (bindings.Length == 0)
			{
				return ScanResult.Empty;
			}

			List<IDebugCommand> commands = new List<IDebugCommand>(bindings.Length);

			for (int i = 0; i < bindings.Length; i++)
			{
				MemberBinding binding = bindings[i];

				if (!binding.IsValid)
				{
					log.Warning(
						$"Attributed member is not registered. Source: {type.Name}; " +
						$"Member: {binding.Member.Name}; Reason: {binding.SkipReason}.");
					continue;
				}

				commands.Add(ReflectedCommandFactory.Create(binding, source));
			}

			return new ScanResult(commands);
		}

		private static ScanResult ScanCustom(ICommandSource source, Type type, ILogSink log)
		{
			IEnumerable<IDebugCommand> supplied = source.GetCommands();
			if (supplied == null)
			{
				return ScanResult.Empty;
			}

			List<IDebugCommand> commands = new List<IDebugCommand>();

			foreach (IDebugCommand command in supplied)
			{
				if (command?.Definition == null)
				{
					log.Warning($"Source supplied a null command and it was ignored. Source: {type.Name}.");
					continue;
				}

				commands.Add(command);
			}

			return new ScanResult(commands);
		}
	}
}