using UnityEngine;
using UnityEngine.Rendering;

namespace DTech.OmniDebugger
{
	internal sealed class QualityInfo : IInfoProvider
	{
		private const string BuiltIn = "Built-in";

		public string Title => "Quality";

		public int Order => 30;

		public void Describe(IInfoSection section)
		{
			section.Live("Level", DescribeLevel);
			section.Live("Render pipeline", () =>
				GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : BuiltIn);
			section.Live("Anti-aliasing", () => QualitySettings.antiAliasing > 0 ? "×" + InfoFormat.Number(QualitySettings.antiAliasing) : "Off");
			section.Live("Shadows", () => QualitySettings.shadows.ToString());
			section.Live("Shadow distance", () => InfoFormat.Number(QualitySettings.shadowDistance, "0"));
			section.Live("LOD bias", () => InfoFormat.Number(QualitySettings.lodBias, "0.##"));
			section.Live("Texture mip limit", () => InfoFormat.Number(QualitySettings.globalTextureMipmapLimit));
			section.Live("Anisotropic", () => QualitySettings.anisotropicFiltering.ToString());
			section.Live("Pixel lights", () => InfoFormat.Number(QualitySettings.pixelLightCount));
		}

		private static string DescribeLevel()
		{
			int level = QualitySettings.GetQualityLevel();
			string[] names = QualitySettings.names;
			return level >= 0 && level < names.Length ? names[level] : InfoFormat.Number(level);
		}
	}
}
