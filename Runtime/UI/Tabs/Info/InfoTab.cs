using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class InfoTab : IOmniDebuggerTab
	{
		private const long RedrawMs = 500;
		private const float BytesPerMegabyte = 1024.0f * 1024.0f;
		private const string Unknown = "—";

		private readonly VisualElement _root;
		private readonly IVisualElementScheduledItem _sampler;
		private readonly IVisualElementScheduledItem _redraw;

		public VisualElement Root => _root;

		private Label _scene;
		private Label _resolution;
		private Label _orientation;
		private Label _safeArea;
		private Label _fps;
		private Label _frameTime;
		private Label _memory;
		private Label _uptime;
		private Label _network;
		private Label _battery;

		private float _frameSum;
		private int _frameCount;
		private bool _disposed;

		public InfoTab()
		{
			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);

			ScrollView scroll = UiBuild.Scroll();
			_root.Add(scroll);

			ResponsiveGrid grid = new ResponsiveGrid();
			scroll.Add(grid);

			grid.AddCell(BuildSection());
			grid.AddCell(ApplicationSection());
			grid.AddCell(DisplaySection());
			grid.AddCell(DeviceSection());
			grid.AddCell(RuntimeSection());

			_sampler = _root.schedule.Execute(Sample).Every(0);
			_redraw = _root.schedule.Execute(Redraw).Every(RedrawMs);
			_sampler.Pause();
			_redraw.Pause();
		}

		public void OnOpen()
		{
			_frameSum = 0.0f;
			_frameCount = 0;
			_sampler.Resume();
			_redraw.Resume();
		}

		public void OnClose()
		{
			_sampler.Pause();
			_redraw.Pause();
		}

		public void Refresh()
		{
			if (!_disposed)
			{
				Redraw();
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_sampler.Pause();
			_redraw.Pause();
			_root.RemoveFromHierarchy();
		}

		private static VisualElement BuildSection()
		{
			VisualElement section = Section("Build");
			Row(section, "Version", Application.version);
			Row(section, "Build number", ReadBuildNumber());
			Row(section, "Build type", Debug.isDebugBuild ? "Development" : "Release");
			Row(section, "Unity", Application.unityVersion);
			Row(section, "Identifier", Application.identifier);
			return section;
		}

		private static VisualElement ApplicationSection()
		{
			VisualElement section = Section("Application");
			Row(section, "Product", Application.productName);
			Row(section, "Company", Application.companyName);
			Row(section, "Platform", Application.platform.ToString());
			Row(section, "Language", Application.systemLanguage.ToString());
			Row(section, "Target FPS", Application.targetFrameRate.ToString(CultureInfo.InvariantCulture));
			Row(section, "VSync", QualitySettings.vSyncCount.ToString(CultureInfo.InvariantCulture));
			return section;
		}

		private static VisualElement DeviceSection()
		{
			VisualElement section = Section("Device");
			Row(section, "Model", SystemInfo.deviceModel);
			Row(section, "OS", SystemInfo.operatingSystem);
			Row(section, "CPU", SystemInfo.processorType);
			Row(section, "Cores", SystemInfo.processorCount.ToString(CultureInfo.InvariantCulture));
			Row(section, "RAM", $"{SystemInfo.systemMemorySize} MB");
			Row(section, "GPU", SystemInfo.graphicsDeviceName);
			Row(section, "Graphics API", SystemInfo.graphicsDeviceType.ToString());
			Row(section, "VRAM", $"{SystemInfo.graphicsMemorySize} MB");
			Row(section, "Max texture", SystemInfo.maxTextureSize.ToString(CultureInfo.InvariantCulture));
			return section;
		}

		private static string DescribeBattery()
		{
			float level = SystemInfo.batteryLevel;

			if (level < 0.0f)
			{
				return Unknown;
			}

			return $"{Mathf.RoundToInt(level * 100.0f)}% ({SystemInfo.batteryStatus})";
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
					return info.Get<int>("versionCode").ToString(CultureInfo.InvariantCulture);
				}
			}
			catch (Exception)
			{
				return Unknown;
			}
#else
			return Application.isEditor ? "Editor" : Unknown;
#endif
		}

		private static VisualElement Section(string title)
		{
			VisualElement section = UiBuild.Element(OmniDebuggerUiClasses.Section);
			section.Add(UiBuild.Label(title.ToUpperInvariant(), OmniDebuggerUiClasses.SectionTitle));
			return section;
		}

		private static Label Row(VisualElement section, string key, string value)
		{
			VisualElement row = UiBuild.Element(OmniDebuggerUiClasses.KeyValueRow);
			row.EnableInClassList(OmniDebuggerUiClasses.First, section.childCount == 1);
			row.Add(UiBuild.Label(key, OmniDebuggerUiClasses.KeyValueKey));

			Label label = UiBuild.Label(string.IsNullOrEmpty(value) ? Unknown : value, OmniDebuggerUiClasses.KeyValueValue);
			row.Add(label);
			section.Add(row);
			return label;
		}

		private VisualElement DisplaySection()
		{
			VisualElement section = Section("Display");
			_resolution = Row(section, "Resolution", Unknown);
			Row(section, "DPI", Screen.dpi > 0.0f ? Screen.dpi.ToString("0", CultureInfo.InvariantCulture) : Unknown);
			_orientation = Row(section, "Orientation", Unknown);
			_safeArea = Row(section, "Safe area", Unknown);
			Row(section, "Touch", InputBackends.Current.IsTouchSupported ? "Supported" : "Not supported");
			return section;
		}

		private VisualElement RuntimeSection()
		{
			VisualElement section = Section("Runtime");
			_scene = Row(section, "Scene", Unknown);
			_fps = Row(section, "FPS", Unknown);
			_frameTime = Row(section, "Frame time", Unknown);
			_memory = Row(section, "Allocated memory", Unknown);
			_uptime = Row(section, "Uptime", Unknown);
			_network = Row(section, "Network", Unknown);
			_battery = Row(section, "Battery", Unknown);
			return section;
		}

		private void Sample()
		{
			_frameSum += Time.unscaledDeltaTime;
			_frameCount++;
		}

		private void Redraw()
		{
			_scene.text = SceneManager.GetActiveScene().name;
			_resolution.text = $"{Screen.width} x {Screen.height}";
			_orientation.text = Screen.orientation.ToString();

			Rect safe = Screen.safeArea;
			_safeArea.text = $"{safe.x:0}, {safe.y:0}, {safe.width:0} x {safe.height:0}";

			if (_frameCount > 0 && _frameSum > 0.0f)
			{
				float average = _frameSum / _frameCount;
				_fps.text = (1.0f / average).ToString("0.0", CultureInfo.InvariantCulture);
				_frameTime.text = $"{(average * 1000.0f).ToString("0.0", CultureInfo.InvariantCulture)} ms";
				_frameSum = 0.0f;
				_frameCount = 0;
			}

			_memory.text = $"{(Profiler.GetTotalAllocatedMemoryLong() / BytesPerMegabyte).ToString("0.0", CultureInfo.InvariantCulture)} MB";
			_uptime.text = TimeSpan.FromSeconds(Time.realtimeSinceStartup).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
			_network.text = Application.internetReachability.ToString();
			_battery.text = DescribeBattery();
		}
	}
}