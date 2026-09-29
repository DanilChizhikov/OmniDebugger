using System;

namespace DTech.OmniDebugger.UI
{
	internal sealed class TextArgumentFieldHandler : IArgumentFieldHandler
	{
		public int Priority => 0;

		public bool CanHandle(Type valueType) =>
			valueType == typeof(string) ||
			valueType == typeof(char) ||
			typeof(IConvertible).IsAssignableFrom(valueType);

		public IArgumentField Create(in ArgumentFieldRequest request) => new TextArgumentField(request);
	}
}