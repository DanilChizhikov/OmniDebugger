using System;

namespace DTech.OmniDebugger.UI
{
	internal sealed class EnumArgumentFieldHandler : IArgumentFieldHandler
	{
		public int Priority => 0;

		public bool CanHandle(Type valueType) => valueType.IsEnum;

		public IArgumentField Create(in ArgumentFieldRequest request) => new EnumArgumentField(request);
	}
}