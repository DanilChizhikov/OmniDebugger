using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	internal sealed class RecordingLogSink : ILogSink
	{
		public List<string> Infos { get; } = new List<string>();
		public List<string> Warnings { get; } = new List<string>();
		public List<string> Errors { get; } = new List<string>();
		public List<Exception> Exceptions { get; } = new List<Exception>();

		public void Info(string message) => Infos.Add(message);

		public void Warning(string message) => Warnings.Add(message);

		public void Error(string message) => Errors.Add(message);

		public void Exception(string message, Exception exception)
		{
			Errors.Add(message);
			Exceptions.Add(exception);
		}

		public void Clear()
		{
			Infos.Clear();
			Warnings.Clear();
			Errors.Clear();
			Exceptions.Clear();
		}
	}
}