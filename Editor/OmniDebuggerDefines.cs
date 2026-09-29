using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DTech.OmniDebugger.Editor
{
	internal static class OmniDebuggerDefines
	{
		public const string Symbol = "OMNI_DEBUGGER";

		private static readonly char[] _defineSeparators = { ';' };

		public static NamedBuildTarget ActiveTarget =>
			NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

		private static List<NamedBuildTarget> _knownTargets;

		public static bool IsEnabled(NamedBuildTarget target)
		{
			string[] defines = Read(target);

			for (int i = 0; i < defines.Length; i++)
			{
				if (string.Equals(defines[i], Symbol, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		public static void SetEnabled(NamedBuildTarget target, bool enabled)
		{
			List<string> defines = new List<string>(Read(target));
			int existing = defines.IndexOf(Symbol);

			if (enabled)
			{
				if (existing >= 0)
				{
					return;
				}

				defines.Add(Symbol);
			}
			else
			{
				if (existing < 0)
				{
					return;
				}

				defines.RemoveAt(existing);
			}

			PlayerSettings.SetScriptingDefineSymbols(target, defines.ToArray());
		}

		public static IReadOnlyList<NamedBuildTarget> GetKnownTargets()
		{
			if (_knownTargets != null)
			{
				return _knownTargets;
			}

			_knownTargets = new List<NamedBuildTarget>();
			HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
			foreach (FieldInfo field in typeof(BuildTargetGroup).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				if (field.IsDefined(typeof(ObsoleteAttribute), inherit: false))
				{
					continue;
				}

				BuildTargetGroup group = (BuildTargetGroup)field.GetValue(null);
				if (group == BuildTargetGroup.Unknown)
				{
					continue;
				}

				NamedBuildTarget target;
				try
				{
					target = NamedBuildTarget.FromBuildTargetGroup(group);
				}
				catch (Exception)
				{
					continue;
				}

				if (string.IsNullOrEmpty(target.TargetName) || !seen.Add(target.TargetName))
				{
					continue;
				}

				_knownTargets.Add(target);
			}

			return _knownTargets;
		}

		public static NamedBuildTarget ResolveTarget(BuildReport report)
		{
			BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(report.summary.platform);

			return group == BuildTargetGroup.Unknown
				? ActiveTarget
				: NamedBuildTarget.FromBuildTargetGroup(group);
		}

		private static string[] Read(NamedBuildTarget target)
		{
			string raw = PlayerSettings.GetScriptingDefineSymbols(target);
			if (string.IsNullOrWhiteSpace(raw))
			{
				return Array.Empty<string>();
			}

			string[] defines = raw.Split(_defineSeparators, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < defines.Length; i++)
			{
				defines[i] = defines[i].Trim();
			}

			return defines;
		}
	}
}