using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal static class TypeBindingCache
	{
		private const BindingFlags ScannedMembers =
			BindingFlags.Instance |
			BindingFlags.Static |
			BindingFlags.Public |
			BindingFlags.NonPublic;

		private const string StaticSkipReason = "a command needs an instance to run against, and this member is static";
		private const string NonPublicSkipReason = "a command has to be reachable from outside its type, and this member is not public";

		private static readonly Assembly _engineAssembly = typeof(MonoBehaviour).Assembly;
		private static readonly MemberBinding[] _noBindings = Array.Empty<MemberBinding>();
		private static readonly Dictionary<Type, MemberBinding[]> _cache = new ();

		public static MemberBinding[] GetBindings(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			if (_cache.TryGetValue(type, out MemberBinding[] cached))
			{
				return cached;
			}

			cached = Build(type);
			_cache[type] = cached;
			return cached;
		}

		private static MemberBinding[] Build(Type type)
		{
			MemberInfo[] members = type.GetMembers(ScannedMembers);
			List<MemberBinding> bindings = null;

			for (int i = 0; i < members.Length; i++)
			{
				MemberInfo member = members[i];

				if (member.DeclaringType == null || member.DeclaringType.Assembly == _engineAssembly)
				{
					continue;
				}

				MemberBinding binding;
				try
				{
					if (!TryBind(member, out binding))
					{
						continue;
					}
				}
				catch (Exception exception)
				{
					binding = MemberBinding.Skipped(member, $"the command attributes could not be read ({exception.GetBaseException().Message})");
				}

				bindings ??= new List<MemberBinding>();
				bindings.Add(binding);
			}

			return bindings == null ? _noBindings : bindings.ToArray();
		}

		private static bool TryBind(MemberInfo member, out MemberBinding binding)
		{
			binding = default;

			DebugCommandAttribute command = member.GetCustomAttribute<DebugCommandAttribute>();
			if (command == null)
			{
				return false;
			}

			IReadOnlyList<string> tags = member.GetCustomAttribute<DebugTagsAttribute>()?.Tags;
			CommandIcon icon = member.GetCustomAttribute<DebugIconAttribute>()?.Icon ?? default;

			switch (member)
			{
				case PropertyInfo property:
					binding = BindProperty(property, command, tags, icon);
					return true;

				case MethodInfo method:
					binding = BindMethod(method, command, tags, icon);
					return true;

				default:
					binding = MemberBinding.Skipped(member, "only methods and properties can be commands");
					return true;
			}
		}

		private static MemberBinding BindProperty(
			PropertyInfo property,
			DebugCommandAttribute command,
			IReadOnlyList<string> tags,
			CommandIcon icon)
		{
			MethodInfo getter = property.GetGetMethod();
			if (getter == null)
			{
				return MemberBinding.Skipped(property, "a command property needs a public getter");
			}

			if (getter.IsStatic)
			{
				return MemberBinding.Skipped(property, StaticSkipReason);
			}

			bool isWritable = property.GetSetMethod() != null;

			ArgumentDefinition[] arguments =
			{
				new ArgumentDefinition(
					property.Name,
					property.PropertyType,
					range: property.GetCustomAttribute<DebugRangeAttribute>()?.Range ?? default),
			};

			CommandDefinition definition = new CommandDefinition(
				name: command.Name ?? property.Name,
				groupName: command.GroupName,
				kind: isWritable ? CommandKind.Value : CommandKind.ReadonlyValue,
				sortOrder: command.SortOrder,
				arguments: arguments,
				tags: tags,
				description: command.Description,
				icon: icon);

			return MemberBinding.Valid(property, definition);
		}

		private static MemberBinding BindMethod(
			MethodInfo method,
			DebugCommandAttribute command,
			IReadOnlyList<string> tags,
			CommandIcon icon)
		{
			if (method.IsStatic)
			{
				return MemberBinding.Skipped(method, StaticSkipReason);
			}

			if (!method.IsPublic)
			{
				return MemberBinding.Skipped(method, NonPublicSkipReason);
			}

			if (method.ReturnType != typeof(void))
			{
				return MemberBinding.Skipped(
					method,
					$"a command method has to return void, but returns {method.ReturnType.Name}");
			}

			if (method.IsGenericMethodDefinition)
			{
				return MemberBinding.Skipped(method, "generic methods are not supported");
			}

			ParameterInfo[] parameters = method.GetParameters();
			ArgumentDefinition[] arguments = parameters.Length == 0
				? Array.Empty<ArgumentDefinition>()
				: new ArgumentDefinition[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo parameter = parameters[i];

				if (parameter.ParameterType.IsByRef)
				{
					return MemberBinding.Skipped(
						method,
						$"'{parameter.Name}' is a ref/out parameter, which is not supported");
				}

				arguments[i] = new ArgumentDefinition(
					name: parameter.Name,
					type: parameter.ParameterType,
					defaultValue: parameter.HasDefaultValue ? parameter.DefaultValue : null,
					isOptional: parameter.IsOptional,
					range: parameter.GetCustomAttribute<DebugRangeAttribute>()?.Range ?? default);
			}

			CommandDefinition definition = new CommandDefinition(
				name: command.Name ?? method.Name,
				groupName: command.GroupName,
				kind: CommandKind.Action,
				sortOrder: command.SortOrder,
				arguments: arguments,
				tags: tags,
				description: command.Description,
				icon: icon);

			return MemberBinding.Valid(method, definition);
		}
	}
}