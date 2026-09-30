using System.Globalization;

namespace DTech.OmniDebugger
{
	internal static class InfoFormat
	{
		private const float BytesPerMegabyte = 1024.0f * 1024.0f;

		public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

		public static string Number(float value, string format = "0.0") => value.ToString(format, CultureInfo.InvariantCulture);

		public static float Megabytes(long bytes) => bytes / BytesPerMegabyte;

		public static string MegabytesText(long bytes) => Number(Megabytes(bytes)) + " MB";

		public static string YesNo(bool value) => value ? "Yes" : "No";
	}
}
