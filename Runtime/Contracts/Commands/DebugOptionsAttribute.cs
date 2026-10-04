using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Lets the panel edit a command property or parameter with a dropdown of values taken from another member
	/// of the same type: <c>[DebugOptions(nameof(LevelIds))] string id</c>. The member can be a property, a
	/// field or a parameterless method, instance or static, that yields an <c>IEnumerable&lt;T&gt;</c> — or a
	/// <c>Func&lt;IEnumerable&lt;T&gt;&gt;</c> that does — where <c>T</c> converts to the argument's type. It
	/// is read every time the dropdown opens, so the list follows the game; a <c>null</c> result shows no
	/// options. Checked at compile time by the source generator.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
	public sealed class DebugOptionsAttribute : Attribute
	{
		/// <summary>Name of the member the options come from. Use <c>nameof</c>.</summary>
		public string MemberName { get; }

		public DebugOptionsAttribute(string memberName)
		{
			if (string.IsNullOrWhiteSpace(memberName))
			{
				throw new ArgumentException("Member name cannot be null or whitespace.", nameof(memberName));
			}

			MemberName = memberName;
		}
	}
}
