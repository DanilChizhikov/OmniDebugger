using System;

namespace DTech.OmniDebugger.UI
{
	internal sealed class BoolArgumentFieldHandler : IArgumentFieldHandler
	{
		public int Priority => 0;

		public bool CanHandle(Type valueType) => valueType == typeof(bool);

		public IArgumentField Create(in ArgumentFieldRequest request) => new BoolArgumentField(request);
	}
}