using System;
using System.Reflection;

namespace DTech.OmniDebugger
{
	internal static class ReflectedCommandFactory
	{
		public static IDebugCommand Create(in MemberBinding binding, object target)
		{
			if (!binding.IsValid)
			{
				throw new ArgumentException("Cannot create a command from a skipped binding.", nameof(binding));
			}

			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			CommandDefinition definition = binding.Definition;

			switch (definition.Kind)
			{
				case CommandKind.Action when binding.Member is MethodInfo method:
					return new ReflectedAction(definition, target, method);

				case CommandKind.Value when binding.Member is PropertyInfo property:
					return new ReflectedValue(definition, target, property);

				case CommandKind.ReadonlyValue when binding.Member is PropertyInfo readonlyProperty:
					return new ReflectedReadonlyValue(definition, target, readonlyProperty);

				default:
					throw new ArgumentException(
						$"Command '{definition.Key}' is {definition.Kind} but is backed by " +
						$"{binding.Member.GetType().Name}.",
						nameof(binding));
			}
		}
	}
}