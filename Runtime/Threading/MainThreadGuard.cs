using System;
using System.Threading;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal static class MainThreadGuard
	{
		private static int _mainThreadId = Thread.CurrentThread.ManagedThreadId;

		private static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
		
		public static void Verify(string operation)
		{
			if (IsMainThread)
			{
				return;
			}

			throw new InvalidOperationException(
				$"{UnityLogSink.Tag} '{operation}' must be called from the Unity main thread, " +
				$"but was called from thread {Thread.CurrentThread.ManagedThreadId}.");
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Capture() => _mainThreadId = Thread.CurrentThread.ManagedThreadId;
	}
}