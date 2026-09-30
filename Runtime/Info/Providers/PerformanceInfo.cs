using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DTech.OmniDebugger
{
	internal sealed class PerformanceInfo : IInfoProvider
	{
		public string Title => "Performance";

		public int Order => 0;

		public void Describe(IInfoSection section)
		{
			section.Graph("FPS", SampleFps, 0.0f, TargetFps(), "fps");
			section.Live("Frame time", () => InfoFormat.Number(Time.smoothDeltaTime * 1000.0f) + " ms");
			section.Live("Target FPS", () => InfoFormat.Number(Application.targetFrameRate));
			section.Live("VSync", () => InfoFormat.Number(QualitySettings.vSyncCount));
			section.Live("Time scale", () => InfoFormat.Number(Time.timeScale, "0.##"));
			section.Live("Scene", () => SceneManager.GetActiveScene().name);
			section.Live("Uptime", () => TimeSpan.FromSeconds(Time.realtimeSinceStartup).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
		}

		private static float SampleFps()
		{
			float delta = Time.unscaledDeltaTime;
			return delta > 0.0f ? 1.0f / delta : float.NaN;
		}

		private static float TargetFps()
		{
			if (Application.targetFrameRate > 0)
			{
				return Application.targetFrameRate * 1.25f;
			}

			double refresh = Screen.currentResolution.refreshRateRatio.value;
			return refresh > 0.0 ? (float)refresh * 1.25f : 0.0f;
		}
	}
}
