using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class OpenButtonGesture : IOmniDebuggerGesture
	{
		private const long ErrorPollMs = 250;
		private const long MinLitMs = 150;
		private const long SnapFrameMs = 16;
		private const float SnapDuration = 0.2f;
		private const float EdgePadding = 12.0f;
		private const float LitOpacity = 1.0f;
		private const int MaxBadgeCount = 99;

		private readonly ILogFeed _logs;
		private readonly IViewPrefs _prefs;
		private readonly ClickSeries _clicks = new ();

		private OmniDebuggerOpenOptions _options;
		private VisualElement _root;
		private VisualElement _button;
		private Label _badge;
		private Action _requestOpen;
		private IVisualElementScheduledItem _errorPoll;
		private IVisualElementScheduledItem _unlit;
		private IVisualElementScheduledItem _snap;

		private ScreenEdge _edge;
		private float _along;
		private Vector2 _pointerStart;
		private Vector2 _pressPosition;
		private Vector2 _position;
		private Vector2 _snapFrom;
		private Vector2 _snapTo;
		private int _pointerId = PointerId.invalidPointerId;
		private bool _hasPosition;
		private bool _dragging;
		private bool _panelOpen;
		private bool _lit;
		private long _seenErrors;
		private float _pressStart;
		private float _snapStart;

		public OpenButtonGesture(OmniDebuggerOpenOptions options, ILogFeed logs, IViewPrefs prefs)
		{
			_options = options ?? new OmniDebuggerOpenOptions();
			_logs = logs;
			_prefs = prefs;
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

			_badge = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.OpenButtonBadge);
			_badge.pickingMode = PickingMode.Ignore;
			UiBuild.SetVisible(_badge, false);
			_button.Add(_badge);

			_button.RegisterCallback<PointerDownEvent>(OnPointerDown);
			_button.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			_button.RegisterCallback<PointerUpEvent>(OnPointerUp);
			_button.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
			_button.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
			_root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
			_root.Add(_button);

			if (!_hasPosition)
			{
				if (!TryRestorePosition())
				{
					ResolveAnchor(_options.ButtonAnchor, out _edge, out _along);
				}

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
				ClearBadge();
				CancelPress();
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

			_errorPoll?.Pause();
			_unlit?.Pause();
			_snap?.Pause();

			_errorPoll = null;
			_unlit = null;
			_snap = null;
			_button = null;
			_badge = null;
			_root = null;
			_requestOpen = null;
			_pointerId = PointerId.invalidPointerId;
			_dragging = false;
			_lit = false;
			_clicks.Reset();
		}

		internal static void ResolveAnchor(OpenButtonAnchor anchor, out ScreenEdge edge, out float along)
		{
			switch (anchor)
			{
				case OpenButtonAnchor.TopLeft:
					edge = ScreenEdge.Left;
					along = 0.0f;
					break;
				case OpenButtonAnchor.Top:
					edge = ScreenEdge.Top;
					along = 0.5f;
					break;
				case OpenButtonAnchor.TopRight:
					edge = ScreenEdge.Right;
					along = 0.0f;
					break;
				case OpenButtonAnchor.Left:
					edge = ScreenEdge.Left;
					along = 0.5f;
					break;
				case OpenButtonAnchor.BottomLeft:
					edge = ScreenEdge.Left;
					along = 1.0f;
					break;
				case OpenButtonAnchor.Bottom:
					edge = ScreenEdge.Bottom;
					along = 0.5f;
					break;
				case OpenButtonAnchor.BottomRight:
					edge = ScreenEdge.Right;
					along = 1.0f;
					break;
				default:
					edge = ScreenEdge.Right;
					along = 0.5f;
					break;
			}
		}

		internal static void Snap(Vector2 position, Rect bounds, out ScreenEdge edge, out float along)
		{
			float left = position.x - bounds.xMin;
			float right = bounds.xMax - position.x;
			float top = position.y - bounds.yMin;
			float bottom = bounds.yMax - position.y;
			float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));

			if (nearest == left || nearest == right)
			{
				edge = nearest == left ? ScreenEdge.Left : ScreenEdge.Right;
				along = bounds.height > 0.0f ? Mathf.Clamp01((position.y - bounds.yMin) / bounds.height) : 0.5f;
				return;
			}

			edge = nearest == top ? ScreenEdge.Top : ScreenEdge.Bottom;
			along = bounds.width > 0.0f ? Mathf.Clamp01((position.x - bounds.xMin) / bounds.width) : 0.5f;
		}

		internal static Vector2 ToPosition(ScreenEdge edge, float along, Rect bounds)
		{
			switch (edge)
			{
				case ScreenEdge.Left:
					return new Vector2(bounds.xMin, Mathf.Lerp(bounds.yMin, bounds.yMax, along));
				case ScreenEdge.Right:
					return new Vector2(bounds.xMax, Mathf.Lerp(bounds.yMin, bounds.yMax, along));
				case ScreenEdge.Top:
					return new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, along), bounds.yMin);
				default:
					return new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, along), bounds.yMax);
			}
		}

		internal void SetOptions(OmniDebuggerOpenOptions options)
		{
			OpenButtonAnchor anchor = _options.ButtonAnchor;
			_options = options ?? new OmniDebuggerOpenOptions();

			if (_hasPosition && anchor != _options.ButtonAnchor)
			{
				ResolveAnchor(_options.ButtonAnchor, out _edge, out _along);
				Place();
			}

			RefreshOpacity();
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			if (evt.button != 0 || _pointerId != PointerId.invalidPointerId)
			{
				return;
			}

			_snap?.Pause();
			_pointerId = evt.pointerId;
			_pointerStart = evt.position;
			_pressPosition = _position;
			_dragging = false;
			_pressStart = Time.realtimeSinceStartup;

			_button.CapturePointer(_pointerId);
			_button.AddToClassList(OmniDebuggerUiClasses.OpenButtonPressed);
			SetLit(true);

			evt.StopPropagation();
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				return;
			}

			Vector2 delta = (Vector2)evt.position - _pointerStart;

			if (!_dragging)
			{
				if (delta.magnitude < TouchSlop.Distance)
				{
					return;
				}

				_dragging = true;
				_clicks.Reset();
				_button.AddToClassList(OmniDebuggerUiClasses.OpenButtonDragging);
			}

			if (TryGetBounds(out Rect bounds))
			{
				Vector2 target = _pressPosition + delta;
				SetPosition(new Vector2(
					Mathf.Clamp(target.x, bounds.xMin, bounds.xMax),
					Mathf.Clamp(target.y, bounds.yMin, bounds.yMax)));
			}
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				return;
			}

			bool wasDragging = _dragging;
			bool inside = _button.worldBound.Contains(evt.position);

			EndPress();

			if (wasDragging)
			{
				SnapToEdge();
				return;
			}

			if (!inside || !_clicks.Register(Time.realtimeSinceStartup, _options.ButtonClicks, _options.MultiClickWindow))
			{
				return;
			}

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
			bool wasDragging = _dragging;
			EndPress();

			if (wasDragging)
			{
				SnapToEdge();
			}
		}

		private void EndPress()
		{
			int pointerId = _pointerId;
			_pointerId = PointerId.invalidPointerId;
			_dragging = false;

			if (_button == null)
			{
				return;
			}

			if (pointerId != PointerId.invalidPointerId && _button.HasPointerCapture(pointerId))
			{
				_button.ReleasePointer(pointerId);
			}

			_button.RemoveFromClassList(OmniDebuggerUiClasses.OpenButtonPressed);
			_button.RemoveFromClassList(OmniDebuggerUiClasses.OpenButtonDragging);
			ReleaseLit();
		}

		private void SnapToEdge()
		{
			if (!TryGetBounds(out Rect bounds))
			{
				return;
			}

			Snap(_position, bounds, out _edge, out _along);
			_prefs?.SetOpenButton(_options.ButtonAnchor, _edge, _along);

			_snapFrom = _position;
			_snapTo = ToPosition(_edge, _along, bounds);
			_snapStart = Time.realtimeSinceStartup;

			_snap ??= _button.schedule.Execute(StepSnap).Every(SnapFrameMs);
			_snap.Resume();
		}

		private void StepSnap()
		{
			float t = Mathf.Clamp01((Time.realtimeSinceStartup - _snapStart) / SnapDuration);
			float eased = 1.0f - Mathf.Pow(1.0f - t, 3.0f);
			SetPosition(Vector2.LerpUnclamped(_snapFrom, _snapTo, eased));

			if (t >= 1.0f)
			{
				_snap.Pause();
				Place();
			}
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
			_unlit = _button.schedule.Execute(() => SetLit(false)).StartingIn(restMs);
		}

		private void RefreshOpacity()
		{
			if (_button != null)
			{
				_button.style.opacity = _lit || _dragging || _seenErrors < (_logs?.ErrorCount ?? 0)
					? LitOpacity
					: _options.ButtonOpacity;
			}
		}

		private void PollErrors()
		{
			if (_panelOpen)
			{
				_seenErrors = _logs.ErrorCount;
				return;
			}

			long unseen = _logs.ErrorCount - _seenErrors;
			bool visible = unseen > 0;

			_badge.text = unseen > MaxBadgeCount
				? MaxBadgeCount.ToString(CultureInfo.InvariantCulture) + "+"
				: unseen.ToString(CultureInfo.InvariantCulture);

			UiBuild.SetVisible(_badge, visible);
			_button.EnableInClassList(OmniDebuggerUiClasses.OpenButtonHasErrors, visible);
			RefreshOpacity();
		}

		private void ClearBadge()
		{
			if (_logs != null)
			{
				_seenErrors = _logs.ErrorCount;
			}

			if (_badge != null)
			{
				UiBuild.SetVisible(_badge, false);
				_button.RemoveFromClassList(OmniDebuggerUiClasses.OpenButtonHasErrors);
			}
		}

		private void OnRootGeometryChanged(GeometryChangedEvent evt)
		{
			if (_pointerId == PointerId.invalidPointerId && (_snap == null || !_snap.isActive))
			{
				Place();
			}
		}

		private void Place()
		{
			if (_button == null || !_hasPosition || !TryGetBounds(out Rect bounds))
			{
				return;
			}

			SetPosition(ToPosition(_edge, _along, bounds));
		}

		private void SetPosition(Vector2 position)
		{
			_position = position;
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

		private bool TryRestorePosition()
		{
			if (_prefs == null ||
				!_prefs.TryGetOpenButton(out OpenButtonAnchor anchor, out ScreenEdge edge, out float along) ||
				anchor != _options.ButtonAnchor ||
				edge > ScreenEdge.Bottom)
			{
				return false;
			}

			_edge = edge;
			_along = Mathf.Clamp01(along);
			return true;
		}
	}
}
