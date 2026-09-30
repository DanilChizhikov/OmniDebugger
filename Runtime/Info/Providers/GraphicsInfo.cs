using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class GraphicsInfo : IInfoProvider
	{
		public string Title => "Graphics";

		public int Order => 20;

		public void Describe(IInfoSection section)
		{
			section.Text("GPU", SystemInfo.graphicsDeviceName);
			section.Text("Vendor", SystemInfo.graphicsDeviceVendor);
			section.Text("API", SystemInfo.graphicsDeviceType.ToString());
			section.Text("Version", SystemInfo.graphicsDeviceVersion);
			section.Text("VRAM", SystemInfo.graphicsMemorySize + " MB");
			section.Text("Shader level", InfoFormat.Number(SystemInfo.graphicsShaderLevel));
			section.Text("Max texture", InfoFormat.Number(SystemInfo.maxTextureSize));
			section.Text("Multithreaded", InfoFormat.YesNo(SystemInfo.graphicsMultiThreaded));
			section.Text("Compute shaders", InfoFormat.YesNo(SystemInfo.supportsComputeShaders));
		}
	}
}
