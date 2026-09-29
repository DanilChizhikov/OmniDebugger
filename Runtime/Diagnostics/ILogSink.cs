using System;

namespace DTech.OmniDebugger
{
	internal interface ILogSink
	{
		void Info(string message);
		void Warning(string message);
		void Error(string message);
		void Exception(string message, Exception exception);
	}
}