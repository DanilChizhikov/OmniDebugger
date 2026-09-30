using System;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal sealed class BuildInfo : IInfoProvider
	{
		public string Title => "Build";

		public int Order => 50;

		public void Describe(IInfoSection section)
		{
			section.Text("Version", Application.version);
			section.Text("Build number", ReadBuildNumber());
			section.Text("Build type", Debug.isDebugBuild ? "Development" : "Release");
			section.Text("Unity", Application.unityVersion);
			section.Text("Identifier", Application.identifier);
			section.Text("Product", Application.productName);
			section.Text("Company", Application.companyName);
			section.Text("Platform", Application.platform.ToString());
			section.Text("Language", Application.systemLanguage.ToString());
		}

		private static string ReadBuildNumber()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
				using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
				using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getPackageManager"))
				using (AndroidJavaObject info = manager.Call<AndroidJavaObject>("getPackageInfo", Application.identifier, 0))
				{
					return InfoFormat.Number(info.Get<int>("versionCode"));
				}
			}
			catch (Exception)
			{
				return null;
			}
#else
			return Application.isEditor ? "Editor" : null;
#endif
		}
	}
}
