using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class HoldToDragButtonGesture : IOmniDebuggerGesture
	{
		private const long UnlockDelayMs = 600;
		private const long ErrorPollMs = 250;
		private const long PulseFrameMs = 16;
		private const long MinLitMs = 150;
		private const float EdgePadding = 12.0f;
		private const float PulsePeriod = 0.8f;
		private const float PulseMinOpacity = 0.45f;
		private const float PulseDuration = 45.0f;
		private const float LitOpacity = 1.0f;

		private readonly ILogFeed _logs;
		private readonly ClickSeries _clicks = new ();

		private OmniDebuggerOpenOptions _options;
		private VisualElement _root;
		private VisualElement _button;
		private VisualElement _alert;
		private DragCornersIndicator _corners;
		private Action _requestOpen;
		private IVisualElementScheduledItem _unlock;
		private IVisualElementScheduledItem _errorPoll;
		private IVisualElementScheduledItem _pulse;
		private IVisualElementScheduledItem _unlit;

		private Vector2 _normalized;
		private Vector2 _pointerStart;
		private Vector2 _pressNormalized;
		private int _pointerId = PointerId.invalidPointerId;
		private bool _hasPosition;
		private bool _pressedInside;
		private bool _unlocked;
		private bool _dragging;
		private bool _panelOpen;
		private bool _lit;
		private long _seenErrors;
		private float _pulseStart;
		private float _pressStart;

		public HoldToDragButtonGesture(OmniDebuggerOpenOptions options, ILogFeed logs)
		{
			_options = options ?? new OmniDebuggerOpenOptions();
			_logs = logs;
		}

		public void Attach(VisualElement root, Action requestOpen)
		{
			if (root == null)
			{
				throw new ArgumentNullException(nameof(root));
			}

			if (requestOpen == null)
			{
				throw new ArgumentNullException(nameof(requestOpen));
			}

			Detach();

			_root = root;
			_requestOpen = requestOpen;

			_button = new VisualElement { name = OmniDebuggerUiClasses.OpenButton };
			_button.AddToClassList(OmniDebuggerUiClasses.OpenButton);
			_button.AddManipulator(new Halo());

			OmniIcon mark = new OmniIcon(IconGlyph.Terminal);
			mark.AddToClassList(OmniDebuggerUiClasses.OpenButtonMark);
			_button.Add(mark);

			_alert = new VisualElement();
			_alert.AddToClassList(OmniDebuggerUiClasses.OpenButtonAlert);
			_alert.Add(new OmniIcon(IconGlyph.Alert));
			_alert.pickingMode = PickingMode.Ignore;
			_alert.style.display = DisplayStyle.None;
			_button.Add(_alert);

			_corners = new DragCornersIndicator();
			_button.Add(_corners);

			_button.RegisterCallback<PointerDownEvent>(OnPointerDown);
			_button.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			_button.RegisterCallback<PointerUpEvent>(OnPointerUp);
			_button.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
			_button.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
			_root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
			_root.Add(_button);

			if (!_hasPosition)
			{
				_normalized = GetDefaultPosition();
				_hasPosition = true;
			}

			_seenErrors = _logs?.ErrorCount ?? 0;

			if (_logs != null)
			{
				_errorPoll = _button.schedule.Execute(PollErrors).Every(ErrorPollMs);
			}

			SetPanelOpen(_panelOpen);
			RefreshOpacity();
			Place();
		}

		public void SetPanelOpen(bool open)
		{
			_panelOpen = open;

			if (_button == null)
			{
				return;
			}

			_button.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;

			if (open)
			{
				StopAlert();
				ResetPress();
				SetLit(false);
			}
		}

		public void Detach()
		{
			if (_button != null)
			{
				if (_pointerId != PointerId.invalidPointerId && _button.HasPointerCapture(_pointerId))
				{
					_button.ReleasePointer(_pointerId);
				}

				_button.UnregisterCallback<PointerDownEvent>(OnPointerDown);
				_button.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
				_button.UnregisterCallback<PointerUpEvent>(OnPointerUp);
				_button.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
				_button.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
				_button.RemoveFromHierarchy();
			}

			_root?.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

			_unlock?.Pause();
			_errorPoll?.Pause();
			_pulse?.Pause();
			_unlit?.Pause();

			_unlock = null;
			_errorPoll = null;
			_pulse = null;
			_unlit = null;
			_button = null;
			_alert = null;
			_corners = null;
			_root = null;
			_requestOpen = null;
			_pointerId = PointerId.invalidPointerId;
			_pressedInside = false;
			_unlocked = false;
			_dragging = false;
			_lit = false;
			_clicks.Reset();
		}

		internal void SetOptions(OmniDebuggerOpenOptions options)
		{
			OpenButtonAnchor anchor = _options.ButtonAnchor;
			_options = options ?? new OmniDebuggerOpenOptions();

			if (_hasPosition && anchor != _options.ButtonAnchor)
			{
				_normalized = GetDefaultPosition();
				Place();
			}

			RefreshOpacity();
		}

		private static Vector2 FromNormalized(Vector2 normalized, Rect bounds) =>
			new (Mathf.Lerp(bounds.xMin, bounds.xMax, normalized.x), Mathf.Lerp(bounds.yMin, bounds.yMax, normalized.y));

		private static Vector2 ToNormalized(Vector2 position, Rect bounds) =>
			new (
				bounds.width > 0.0f ? Mathf.Clamp01((position.x - bounds.xMin) / bounds.width) : 0.5f,
				bounds.height > 0.0f ? Mathf.Clamp01((position.y - bounds.yMin) / bounds.height) : 0.5f);

		private void OnPointerDown(PointerDownEvent evt)
		{
			if (evt.button != 0 || _pointerId != PointerId.invalidPointerId)
			{
				return;
			}

			_pointerId = evt.pointerId;
			_pointerStart = evt.position;
			_pressNormalized = _normalized;
			_pressedInside = true;
			_unlocked = false;
			_dragging = false;
			_pressStart = Time.realtimeSinceStartup;

			_button.CapturePointer(_pointerId);
			_button.AddToClassList(OmniDebuggerUiClasses.OpenButtonPressed);
			SetLit(true);

			_unlock?.Pause();
			_unlock = _button.schedule.Execute(UnlockDrag).StartingIn(UnlockDelayMs);

			evt.StopPropagation();
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				return;
			}

			Vector2 position = evt.position;

			if (!_unlocked)
			{
				bool inside = _button.worldBound.Contains(position);

				if (inside != _pressedInside)
				{
					_pressedInside = inside;
					_button.EnableInClassList(OmniDebuggerUiClasses.OpenButtonPressed, inside);
					SetLit(inside);
				}

				if (!inside)
				{
					_unlock?.Pause();
				}

				return;
			}

			Vector2 delta = position - _pointerStart;

			if (!_dragging && delta.magnitude < TouchSlop.Distance)
			{
				return;
			}

			_dragging = true;
			_clicks.Reset();

			if (TryGetBounds(out Rect bounds))
			{
				Vector2 start = FromNormalized(_pressNormalized, bounds);
				_normalized = ToNormalized(start + delta, bounds);
				Place();
			}
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				return;
			}

			bool wasUnlocked = _unlocked;
			bool inside = _button.worldBound.Contains(evt.position);

			EndPress();

			if (wasUnlocked)
			{
				return;
			}

			if (!inside)
			{
				return;
			}

			if (!_clicks.Register(Time.realtimeSinceStartup, _options.ButtonClicks, _options.MultiClickWindow))
			{
				return;
			}

			StopAlert();
			_requestOpen?.Invoke();
		}

		private void OnPointerCancel(PointerCancelEvent evt)
		{
			if (evt.pointerId == _pointerId)
			{
				CancelPress();
			}
		}

		private void OnCaptureOut(PointerCaptureOutEvent evt)
		{
			if (_pointerId != PointerId.invalidPointerId)
			{
				CancelPress();
			}
		}

		private void CancelPress()
		{
			if (_dragging)
			{
				_normalized = _pressNormalized;
				Place();
			}

			EndPress();
		}

		private void EndPress()
		{
			int pointerId = _pointerId;
			ResetPress();

			if (_button != null && pointerId != PointerId.invalidPointerId && _button.HasPointerCapture(pointerId))
			{
				_button.ReleasePointer(pointerId);
			}
		}

		private void ResetPress()
		{
			_unlock?.Pause();
			_pointerId = PointerId.invalidPointerId;
			_pressedInside = false;
			_unlocked = false;
			_dragging = false;

			if (_button == null)
			{
				return;
			}

			_button.RemoveFromClassList(OmniDebuggerUiClasses.OpenButtonPressed);
			_button.RemoveFromClassList(OmniDebuggerUiClasses.OpenButtonDragging);
			_corners.Hide();
			ReleaseLit();
		}

		private void SetLit(bool lit)
		{
			_unlit?.Pause();
			_lit = lit;

			if (_button == null)
			{
				return;
			}

			_button.EnableInClassList(OmniDebuggerUiClasses.OpenButtonLit, lit);
			RefreshOpacity();
		}

		private void ReleaseLit()
		{
			if (!_lit)
			{
				RefreshOpacity();
				return;
			}

			long elapsedMs = (long)((Time.realtimeSinceStartup - _pressStart) * 1000.0f);
			long restMs = MinLitMs - elapsedMs;

			if (restMs <= 0)
			{
				SetLit(false);
				return;
			}

			_unlit?.Pause();
			_unlit = _button.schedule.Execute(Unlight).StartingIn(restMs);
		}

		private void Unlight() => SetLit(false);

		private void RefreshOpacity()
		{
			if (_button != null)
			{
				_button.style.opacity = _lit || _unlocked ? LitOpacity : _options.ButtonOpacity;
			}
		}

		private void UnlockDrag()
		{
			if (_button == null || _pointerId == PointerId.invalidPointerId || !_pressedInside)
			{
				return;
			}

			_unlocked = true;
			_clicks.Reset();
			_button.AddToClassList(OmniDebuggerUiClasses.OpenButtonDragging);
			_corners.Show();
			RefreshOpacity();
		}

		private void PollErrors()
		{
			long errors = _logs.ErrorCount;

			if (errors <= _seenErrors)
			{
				return;
			}

			_seenErrors = errors;

			if (!_panelOpen)
			{
				StartAlert();
			}
		}

		private void StartAlert()
		{
			_pulseStart = Time.realtimeSinceStartup;
			_alert.style.display = DisplayStyle.Flex;
			_button.AddToClassList(OmniDebuggerUiClasses.OpenButtonAlerting);

			_pulse ??= _alert.schedule.Execute(Pulse).Every(PulseFrameMs);
			_pulse.Resume();
		}

		private void Pulse()
		{
			float elapsed = Time.realtimeSinceStartup - _pulseStart;

			if (elapsed >= PulseDuration)
			{
				StopAlert();
				return;
			}

			float wave = Mathf.PingPong(elapsed / PulsePeriod, 1.0f);
			_alert.style.opacity = Mathf.Lerp(PulseMinOpacity, 1.0f, wave);
		}

		private void StopAlert()
		{
			_pulse?.Pause();

			if (_alert != null)
			{
				_alert.style.display = DisplayStyle.None;
			}

			_button?.RemoveFromClassList(OmniDebuggerUiClasses.OpenButtonAlerting);

			if (_logs != null)
			{
				_seenErrors = _logs.ErrorCount;
			}
		}

		private void OnRootGeometryChanged(GeometryChangedEvent evt) => Place();

		private void Place()
		{
			if (_button == null || !_hasPosition || !TryGetBounds(out Rect bounds))
			{
				return;
			}

			Vector2 position = FromNormalized(_normalized, bounds);
			_button.style.left = position.x;
			_button.style.top = position.y;
		}

		private bool TryGetBounds(out Rect bounds)
		{
			bounds = default;

			if (_root == null || _button == null)
			{
				return false;
			}

			Rect area = _root.layout;
			float width = _button.resolvedStyle.width;
			float height = _button.resolvedStyle.height;

			if (area.width <= 0.0f || area.height <= 0.0f || float.IsNaN(width) || float.IsNaN(height))
			{
				return false;
			}

			Vector4 insets = SafeArea.TryMeasure(_root.panel, out Vector4 measured) ? measured : Vector4.zero;

			float xMin = insets.x + EdgePadding;
			float yMin = insets.z + EdgePadding;
			float xMax = Mathf.Max(xMin, area.width - insets.y - EdgePadding - width);
			float yMax = Mathf.Max(yMin, area.height - insets.w - EdgePadding - height);

			bounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
			return true;
		}

		private Vector2 GetDefaultPosition()
		{
			switch (_options.ButtonAnchor)
			{
				case OpenButtonAnchor.TopLeft:
					return new Vector2(0.0f, 0.0f);
				case OpenButtonAnchor.Top:
					return new Vector2(0.5f, 0.0f);
				case OpenButtonAnchor.TopRight:
					return new Vector2(1.0f, 0.0f);
				case OpenButtonAnchor.Left:
					return new Vector2(0.0f, 0.5f);
				case OpenButtonAnchor.BottomLeft:
					return new Vector2(0.0f, 1.0f);
				case OpenButtonAnchor.Bottom:
					return new Vector2(0.5f, 1.0f);
				case OpenButtonAnchor.BottomRight:
					return new Vector2(1.0f, 1.0f);
				default:
					return new Vector2(1.0f, 0.5f);
			}
		}
	}
}