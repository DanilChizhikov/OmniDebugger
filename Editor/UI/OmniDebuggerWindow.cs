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

		private VisualElement _viewport;
		private VisualElement _canvas;
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

		private void OnEnable()
		{
			OmniDebuggerViews.OnCurrentChanged += OnCurrentChanged;
			EditorWindowPrefs.OnChanged += OnPrefsChanged;
		}

		private void OnDisable()
		{
			OmniDebuggerViews.OnCurrentChanged -= OnCurrentChanged;
			EditorWindowPrefs.OnChanged -= OnPrefsChanged;
			ReleaseView();
		}

		private void CreateGUI()
		{
			_viewport = new VisualElement { pickingMode = PickingMode.Ignore };
			_viewport.style.flexGrow = 1.0f;
			_viewport.style.overflow = Overflow.Hidden;
			_viewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
			rootVisualElement.Add(_viewport);

			_canvas = new VisualElement { pickingMode = PickingMode.Ignore };
			_canvas.style.position = Position.Absolute;
			_canvas.style.left = 0.0f;
			_canvas.style.top = 0.0f;
			_canvas.style.transformOrigin = new TransformOrigin(Length.Percent(0.0f), Length.Percent(0.0f));
			_viewport.Add(_canvas);

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
			if (_canvas == null)
			{
				return;
			}

			Bind();
		}

		private void OnPrefsChanged()
		{
			UpdateCanvasScale();
			_view?.SetOrientation(EditorWindowPrefs.Orientation);
		}

		private void OnViewportGeometryChanged(GeometryChangedEvent evt) => UpdateCanvasScale();

		private void UpdateCanvasScale()
		{
			if (_viewport == null || _canvas == null)
			{
				return;
			}

			float width = _viewport.resolvedStyle.width;
			float height = _viewport.resolvedStyle.height;

			if (!(width > 0.0f) || !(height > 0.0f) || float.IsInfinity(width) || float.IsInfinity(height))
			{
				return;
			}

			float scale = PanelScaling.ResolveFitScale(width, height) * EditorWindowPrefs.Zoom;

			_canvas.style.width = width / scale;
			_canvas.style.height = height / scale;
			_canvas.style.scale = new Scale(new Vector3(scale, scale, 1.0f));
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
				SetWaiting(true);
				return;
			}

			OmniDebuggerViewSettings settings = new OmniDebuggerViewSettings(
				_canvas,
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
				SetWaiting(true);
				return;
			}

			_view.SetOrientation(EditorWindowPrefs.Orientation);
			SetWaiting(false);
			_bound = debugger;
		}

		private void SetWaiting(bool waiting)
		{
			_waiting.style.display = waiting ? DisplayStyle.Flex : DisplayStyle.None;
			_viewport.style.display = waiting ? DisplayStyle.None : DisplayStyle.Flex;
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
