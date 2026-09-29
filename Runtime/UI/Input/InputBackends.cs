using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal static class InputBackends
	{
		public static IInputBackend Current => _current ??= Create();

		private static IInputBackend _current;

		private static IInputBackend Create()
		{
#if ENABLE_INPUT_SYSTEM && OMNI_DEBUGGER_INPUT_SYSTEM
			return new InputSystemBackend();
#elif ENABLE_LEGACY_INPUT_MANAGER
			return new LegacyInputBackend();
#else
			UnityLogSink.Default.Warning(
				"No input backend is available, so keyboard shortcuts are off. " +
				"Install com.unity.inputsystem, or set Active Input Handling to Input Manager (Old) or Both.");
			return new NullInputBackend();
#endif
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset() => _current = null;
	}
}