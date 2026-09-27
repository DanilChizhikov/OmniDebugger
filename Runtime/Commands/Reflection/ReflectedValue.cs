using System;
using System.Reflection;

namespace DTech.OmniDebugger
{
	internal sealed class ReflectedValue : IExecutableCommand, IReadableCommand
	{
		private readonly object _target;
		private readonly PropertyInfo _property;

		public CommandDefinition Definition { get; }

		public ReflectedValue(CommandDefinition definition, object target, PropertyInfo property)
		{
			Definition = definition ?? throw new ArgumentNullException(nameof(definition));
			_target = target ?? throw new ArgumentNullException(nameof(target));
			_property = property ?? throw new ArgumentNullException(nameof(property));
		}

		public void Execute(object[] arguments)
		{
			if (arguments == null || arguments.Length != 1)
			{
				throw new ArgumentException(
					$"Command '{Definition.Key}' writes a single value and needs exactly one argument, " +
					$"but got {arguments?.Length ?? 0}.",
					nameof(arguments));
			}

			_property.SetValue(_target, arguments[0]);
		}

		public object GetValue() => _property.GetValue(_target);
	}
}