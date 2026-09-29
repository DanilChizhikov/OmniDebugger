using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class WindowsTab : IOmniDebuggerTab
	{
		private const string EmptyMessage = "No windows registered. Pin a command, or register one with IOmniDebuggerHost.Windows.";
		private const string Caption = "Windows float over the game while the panel is closed or floating.";
		private const string HideAllLabel = "Hide all";
		private const string ScaleLabel = "Window scale";
		private const string ScaleFormat = "×0.0";

		private readonly VisualElement _root;
		private readonly ScrollView _scroll;
		private readonly IWindowRegistry _windows;
		private readonly WindowRegistry _registry;

		public VisualElement Root => _root;

		private Slider _scaleSlider;
		private Label _scaleValue;
		private bool _disposed;

		public WindowsTab(IWindowRegistry windows)
		{
			_windows = windows;
			_registry = windows as WindowRegistry;
			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);
			_scroll = UiBuild.Scroll();
			_root.Add(_scroll);

			_windows.OnChanged += Rebuild;

			if (_registry != null)
			{
				_registry.OnScaleChanged += ShowScale;
			}

			Rebuild();
		}

		public void OnOpen()
		{
		}

		public void OnClose()
		{
		}

		public void Refresh() => Rebuild();

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_windows.OnChanged -= Rebuild;

			if (_registry != null)
			{
				_registry.OnScaleChanged -= ShowScale;
			}

			_root.RemoveFromHierarchy();
		}

		private static VisualElement CreateRow(IOmniDebuggerWindow window, bool first)
		{
			VisualElement row = UiBuild.Element(OmniDebuggerUiClasses.WindowRow);
			row.EnableInClassList(OmniDebuggerUiClasses.First, first);
			row.AddToClassList(OmniDebuggerUiClasses.Tappable);
			row.Add(UiBuild.Label(window.Title, OmniDebuggerUiClasses.WindowRowTitle));

			SwitchField toggle = new SwitchField();
			toggle.SetValueWithoutNotify(window.IsOpen);
			toggle.RegisterValueChangedCallback(evt => SetOpen(window, evt.newValue));

			VisualElement control = UiBuild.Element(OmniDebuggerUiClasses.WindowRowControl);
			control.Add(toggle);
			row.Add(control);

			row.RegisterCallback<NavigationSubmitEvent>(evt =>
			{
				if (evt.target == row)
				{
					toggle.value = !toggle.value;
				}
			});

			return row;
		}

		private static void SetOpen(IOmniDebuggerWindow window, bool open)
		{
			if (open)
			{
				window.Open();
			}
			else
			{
				window.Close();
			}
		}

		private void Rebuild()
		{
			if (_disposed)
			{
				return;
			}

			_scroll.Clear();
			_scaleSlider = null;
			_scaleValue = null;

			IReadOnlyList<IOmniDebuggerWindow> windows = _windows.All;

			if (windows.Count == 0)
			{
				_scroll.Add(UiBuild.Label(EmptyMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			VisualElement header = UiBuild.Element(OmniDebuggerUiClasses.PageHeader);
			header.Add(UiBuild.Label(Caption, OmniDebuggerUiClasses.PageHeaderCaption));
			header.Add(UiBuild.TextButton(HideAllLabel, _windows.CloseAll));
			_scroll.Add(header);

			if (_registry != null)
			{
				_scroll.Add(CreateScaleCard());
			}

			VisualElement list = UiBuild.Element(OmniDebuggerUiClasses.Card);
			list.AddToClassList(OmniDebuggerUiClasses.RowList);
			_scroll.Add(list);

			for (int i = 0; i < windows.Count; i++)
			{
				list.Add(CreateRow(windows[i], i == 0));
			}
		}

		private VisualElement CreateScaleCard()
		{
			VisualElement card = UiBuild.Element(OmniDebuggerUiClasses.Card);
			card.AddToClassList(OmniDebuggerUiClasses.RowList);
			card.AddToClassList(OmniDebuggerUiClasses.WindowScale);

			VisualElement row = UiBuild.Element(OmniDebuggerUiClasses.WindowRow);
			row.AddToClassList(OmniDebuggerUiClasses.WindowRowScale);
			row.AddToClassList(OmniDebuggerUiClasses.First);
			row.Add(UiBuild.Label(ScaleLabel, OmniDebuggerUiClasses.WindowRowTitle));

			VisualElement range = UiBuild.Element(OmniDebuggerUiClasses.Range);
			_scaleSlider = new Slider(WindowRegistry.MinScale, WindowRegistry.MaxScale) { fill = true };
			_scaleSlider.AddToClassList(OmniDebuggerUiClasses.RangeSlider);
			_scaleSlider.RegisterValueChangedCallback(OnScaleSliderChanged);
			range.Add(_scaleSlider);
			_scaleValue = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.WindowScaleValue);
			range.Add(_scaleValue);
			row.Add(range);

			card.Add(row);
			ShowScale();
			return card;
		}

		private void OnScaleSliderChanged(ChangeEvent<float> evt)
		{
			_registry.SetScale(evt.newValue);
			ShowScale();
		}

		private void ShowScale()
		{
			if (_scaleSlider == null)
			{
				return;
			}

			_scaleSlider.SetValueWithoutNotify(_registry.Scale);
			_scaleValue.text = _registry.Scale.ToString(ScaleFormat, CultureInfo.InvariantCulture);
		}
	}
}
