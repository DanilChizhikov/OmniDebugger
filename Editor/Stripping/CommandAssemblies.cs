#if OMNI_DEBUGGER
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;
using CompiledAssembly = UnityEditor.Compilation.Assembly;

namespace DTech.OmniDebugger.Editor
{
	internal static class CommandAssemblies
	{
		public static List<Type> CollectTypes()
		{
			string runtimeName = typeof(DebugCommandAttribute).Assembly.GetName().Name;
			HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);

			foreach (CompiledAssembly compiled in CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies))
			{
				if (References(compiled, runtimeName))
				{
					names.Add(compiled.name);
				}
			}

			List<Type> types = new List<Type>();

			foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (names.Contains(loaded.GetName().Name))
				{
					AddTypes(loaded, types);
				}
			}

			return types;
		}

		private static bool References(CompiledAssembly compiled, string assemblyName)
		{
			CompiledAssembly[] references = compiled.assemblyReferences;

			for (int i = 0; i < references.Length; i++)
			{
				if (string.Equals(references[i].name, assemblyName, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private static void AddTypes(Assembly assembly, List<Type> types)
		{
			try
			{
				types.AddRange(assembly.GetTypes());
			}
			catch (ReflectionTypeLoadException exception)
			{
				foreach (Type type in exception.Types)
				{
					if (type != null)
					{
						types.Add(type);
					}
				}
			}
		}
	}
}
#endif
