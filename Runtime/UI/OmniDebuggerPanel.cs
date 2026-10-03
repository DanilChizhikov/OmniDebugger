using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Puts the panel on screen in a running game. <see cref="OmniDebuggerHost"/> builds one for itself
	/// by default; drop the component on a GameObject and call <see cref="Bind"/> only when the
	/// debugger was built with <see cref="OmniDebuggerOptions.CreatePanel"/> off. The component carries no
	/// options of its own: it reads <c>Project Settings → DTech → OmniDebugger → Panel</c>, and code can
	/// change <see cref="Options"/> before binding.
	/// </summary>
	[Preserve]
	[AddComponentMenu("DTech/OmniDebugger Panel")]
	public sealed class OmniDebuggerPanel : MonoBehaviour
	{
		private const string DocumentName = "OmniDebugger Document";
		private const string PanelObjectName = "OmniDebugger Panel";

		private static readonly OmniDebuggerShortcut[] _paletteShortcuts =
		{
			new (KeyCode.LeftControl, KeyCode.K),
			new (KeyCode.LeftCommand, KeyCode.K),
		};

		private readonly OmniDebuggerViewState _state = new ();
		private readonly LockAttempts _lockAttempts = new ();

		/// <summary>Whether the panel is showing.</summary>
		public bool IsOpen => _view != null && _view.IsOpen;

		/// <summary>The bound debugger, or null when nothing was bound yet.</summary>
		public IOmniDebuggerHost Debugger => _debugger;

		/// <summary>
		/// How the panel is scaled, skinned, opened and locked: a copy of the project settings, read the first
		/// time it is touched, or what <see cref="Create"/> was given. Panel settings and sorting order
		/// are read when the panel is built, so change them before <see cref="Bind"/>. Changes to
		/// scaling apply through <see cref="SetScale"/>, changes to the button through
		/// <see cref="SetOpenButtonEnabled"/>; shortcuts, tap counts and the lock are read live. Unless
		/// <see cref="Create"/> was given options, play mode in the editor replaces this copy whenever the
		/// project settings are edited, and the panel applies the edit on its next frame.
		/// </summary>
		public OmniDebuggerPanelOptions Options => _options ??= OmniDebuggerOptions.Default.Panel;

		[Tooltip("Left empty, a document is created as a child of this object.")]
		[SerializeField] private UIDocument _document;

		private OmniDebuggerPanelOptions _options;
		private bool _optionsGiven;
		private bool _everBuilt;
		private bool _projectOptionsChanged;
		private IOmniDebuggerGesture _gesture;
		private ShortcutTrigger _shortcuts;
		private PanelSettings _runtimeSettings;
		private UIDocument _ownedDocument;
		private OmniDebuggerView _view;
		private LockPrompt _lockPrompt;
		private IOmniDebuggerHost _debugger;

#if OMNI_DEBUGGER_UGUI
		private PanelInputBinding _inputBinding;
#endif

		private bool IsLockPromptShowing => _lockPrompt != null && _lockPrompt.IsShowing;

		private bool IsLockRequired => Options.Lock.IsActive && !UnlockMemory.IsUnlocked(Options.Lock);

		/// <summary>
		/// Creates a panel from nothing: a new object that survives scene loads, already bound.
		/// <see cref="OmniDebuggerHost"/> calls this itself unless told otherwise.
		/// </summary>
		/// <param name="options">Null uses the project settings.</param>
		public static OmniDebuggerPanel Create(IOmniDebuggerHost debugger, OmniDebuggerPanelOptions options = null)
		{
			if (debugger == null)
			{
				throw new ArgumentNullException(nameof(debugger));
			}

			GameObject host = new GameObject(PanelObjectName);
			host.SetActive(false);
			DontDestroyOnLoad(host);

			OmniDebuggerPanel panel = host.AddComponent<OmniDebuggerPanel>();
			panel._options = options;
			panel._optionsGiven = options != null;
			panel.Bind(debugger);

			host.SetActive(true);
			return panel;
		}

		/// <summary>
		/// Points the panel at a debugger and makes it the one the editor window shows as well.
		/// Binding a second debugger replaces the first.
		/// </summary>
		public void Bind(IOmniDebuggerHost debugger)
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
		public void SetOpenButtonEnabled(bool enabled)
		{
			Options.Open.ButtonEnabled = enabled;

			if (!enabled)
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

		/// <summary>
		/// Shows the panel, or first the PIN or password prompt when <see cref="OmniDebuggerPanelOptions.Lock"/>
		/// asks for one and the panel is not unlocked yet.
		/// </summary>
		public void Open()
		{
			if (_view == null)
			{
				return;
			}

			if (IsLockRequired)
			{
				ShowLockPrompt();
				return;
			}

			OpenView();
		}

		/// <summary>Hides the panel, keeping what was typed and selected. Dismisses the lock prompt too.</summary>
		public void Close()
		{
			if (_view == null)
			{
				return;
			}

			if (IsLockPromptShowing)
			{
				_lockPrompt.Cancel();
				return;
			}

			_view.Close();
		}

		/// <summary>Opens the panel when it is closed, closes it when it is open.</summary>
		public void Toggle()
		{
			if (IsOpen || IsLockPromptShowing)
			{
				Close();
			}
			else
			{
				Open();
			}
		}

		internal void Apply(OmniDebuggerPanelOptions next)
		{
			if (next == null)
			{
				throw new ArgumentNullException(nameof(next));
			}

			OmniDebuggerPanelOptions previous = Options;
			_options = next;

			if (_gesture is OpenButtonGesture button)
			{
				button.SetOptions(next.Open);
			}

			if (previous.Open.ButtonEnabled != next.Open.ButtonEnabled)
			{
				SetOpenButtonEnabled(next.Open.ButtonEnabled);
			}

			if (_view == null)
			{
				return;
			}

			if (previous.PanelSettings != next.PanelSettings)
			{
				ReleaseView();
				BuildView();
				return;
			}

			ApplyLayout();

			if (_runtimeSettings != null)
			{
				_runtimeSettings.sortingOrder = next.SortingOrder;
				PanelScaling.Apply(_runtimeSettings, next.ScaleMode, next.Scale);
			}
		}

		private static string DescribeShortcut(IReadOnlyList<OmniDebuggerShortcut> shortcuts)
		{
			for (int i = 0; i < shortcuts.Count; i++)
			{
				string hint = shortcuts[i]?.ToHint();

				if (!string.IsNullOrEmpty(hint))
				{
					return hint;
				}
			}

			return null;
		}

		private void OnEnable()
		{
			if (!_optionsGiven)
			{
				ProjectOptions.OnEditorChanged += OnProjectOptionsChanged;
			}

			if (_debugger == null)
			{
				return;
			}

			BuildView();
		}

		private void OnDisable()
		{
			ProjectOptions.OnEditorChanged -= OnProjectOptionsChanged;
			ReleaseView();
		}

		private void Update()
		{
			if (_projectOptionsChanged)
			{
				_projectOptionsChanged = false;
				Apply(OmniDebuggerOptions.Default.Panel);
			}

			if (_view == null)
			{
				return;
			}

#if OMNI_DEBUGGER_UGUI
			_inputBinding?.Sync(_view.Root.panel);
#endif

			_shortcuts ??= new ShortcutTrigger(InputBackends.Current);

			if (_shortcuts.Poll(Options.Open.Shortcuts) || _shortcuts.PollGamepad(Options.Open.GamepadCombo))
			{
				Toggle();
			}
			else if (_shortcuts.Poll(_paletteShortcuts))
			{
				OpenPalette();
			}

			RouteStrandedDpad();
		}

		private void RouteStrandedDpad()
		{
			NavigationMoveEvent.Direction direction = InputBackends.Current.PollStrandedDpad();
			VisualElement root = _view.Root;

			if (direction == NavigationMoveEvent.Direction.None ||
				root.panel?.focusController?.focusedElement is not VisualElement focused ||
				!root.Contains(focused))
			{
				return;
			}

			using NavigationMoveEvent move = NavigationMoveEvent.GetPooled(direction);
			move.target = focused;
			focused.SendEvent(move);
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

			bool openOnStart = Options.OpenOnStart && !_everBuilt;
			bool locked = IsLockRequired;

			OmniDebuggerViewSettings settings = new OmniDebuggerViewSettings(
				document.rootVisualElement,
				_debugger,
				_state,
				PlayerPrefsViewPrefs.Default,
				OmniDebuggerViewSettings.DefaultOrigin,
				useScreenSafeArea: true,
				showCloseButton: true,
				startOpen: openOnStart && !locked,
				hostOverlays: true);

			_everBuilt = true;

			_view = new OmniDebuggerView(settings);
			_view.OnClosed += OnViewClosed;
			ApplyLayout();
			document.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnDocumentGeometryChanged);

#if OMNI_DEBUGGER_UGUI
			_inputBinding = new PanelInputBinding(transform);
			_inputBinding.Sync(document.rootVisualElement.panel);
#endif

			if (Options.Open.ButtonEnabled)
			{
				_gesture ??= CreateDefaultGesture();
			}

			AttachGesture();

			if (openOnStart && locked)
			{
				ShowLockPrompt();
			}
		}

		private void OnProjectOptionsChanged() => _projectOptionsChanged = true;

		private void ApplyLayout()
		{
			_view.SetLandscapeLayout(Options.LandscapeLayout);
			_view.SetFloatingScale(Options.FloatingScale);
			_view.SetHotbarEdge(Options.HotbarEdge);
			_view.SetShortcutHint(DescribeShortcut(Options.Open.Shortcuts));
		}

		private void OpenPalette()
		{
			if (!IsOpen && IsLockRequired)
			{
				Open();
				return;
			}

			_view.ShowPalette();
		}

		private IOmniDebuggerGesture CreateDefaultGesture() =>
			new OpenButtonGesture(Options.Open, _debugger?.Logs, PlayerPrefsViewPrefs.Default);

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

			PanelSettings source = Options.PanelSettings != null ? Options.PanelSettings : OmniDebuggerUiAssets.PanelSettings;

			if (source == null)
			{
				UnityLogSink.Default.Error(
					"Panel settings could not be loaded, so the runtime panel cannot be built. " +
					"Assign one in Project Settings → DTech → OmniDebugger → Panel, " +
					"or restore Runtime/UI/Resources/OmniDebugger.");
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

		private void OpenView()
		{
			_view.Open();
			_gesture?.SetPanelOpen(true);
		}

		private void ShowLockPrompt()
		{
			if (_lockPrompt == null)
			{
				_lockPrompt = new LockPrompt(_view.Root, _lockAttempts);
				_lockPrompt.OnUnlocked += OnLockUnlocked;
				_lockPrompt.OnCancelled += OnLockCancelled;
			}

			_lockPrompt.Show(Options.Lock);
			_gesture?.SetPanelOpen(true);
		}

		private void OnLockUnlocked()
		{
			UnlockMemory.Remember(Options.Lock);
			_lockAttempts.Reset();

			if (_view != null)
			{
				OpenView();
			}
		}

		private void OnLockCancelled() => _gesture?.SetPanelOpen(false);

		private void ReleaseLockPrompt()
		{
			if (_lockPrompt == null)
			{
				return;
			}

			_lockPrompt.OnUnlocked -= OnLockUnlocked;
			_lockPrompt.OnCancelled -= OnLockCancelled;
			_lockPrompt.Dispose();
			_lockPrompt = null;
		}

		private void ReleaseView()
		{
			_gesture?.Detach();
			ReleaseLockPrompt();

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