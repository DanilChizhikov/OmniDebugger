using System;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class LogCapture : IDisposable
	{
		private readonly LogStore _store;

		private bool _disposed;

		public LogCapture(LogStore store)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
			Application.logMessageReceivedThreaded += OnLogMessageReceived;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			Application.logMessageReceivedThreaded -= OnLogMessageReceived;
		}

		private void OnLogMessageReceived(string message, string stackTrace, LogType type) =>
			_store.Add(message, stackTrace, type, DateTime.UtcNow);
	}
}