using System;
using System.Reflection;

namespace DTech.OmniDebugger
{
	internal sealed class ReflectedReadonlyValue : IReadableCommand
	{
		private readonly object _target;
		private readonly PropertyInfo _property;

		public CommandDefinition Definition { get; }

		public ReflectedReadonlyValue(CommandDefinition definition, object target, PropertyInfo property)
		{
			Definition = definition ?? throw new ArgumentNullException(nameof(definition));
			_target = target ?? throw new ArgumentNullException(nameof(target));
			_property = property ?? throw new ArgumentNullException(nameof(property));
		}

		public object GetValue() => _property.GetValue(_target);
	}
}