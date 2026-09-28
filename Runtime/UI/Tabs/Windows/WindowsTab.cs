using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class WindowsTab : IOmniDebuggerTab
	{
		private const string EmptyMessage = "No windows registered. Pin a command, or register one with IOmniDebugger.Windows.";

		private readonly VisualElement _root;
		private readonly ScrollView _scroll;
		private readonly IWindowRegistry _windows;

		public VisualElement Root => _root;

		private bool _disposed;

		public WindowsTab(IWindowRegistry windows)
		{
			_windows = windows;
			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);
			_scroll = UiBuild.Scroll();
			_root.Add(_scroll);

			_windows.OnChanged += Rebuild;
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
			_root.RemoveFromHierarchy();
		}

		private static VisualElement CreateRow(IOmniDebuggerWindow window)
		{
			VisualElement row = UiBuild.Element(OmniDebuggerUiClasses.Card);
			row.AddToClassList(OmniDebuggerUiClasses.WindowRow);
			row.Add(UiBuild.Label(window.Title, OmniDebuggerUiClasses.WindowRowTitle));

			Button toggle = UiBuild.TextButton(
				window.IsOpen ? "Shown" : "Hidden",
				() => Toggle(window));

			toggle.EnableInClassList(OmniDebuggerUiClasses.ButtonPrimary, window.IsOpen);
			row.Add(toggle);
			return row;
		}

		private static void Toggle(IOmniDebuggerWindow window)
		{
			if (window.IsOpen)
			{
				window.Close();
			}
			else
			{
				window.Open();
			}
		}

		private void Rebuild()
		{
			if (_disposed)
			{
				return;
			}

			_scroll.Clear();

			IReadOnlyList<IOmniDebuggerWindow> windows = _windows.All;

			if (windows.Count == 0)
			{
				_scroll.Add(UiBuild.Label(EmptyMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			VisualElement header = UiBuild.Element(OmniDebuggerUiClasses.PageHeader);
			Label caption = UiBuild.Label("Windows float over the game while the panel is closed.", OmniDebuggerUiClasses.CommandMeta);
			caption.style.flexGrow = 1.0f;
			caption.style.flexShrink = 1.0f;
			header.Add(caption);
			header.Add(UiBuild.TextButton("Hide all", _windows.CloseAll));
			_scroll.Add(header);

			ResponsiveGrid grid = new ResponsiveGrid();
			_scroll.Add(grid);

			for (int i = 0; i < windows.Count; i++)
			{
				grid.AddCell(CreateRow(windows[i]));
			}
		}
	}
}