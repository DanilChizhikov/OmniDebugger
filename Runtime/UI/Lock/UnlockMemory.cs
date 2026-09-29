using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal static class UnlockMemory
	{
		private static string _sessionHash;

		public static bool IsUnlocked(OmniDebuggerLockOptions options)
		{
			string hash = options.SecretHash;

			switch (options.UnlockScope)
			{
				case OmniDebuggerUnlockScope.Session:
					return LockSecret.Matches(_sessionHash, hash);
				case OmniDebuggerUnlockScope.Device:
					return LockSecret.Matches(_sessionHash, hash) ||
						LockSecret.Matches(PlayerPrefs.GetString(ViewStateKeys.Unlocked, string.Empty), hash);
				default:
					return false;
			}
		}

		public static void Remember(OmniDebuggerLockOptions options)
		{
			if (options.UnlockScope == OmniDebuggerUnlockScope.EveryOpen)
			{
				return;
			}

			_sessionHash = options.SecretHash;

			if (options.UnlockScope == OmniDebuggerUnlockScope.Device)
			{
				PlayerPrefs.SetString(ViewStateKeys.Unlocked, options.SecretHash ?? string.Empty);
				PlayerPrefs.Save();
			}
		}

		public static void Forget()
		{
			_sessionHash = null;
			PlayerPrefs.DeleteKey(ViewStateKeys.Unlocked);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetSession() => _sessionHash = null;
	}
}