using System;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class UnityLogSink : ILogSink
	{
		public const string Tag = "[OmniDebugger]";

		public static readonly UnityLogSink Default = new UnityLogSink();

		[HideInCallstack]
		public void Info(string message) => Debug.Log($"{Tag} {message}");

		[HideInCallstack]
		public void Warning(string message) => Debug.LogWarning($"{Tag} {message}");

		[HideInCallstack]
		public void Error(string message) => Debug.LogError($"{Tag} {message}");

		[HideInCallstack]
		public void Exception(string message, Exception exception) =>
			Debug.LogError($"{Tag} {message}{Environment.NewLine}{exception}");
	}
}