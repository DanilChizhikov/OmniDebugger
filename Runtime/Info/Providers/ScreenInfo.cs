using UnityEngine;
using DTech.OmniDebugger.UI;

namespace DTech.OmniDebugger
{
	internal sealed class ScreenInfo : IInfoProvider
	{
		public string Title => "Screen";

		public int Order => 40;

		public void Describe(IInfoSection section)
		{
			section.Live("Resolution", () => $"{Screen.width} × {Screen.height}");
			section.Live("Refresh rate", () => InfoFormat.Number((float)Screen.currentResolution.refreshRateRatio.value, "0.#") + " Hz");
			section.Live("DPI", () => Screen.dpi > 0.0f ? InfoFormat.Number(Screen.dpi, "0") : null);
			section.Live("Orientation", () => Screen.orientation.ToString());
			section.Live("Safe area", DescribeSafeArea);
			section.Live("Mode", () => Screen.fullScreenMode.ToString());
			section.Text("Touch", InputBackends.Current.IsTouchSupported ? "Supported" : "Not supported");
		}

		private static string DescribeSafeArea()
		{
			Rect safe = Screen.safeArea;
			return $"{InfoFormat.Number(safe.x, "0")}, {InfoFormat.Number(safe.y, "0")}, " +
				$"{InfoFormat.Number(safe.width, "0")} × {InfoFormat.Number(safe.height, "0")}";
		}
	}
}
