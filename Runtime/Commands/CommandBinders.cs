using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The table <see cref="ICommandRegistry.Register(object)"/> reads: for every type that declares
	/// <see cref="DebugCommandAttribute"/> members, the code the source generator wrote to turn an instance of
	/// it into commands. Filled by that generated code as the domain loads; not meant to be called by hand.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static class CommandBinders
	{
		private static readonly Dictionary<Type, Action<object, ICollection<DebugCommand>>> _binders = new ();
		private static readonly object _gate = new ();

		/// <summary>Records how commands are made for instances of <paramref name="type"/>. Idempotent.</summary>
		public static void Add(Type type, Action<object, ICollection<DebugCommand>> bind)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			if (bind == null)
			{
				throw new ArgumentNullException(nameof(bind));
			}

			lock (_gate)
			{
				_binders[type] = bind;
			}
		}

		internal static bool TryGet(Type type, out Action<object, ICollection<DebugCommand>> bind)
		{
			lock (_gate)
			{
				return _binders.TryGetValue(type, out bind);
			}
		}

		internal static bool Collect(object target, ICollection<DebugCommand> results)
		{
			bool found = false;

			for (Type type = target.GetType(); type != null && type != typeof(object); type = type.BaseType)
			{
				if (!TryGet(type, out Action<object, ICollection<DebugCommand>> bind))
				{
					continue;
				}

				bind(target, results);
				found = true;
			}

			return found;
		}
	}
}
