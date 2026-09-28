using System;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal static class OmniDebuggerViews
	{
		public static event Action OnCurrentChanged;

		public static IOmniDebugger Current { get; private set; }

		public static void Register(IOmniDebugger debugger)
		{
			MainThreadGuard.Verify(nameof(Register));

			if (debugger == null)
			{
				throw new ArgumentNullException(nameof(debugger));
			}

			if (ReferenceEquals(Current, debugger))
			{
				return;
			}

			Current = debugger;
			OnCurrentChanged?.Invoke();
		}

		public static bool Unregister(IOmniDebugger debugger)
		{
			MainThreadGuard.Verify(nameof(Unregister));

			if (debugger == null)
			{
				throw new ArgumentNullException(nameof(debugger));
			}

			if (!ReferenceEquals(Current, debugger))
			{
				return false;
			}

			Current = null;
			OnCurrentChanged?.Invoke();
			return true;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			if (Current == null)
			{
				return;
			}

			Current = null;
			OnCurrentChanged?.Invoke();
		}
	}
}