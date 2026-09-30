using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class DeviceInfo : IInfoProvider
	{
		public string Title => "Device";

		public int Order => 60;

		public void Describe(IInfoSection section)
		{
			section.Text("Model", SystemInfo.deviceModel);
			section.Text("OS", SystemInfo.operatingSystem);
			section.Text("CPU", SystemInfo.processorType);
			section.Text("Cores", InfoFormat.Number(SystemInfo.processorCount));
			section.Live("Battery", DescribeBattery);
			section.Live("Network", () => Application.internetReachability.ToString());
		}

		private static string DescribeBattery()
		{
			float level = SystemInfo.batteryLevel;
			return level < 0.0f ? null : $"{Mathf.RoundToInt(level * 100.0f)}% ({SystemInfo.batteryStatus})";
		}
	}
}
