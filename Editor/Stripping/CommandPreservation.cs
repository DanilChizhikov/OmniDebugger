#if OMNI_DEBUGGER
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace DTech.OmniDebugger.Editor
{
	internal sealed class CommandPreservation
	{
		private readonly SortedDictionary<string, SortedSet<string>> _typesByAssembly = new (StringComparer.Ordinal);

		public int AssemblyCount => _typesByAssembly.Count;

		public int TypeCount { get; private set; }

		public bool IsEmpty => TypeCount == 0;

		public static CommandPreservation Collect(IEnumerable<Type> types)
		{
			if (types == null)
			{
				throw new ArgumentNullException(nameof(types));
			}

			CommandPreservation preservation = new CommandPreservation();

			foreach (Type type in types)
			{
				preservation.AddCommandsOf(type);
			}

			return preservation;
		}

		public string ToXml()
		{
			StringBuilder builder = new StringBuilder();
			builder.Append("<linker>\n");

			foreach (KeyValuePair<string, SortedSet<string>> assembly in _typesByAssembly)
			{
				builder.Append("  <assembly fullname=\"")
					.Append(SecurityElement.Escape(assembly.Key))
					.Append("\" ignoreIfMissing=\"1\">\n");

				foreach (string typeName in assembly.Value)
				{
					builder.Append("    <type fullname=\"")
						.Append(SecurityElement.Escape(typeName))
						.Append("\" preserve=\"all\" />\n");
				}

				builder.Append("  </assembly>\n");
			}

			builder.Append("</linker>\n");
			return builder.ToString();
		}

		private void AddCommandsOf(Type type)
		{
			if (type == null)
			{
				return;
			}

			MemberBinding[] bindings;

			try
			{
				bindings = TypeBindingCache.GetBindingsWithoutCaching(type);
			}
			catch (Exception exception)
			{
				UnityLogSink.Default.Warning(
					$"The commands of {type.FullName} could not be read, so code stripping may remove them " +
					$"({exception.GetBaseException().Message}).");
				return;
			}

			for (int i = 0; i < bindings.Length; i++)
			{
				MemberBinding binding = bindings[i];

				if (!binding.IsValid)
				{
					continue;
				}

				AddType(binding.Member.DeclaringType);

				IReadOnlyList<ArgumentDefinition> arguments = binding.Definition.Arguments;

				for (int j = 0; j < arguments.Count; j++)
				{
					Type valueType = Nullable.GetUnderlyingType(arguments[j].Type) ?? arguments[j].Type;

					if (valueType.IsEnum)
					{
						AddType(valueType);
					}
				}
			}
		}

		private void AddType(Type type)
		{
			if (type == null)
			{
				return;
			}

			if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				type = type.GetGenericTypeDefinition();
			}

			if (string.IsNullOrEmpty(type.FullName))
			{
				return;
			}

			string assemblyName = type.Assembly.GetName().Name;

			if (!_typesByAssembly.TryGetValue(assemblyName, out SortedSet<string> typeNames))
			{
				typeNames = new SortedSet<string>(StringComparer.Ordinal);
				_typesByAssembly.Add(assemblyName, typeNames);
			}

			if (typeNames.Add(type.FullName.Replace('+', '/')))
			{
				TypeCount++;
			}
		}
	}
}
#endif
