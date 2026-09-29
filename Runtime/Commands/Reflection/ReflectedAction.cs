using System;
using System.Reflection;

namespace DTech.OmniDebugger
{
	internal sealed class ReflectedAction : IExecutableCommand
	{
		private readonly object _target;
		private readonly MethodInfo _method;

		public CommandDefinition Definition { get; }

		public ReflectedAction(CommandDefinition definition, object target, MethodInfo method)
		{
			Definition = definition ?? throw new ArgumentNullException(nameof(definition));
			_target = target ?? throw new ArgumentNullException(nameof(target));
			_method = method ?? throw new ArgumentNullException(nameof(method));
		}

		public void Execute(object[] arguments) => _method.Invoke(_target, arguments);
	}
}