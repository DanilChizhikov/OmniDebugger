#if OMNI_DEBUGGER
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI.Editor
{
	internal sealed class OmniDebuggerWindow : EditorWindow
	{
		private const string MenuPath = "Window/DTech/OmniDebugger";
		private const string WindowTitle = "OmniDebugger";
		private const string Origin = "Editor Window";
		private const string WaitingMessage =
			"No live debugger.\n\nEnter play mode with code that constructs an OmniDebugger; " +
			"the newest one shows up here on its own.";

		private readonly OmniDebuggerViewState _state = new ();

		private VisualElement _host;
		private Label _waiting;
		private OmniDebuggerView _view;
		private IOmniDebugger _bound;

		[MenuItem(MenuPath)]
		private static void Open()
		{
			OmniDebuggerWindow window = GetWindow<OmniDebuggerWindow>();
			window.titleContent = new GUIContent(WindowTitle);
			window.minSize = new Vector2(360.0f, 240.0f);
			window.Show();
		}

		private void OnEnable() => OmniDebuggerViews.OnCurrentChanged += OnCurrentChanged;

		private void OnDisable()
		{
			OmniDebuggerViews.OnCurrentChanged -= OnCurrentChanged;
			ReleaseView();
		}

		private void CreateGUI()
		{
			_host = new VisualElement();
			_host.style.flexGrow = 1.0f;
			rootVisualElement.Add(_host);

			_waiting = new Label(WaitingMessage);
			_waiting.style.flexGrow = 1.0f;
			_waiting.style.paddingLeft = 12.0f;
			_waiting.style.paddingTop = 12.0f;
			_waiting.style.whiteSpace = WhiteSpace.Normal;
			rootVisualElement.Add(_waiting);

			Bind();
		}

		private void OnCurrentChanged()
		{
			if (_host == null)
			{
				return;
			}

			Bind();
		}

		private void Bind()
		{
			IOmniDebugger debugger = OmniDebuggerViews.Current;

			if (ReferenceEquals(debugger, _bound) && (_view != null || debugger == null))
			{
				return;
			}

			ReleaseView();

			if (debugger == null)
			{
				_waiting.style.display = DisplayStyle.Flex;
				return;
			}

			OmniDebuggerViewSettings settings = new OmniDebuggerViewSettings(
				_host,
				debugger,
				_state,
				EditorPrefsViewPrefs.Default,
				origin: Origin,
				showCloseButton: false,
				startOpen: true);

			try
			{
				_view = new OmniDebuggerView(settings);
			}
			catch (ObjectDisposedException)
			{
				_waiting.style.display = DisplayStyle.Flex;
				return;
			}

			_waiting.style.display = DisplayStyle.None;
			_bound = debugger;
		}

		private void ReleaseView()
		{
			_view?.Dispose();
			_view = null;
			_bound = null;
		}
	}
}
#endif