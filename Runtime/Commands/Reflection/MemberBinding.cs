using System.Reflection;

namespace DTech.OmniDebugger
{
	internal readonly struct MemberBinding
	{
		public readonly MemberInfo Member;
		public readonly CommandDefinition Definition;
		public readonly string SkipReason;

		public bool IsValid => SkipReason == null && Definition != null;

		private MemberBinding(MemberInfo member, CommandDefinition definition, string skipReason)
		{
			Member = member;
			Definition = definition;
			SkipReason = skipReason;
		}

		public static MemberBinding Valid(MemberInfo member, CommandDefinition definition) =>
			new MemberBinding(member, definition, null);

		public static MemberBinding Skipped(MemberInfo member, string skipReason) =>
			new MemberBinding(member, null, skipReason);
	}
}