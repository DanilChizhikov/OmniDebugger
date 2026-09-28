using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Puts the panel on screen in a running game. <see cref="DTech.OmniDebugger.OmniDebugger"/> builds one for itself
	/// by default; drop the component on a GameObject and call <see cref="Bind"/> only when the
	/// debugger was built with <see cref="OmniDebuggerOptions.CreatePanel"/> off.
	/// </summary>
	[Preserve]
	[AddComponentMenu("DTech/OmniDebugger Panel")]
	public sealed class OmniDebuggerPanel : MonoBehaviour
	{
		private const string DocumentName = "OmniDebugger Document";
		private const string PanelObjectName = "OmniDebugger Panel";

		private readonly OmniDebuggerViewState _state = new ();

		/// <summary>Whether the panel is showing.</summary>
		public bool IsOpen => _view != null && _view.IsOpen;

		/// <summary>The bound debugger, or null when nothing was bound yet.</summary>
		public IOmniDebugger Debugger => _debugger;

		/// <summary>
		/// How the panel is scaled and opened. Changes to scaling apply through
		/// <see cref="SetScale"/>, changes to the button through <see cref="SetOpenButtonVisible"/>;
		/// shortcuts and tap counts are read live.
		/// </summary>
		public OmniDebuggerPanelOptions Options => _options ??= new OmniDebuggerPanelOptions();

		[Tooltip("Left empty, a document is created as a child of this object.")]
		[SerializeField] private UIDocument _document;

		[Tooltip("Left empty, the settings shipped with the package are used. Always cloned, never edited.")]
		[SerializeField] private PanelSettings _panelSettings;

		[SerializeField] private OmniDebuggerPanelOptions _options = new ();

		private IOmniDebuggerGesture _gesture;
		private ShortcutTrigger _shortcuts;
		private PanelSettings _runtimeSettings;
		private UIDocument _ownedDocument;
		private OmniDebuggerView _view;
		private IOmniDebugger _debugger;

#if OMNI_DEBUGGER_UGUI
		private PanelInputBinding _inputBinding;
#endif

		/// <summary>
		/// Creates a panel from nothing: a new object that survives scene loads, already bound.
		/// <see cref="DTech.OmniDebugger.OmniDebugger"/> calls this itself unless told otherwise.
		/// </summary>
		/// <param name="options">Null uses the defaults.</param>
		public static OmniDebuggerPanel Create(IOmniDebugger debugger, OmniDebuggerPanelOptions options = null)
		{
			if (debugger == null)
			{
				throw new ArgumentNullException(nameof(debugger));
			}

			GameObject host = new GameObject(PanelObjectName);
			host.SetActive(false);
			DontDestroyOnLoad(host);

			OmniDebuggerPanel panel = host.AddComponent<OmniDebuggerPanel>();
			panel._options = options ?? new OmniDebuggerPanelOptions();
			panel.Bind(debugger);

			host.SetActive(true);
			return panel;
		}

		/// <summary>
		/// Points the panel at a debugger and makes it the one the editor window shows as well.
		/// Binding a second debugger replaces the first.
		/// </summary>
		public void Bind(IOmniDebugger debugger)
		{
			if (debugger == null)
			{
				throw new ArgumentNullException(nameof(debugger));
			}

			if (ReferenceEquals(_debugger, debugger))
			{
				return;
			}

			ReleaseView();
			_debugger = debugger;
			OmniDebuggerViews.Register(debugger);

			if (isActiveAndEnabled)
			{
				BuildView();
			}
		}

		/// <summary>Drops the debugger and tears the panel down, leaving the component usable.</summary>
		public void Unbind()
		{
			if (_debugger == null)
			{
				return;
			}

			ReleaseView();
			OmniDebuggerViews.Unregister(_debugger);
			_debugger = null;
		}

		/// <summary>
		/// Replaces the floating button with another way in. Null removes it; the keyboard shortcuts
		/// keep working either way.
		/// </summary>
		public void SetGesture(IOmniDebuggerGesture gesture)
		{
			_gesture?.Detach();
			_gesture = gesture;
			AttachGesture();
		}

		/// <summary>Shows or hides the floating button, and remembers it in <see cref="Options"/>.</summary>
		public void SetOpenButtonVisible(bool visible)
		{
			Options.Open.ShowButton = visible;

			if (!visible)
			{
				SetGesture(null);
				return;
			}

			if (_gesture == null && _view != null)
			{
				SetGesture(CreateDefaultGesture());
			}
		}

		/// <summary>Rescales the panel on the spot and remembers the choice in <see cref="Options"/>.</summary>
		public void SetScale(OmniDebuggerScaleMode mode, float scale = 1.0f)
		{
			Options.ScaleMode = mode;
			Options.Scale = scale;
			PanelScaling.Apply(_runtimeSettings, Options.ScaleMode, Options.Scale);
		}

		/// <summary>Shows the panel.</summary>
		public void Open()
		{
			if (_view == null)
			{
				return;
			}

			_view.Open();
			_gesture?.SetPanelOpen(true);
		}

		/// <summary>Hides the panel, keeping what was typed and selected.</summary>
		public void Close()
		{
			if (_view == null)
			{
				return;
			}

			_view.Close();
		}

		/// <summary>Opens the panel when it is closed, closes it when it is open.</summary>
		public void Toggle()
		{
			if (IsOpen)
			{
				Close();
			}
			else
			{
				Open();
			}
		}

		private void OnEnable()
		{
			if (_debugger == null)
			{
				return;
			}

			BuildView();
		}

		private void OnDisable() => ReleaseView();

		private void Update()
		{
			if (_view == null)
			{
				return;
			}

#if OMNI_DEBUGGER_UGUI
			_inputBinding?.Sync(_view.Root.panel);
#endif

			_shortcuts ??= new ShortcutTrigger(InputBackends.Current);

			if (_shortcuts.Poll(Options.Open.Shortcuts))
			{
				Toggle();
			}
		}

		private void BuildView()
		{
			if (_view != null)
			{
				return;
			}

			UIDocument document = ResolveDocument();

			if (document == null)
			{
				return;
			}

			OmniDebuggerViewSettings settings = new OmniDebuggerViewSettings(
				document.rootVisualElement,
				_debugger,
				_state,
				PlayerPrefsViewPrefs.Default,
				Options.Theme,
				OmniDebuggerViewSettings.DefaultOrigin,
				useScreenSafeArea: true,
				showCloseButton: true,
				startOpen: Options.OpenOnStart,
				hostWindows: true);

			_view = new OmniDebuggerView(settings);
			_view.OnClosed += OnViewClosed;
			document.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnDocumentGeometryChanged);

#if OMNI_DEBUGGER_UGUI
			_inputBinding = new PanelInputBinding(transform);
			_inputBinding.Sync(document.rootVisualElement.panel);
#endif

			if (Options.Open.ShowButton)
			{
				_gesture ??= CreateDefaultGesture();
			}

			AttachGesture();
		}

		private IOmniDebuggerGesture CreateDefaultGesture() =>
			new HoldToDragButtonGesture(Options.Open, _debugger?.Logs);

		private void AttachGesture()
		{
			if (_gesture == null || _view == null)
			{
				return;
			}

			_gesture.Attach(_view.Root, Open);
			_gesture.SetPanelOpen(IsOpen);
		}

		private UIDocument ResolveDocument()
		{
			if (_document != null)
			{
				_ownedDocument = _document;
				return _document;
			}

			if (_ownedDocument != null)
			{
				return _ownedDocument;
			}

			PanelSettings source = _panelSettings != null ? _panelSettings : OmniDebuggerUiAssets.PanelSettings;

			if (source == null)
			{
				UnityLogSink.Default.Error(
					"Panel settings could not be loaded, so the runtime panel cannot be built. " +
					"Assign one on the component, or restore Runtime/UI/Resources/OmniDebugger.");
				return null;
			}

			_runtimeSettings = Instantiate(source);
			_runtimeSettings.name = $"{source.name} (Runtime)";
			_runtimeSettings.hideFlags = HideFlags.HideAndDontSave;
			_runtimeSettings.sortingOrder = Options.SortingOrder;
			_runtimeSettings.clearColor = false;
			PanelScaling.Apply(_runtimeSettings, Options.ScaleMode, Options.Scale);

			if (_runtimeSettings.themeStyleSheet == null)
			{
				_runtimeSettings.themeStyleSheet = OmniDebuggerUiAssets.RuntimeTheme;
			}

			GameObject documentObject = new GameObject(DocumentName);
			documentObject.SetActive(false);
			documentObject.transform.SetParent(transform, false);

			_ownedDocument = documentObject.AddComponent<UIDocument>();
			_ownedDocument.panelSettings = _runtimeSettings;
			documentObject.SetActive(true);

			return _ownedDocument;
		}

		private void OnDocumentGeometryChanged(GeometryChangedEvent evt) => PanelScaling.UpdateMatch(_runtimeSettings);

		private void OnViewClosed() => _gesture?.SetPanelOpen(false);

		private void ReleaseView()
		{
			_gesture?.Detach();

			if (_view != null)
			{
				VisualElement host = _ownedDocument != null ? _ownedDocument.rootVisualElement : null;
				host?.UnregisterCallback<GeometryChangedEvent>(OnDocumentGeometryChanged);

#if OMNI_DEBUGGER_UGUI
				_inputBinding?.Release(_view.Root.panel);
				_inputBinding = null;
#endif

				_view.OnClosed -= OnViewClosed;
				_view.Dispose();
				_view = null;
			}

			if (_ownedDocument != null && _document == null)
			{
				Destroy(_ownedDocument.gameObject);
			}

			_ownedDocument = null;

			if (_runtimeSettings != null)
			{
				Destroy(_runtimeSettings);
				_runtimeSettings = null;
			}
		}
	}
}