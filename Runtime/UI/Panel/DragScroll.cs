using System;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace DTech.OmniDebugger.UI
{
	internal sealed class DragScroll : Manipulator
	{
		public event Action OnDragStarted;

		private const float VelocitySmoothing = 0.3f;
		private const float InertiaHalfLife = 0.2f;
		private const float MinVelocity = 25.0f;
		private const float MaxVelocity = 4000.0f;
		private const float MaxReleaseDelay = 0.1f;
		private const long InertiaIntervalMs = 16;
		private const int PrimaryButtonMask = 1;
		private const float NavigationStep = 0.33f;
		private const float NavigationTolerance = 1.0f;

		private readonly PrimaryPress _press = new PrimaryPress();

		public bool IsDragging { get; private set; }

		private bool IsHorizontal => _scroll.mode == ScrollViewMode.Horizontal;

		private ScrollView _scroll;
		private IVisualElementScheduledItem _inertia;
		private VisualElement _pressed;
		private VisualElement _pressTarget;
		private VisualElement _adjustable;
		private VisualElement _editable;
		private Vector2 _pointerStart;
		private Vector2 _pointerLast;
		private float _lastMoveTime;
		private float _lastInertiaTime;
		private float _velocity;
		private bool _coasting;
		private int _pointerId = PointerId.invalidPointerId;
		private int _handOffPointerId = PointerId.invalidPointerId;

		protected override void RegisterCallbacksOnTarget()
		{
			_scroll = target as ScrollView ?? throw new InvalidOperationException("A drag scroll drives a ScrollView only.");
			_inertia = _scroll.schedule.Execute(Coast).Every(InertiaIntervalMs);
			_inertia.Pause();

			_scroll.verticalScroller.slider.focusable = false;
			_scroll.horizontalScroller.slider.focusable = false;

			_scroll.RegisterCallback<FocusInEvent>(OnFocusIn);
			_scroll.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
			_scroll.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			_scroll.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
			_scroll.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			_scroll.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
			_scroll.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
			_scroll.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			_scroll.UnregisterCallback<FocusInEvent>(OnFocusIn);
			_scroll.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
			_scroll.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			_scroll.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
			_scroll.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			_scroll.UnregisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
			_scroll.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
			_scroll.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);

			StopInertia();
			EndPointer();
			_handOffPointerId = PointerId.invalidPointerId;
		}

		private static void Tap(VisualElement element)
		{
			if (!element.enabledInHierarchy)
			{
				return;
			}

			using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
			submit.target = element;
			element.SendEvent(submit);
		}

		private void OnFocusIn(FocusInEvent evt)
		{
			if (_pointerId != PointerId.invalidPointerId ||
				evt.target is not VisualElement focused ||
				!_scroll.contentContainer.Contains(focused))
			{
				return;
			}

			StopInertia();
			Reveal(focused);
		}

		private void OnNavigationMove(NavigationMoveEvent evt)
		{
			int sign = AxisSign(evt.direction);

			if (sign == 0 || evt.target is not VisualElement focused || !_scroll.contentContainer.Contains(focused))
			{
				return;
			}

			Rect from = focused.worldBound;
			VisualElement next = null;
			float bestAlong = float.MaxValue;
			float bestAcross = float.MaxValue;
			FindNext(_scroll.contentContainer, focused, from, sign, ref next, ref bestAlong, ref bestAcross);

			if (next != null)
			{
				Reveal(next);
				return;
			}

			float offset = AxisOffset();
			float stepped = Clamp(offset + sign * Along(_scroll.contentViewport.layout.size) * NavigationStep);

			if (Mathf.Approximately(offset, stepped))
			{
				return;
			}

			StopInertia();
			SetAxisOffset(stepped);
			focused.focusController?.IgnoreEvent(evt);
			evt.StopPropagation();
		}

		private void FindNext(
			VisualElement element,
			VisualElement focused,
			Rect from,
			int sign,
			ref VisualElement next,
			ref float bestAlong,
			ref float bestAcross)
		{
			if (element.resolvedStyle.display == DisplayStyle.None)
			{
				return;
			}

			if (element != focused && element.canGrabFocus)
			{
				Rect bounds = element.worldBound;
				float along = sign > 0 ? AlongMin(bounds) - AlongMax(from) : AlongMin(from) - AlongMax(bounds);

				if (along >= -NavigationTolerance)
				{
					float across = Mathf.Abs(AcrossCenter(bounds) - AcrossCenter(from));

					if (along < bestAlong - NavigationTolerance ||
						(along <= bestAlong + NavigationTolerance && across < bestAcross))
					{
						next = element;
						bestAlong = along;
						bestAcross = across;
					}
				}
			}

			for (int i = 0; i < element.hierarchy.childCount; i++)
			{
				FindNext(element.hierarchy[i], focused, from, sign, ref next, ref bestAlong, ref bestAcross);
			}
		}

		private void Reveal(VisualElement element)
		{
			Rect bounds = _scroll.contentViewport.WorldToLocal(element.worldBound);
			float viewport = Along(_scroll.contentViewport.layout.size);
			float start = AlongMin(bounds);
			float end = AlongMax(bounds);
			float delta;

			if (start < -NavigationTolerance || end - start > viewport)
			{
				delta = start;
			}
			else if (end > viewport + NavigationTolerance)
			{
				delta = end - viewport;
			}
			else
			{
				return;
			}

			float offset = AxisOffset();
			float revealed = Clamp(offset + delta);

			if (Mathf.Approximately(offset, revealed))
			{
				return;
			}

			StopInertia();
			SetAxisOffset(revealed);
		}

		private int AxisSign(NavigationMoveEvent.Direction direction)
		{
			switch (direction)
			{
				case NavigationMoveEvent.Direction.Down:
					return IsHorizontal ? 0 : 1;
				case NavigationMoveEvent.Direction.Up:
					return IsHorizontal ? 0 : -1;
				case NavigationMoveEvent.Direction.Right:
					return IsHorizontal ? 1 : 0;
				case NavigationMoveEvent.Direction.Left:
					return IsHorizontal ? -1 : 0;
				default:
					return 0;
			}
		}

		private float AlongMin(Rect rect) => IsHorizontal ? rect.xMin : rect.yMin;

		private float AlongMax(Rect rect) => IsHorizontal ? rect.xMax : rect.yMax;

		private float AcrossCenter(Rect rect) => IsHorizontal ? rect.center.y : rect.center.x;

		private void OnPointerDown(PointerDownEvent evt)
		{
			if (evt.pointerId == _handOffPointerId)
			{
				_handOffPointerId = PointerId.invalidPointerId;
				return;
			}

			VisualElement pressed = evt.target as VisualElement;

			if (evt.button != 0 ||
				!evt.isPrimary ||
				!TryClassify(pressed, evt.pointerType, out VisualElement adjustable, out VisualElement editable))
			{
				return;
			}

			if (_pointerId != PointerId.invalidPointerId)
			{
				if (evt.pointerId != _pointerId)
				{
					return;
				}

				EndPointer();
			}

			_coasting = _inertia.isActive;
			StopInertia();

			_pointerId = evt.pointerId;
			_pointerStart = _scroll.WorldToLocal(evt.position);
			_pointerLast = _pointerStart;
			_lastMoveTime = Time.realtimeSinceStartup;
			IsDragging = false;

			_pressTarget = pressed;
			_adjustable = adjustable;
			_editable = editable;

			if (_editable != null)
			{
				_scroll.focusController?.IgnoreEvent(evt);
			}

			if (!_coasting && _adjustable == null && _editable == null)
			{
				_pressed = FindTappable(pressed);
				_pressed?.AddToClassList(OmniDebuggerUiClasses.Pressed);
			}

			_scroll.CapturePointer(_pointerId);
			evt.StopPropagation();
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				return;
			}

			if ((evt.pressedButtons & PrimaryButtonMask) == 0)
			{
				Release(evt);
				return;
			}

			evt.StopPropagation();

			Vector2 position = _scroll.WorldToLocal(evt.position);

			if (!IsDragging)
			{
				_pressed?.EnableInClassList(OmniDebuggerUiClasses.Pressed, _pressed.worldBound.Contains(evt.position));

				Vector2 travel = position - _pointerStart;
				float along = Mathf.Abs(Along(travel));
				float across = Mathf.Abs(Across(travel));

				if (along < TouchSlop.Distance || along <= across)
				{
					if (across >= TouchSlop.Distance && across > along)
					{
						TryHandOff(evt);
					}

					return;
				}

				IsDragging = true;
				ClearPressed();

				if (!_scroll.HasPointerCapture(_pointerId))
				{
					_scroll.CapturePointer(_pointerId);
				}

				OnDragStarted?.Invoke();
			}

			float step = Along(_pointerLast - position);
			_pointerLast = position;
			SetAxisOffset(Clamp(AxisOffset() + step));
			TrackVelocity(step);
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				ForgetHandOff(evt.pointerId);
				return;
			}

			bool tap = !IsDragging && !_coasting;
			VisualElement target = _pressTarget;

			if (tap && _adjustable != null && _adjustable.worldBound.Contains(evt.position) && TryHandOff(evt))
			{
				using PointerUpEvent up = PointerUpEvent.GetPooled(evt);
				up.target = target;
				target.SendEvent(up);
				return;
			}

			VisualElement tapped = tap && _pressed != null && _pressed.worldBound.Contains(evt.position)
				? _pressed
				: null;
			VisualElement edited = tap && _editable != null && _editable.worldBound.Contains(evt.position)
				? _editable
				: null;

			Release(evt);

			if (tapped != null)
			{
				Tap(tapped);
			}
			else if (edited != null && edited.enabledInHierarchy)
			{
				edited.Focus();
			}
		}

		private void OnPointerCancel(PointerCancelEvent evt)
		{
			if (evt.pointerId == _pointerId)
			{
				Release(evt);
			}
			else
			{
				ForgetHandOff(evt.pointerId);
			}
		}

		private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
		{
			if (evt.pointerId != _pointerId || evt.target != _scroll)
			{
				return;
			}

			bool dragged = IsDragging;
			EndPointer();

			if (dragged)
			{
				StartInertia();
			}
		}

		private void OnWheel(WheelEvent evt) => StopInertia();

		private void Release(EventBase evt)
		{
			bool dragged = IsDragging;
			EndPointer();

			if (!dragged)
			{
				return;
			}

			evt.StopPropagation();
			StartInertia();
		}

		private void EndPointer()
		{
			int pointerId = _pointerId;

			_pointerId = PointerId.invalidPointerId;
			IsDragging = false;
			_coasting = false;
			_pressTarget = null;
			_adjustable = null;
			_editable = null;
			ClearPressed();

			if (pointerId != PointerId.invalidPointerId && _scroll.HasPointerCapture(pointerId))
			{
				_scroll.ReleasePointer(pointerId);
			}
		}

		private void ClearPressed()
		{
			_pressed?.RemoveFromClassList(OmniDebuggerUiClasses.Pressed);
			_pressed = null;
		}

		private void TrackVelocity(float step)
		{
			float now = Time.realtimeSinceStartup;
			float elapsed = now - _lastMoveTime;

			if (elapsed > Mathf.Epsilon)
			{
				_velocity = Mathf.Lerp(_velocity, step / elapsed, VelocitySmoothing);
			}

			_lastMoveTime = now;
		}

		private void StartInertia()
		{
			float now = Time.realtimeSinceStartup;
			float velocity = now - _lastMoveTime <= MaxReleaseDelay ? _velocity : 0.0f;

			StopInertia();

			if (Mathf.Abs(velocity) < MinVelocity)
			{
				return;
			}

			_velocity = Mathf.Clamp(velocity, -MaxVelocity, MaxVelocity);
			_lastInertiaTime = now;
			_inertia.Resume();
		}

		private void Coast()
		{
			float now = Time.realtimeSinceStartup;
			float elapsed = Mathf.Max(0.0f, now - _lastInertiaTime);
			_lastInertiaTime = now;

			float desired = AxisOffset() + _velocity * elapsed;
			float clamped = Clamp(desired);
			SetAxisOffset(clamped);

			_velocity *= Mathf.Pow(0.5f, elapsed / InertiaHalfLife);

			if (!Mathf.Approximately(desired, clamped) || Mathf.Abs(_velocity) < MinVelocity)
			{
				StopInertia();
			}
		}

		private void StopInertia()
		{
			_inertia?.Pause();
			_velocity = 0.0f;
		}

		private bool TryHandOff(IPointerEvent at)
		{
			VisualElement target = _pressTarget;

			if (_adjustable == null || !_adjustable.enabledInHierarchy || target == null)
			{
				return false;
			}

			int pointerId = _pointerId;
			EndPointer();
			_handOffPointerId = pointerId;

			_press.Set(at);
			using PointerDownEvent down = PointerDownEvent.GetPooled(_press);
			_press.Set(null);

			down.target = target;
			target.SendEvent(down);
			return true;
		}

		private void ForgetHandOff(int pointerId)
		{
			if (pointerId == _handOffPointerId)
			{
				_handOffPointerId = PointerId.invalidPointerId;
			}
		}

		private bool TryClassify(
			VisualElement element,
			string pointerType,
			out VisualElement adjustable,
			out VisualElement editable)
		{
			adjustable = null;
			editable = null;

			VisualElement slider = null;
			VisualElement field = null;

			while (element != null && element != _scroll)
			{
				if (element is Scroller)
				{
					return false;
				}

				if (slider == null && element.ClassListContains(BaseSlider<float>.ussClassName))
				{
					slider = element;
				}

				if (field == null && element.ClassListContains(TextInputBaseField<string>.ussClassName))
				{
					field = element;
				}

				element = element.parent;
			}

			if (pointerType == PointerType.mouse)
			{
				return slider == null && field == null;
			}

			if (slider != null)
			{
				string across = IsHorizontal
					? BaseSlider<float>.verticalVariantUssClassName
					: BaseSlider<float>.horizontalVariantUssClassName;

				if (!slider.ClassListContains(across))
				{
					return false;
				}

				adjustable = slider;
			}

			if (field != null)
			{
				if (field.focusController?.focusedElement is VisualElement focused && field.Contains(focused))
				{
					return false;
				}

				editable = field;
			}

			return true;
		}

		private VisualElement FindTappable(VisualElement element)
		{
			while (element != null && element != _scroll)
			{
				if (element is Button ||
					element is BaseBoolField ||
					element.ClassListContains(OmniDebuggerUiClasses.Tappable))
				{
					return element;
				}

				element = element.parent;
			}

			return null;
		}

		private float Clamp(float offset)
		{
			float content = Along(_scroll.contentContainer.layout.size);
			float viewport = Along(_scroll.contentViewport.layout.size);

			if (float.IsNaN(content) || float.IsNaN(viewport))
			{
				return 0.0f;
			}

			return Mathf.Clamp(offset, 0.0f, Mathf.Max(0.0f, content - viewport));
		}

		private float AxisOffset() => Along(_scroll.scrollOffset);

		private void SetAxisOffset(float value)
		{
			Scroller scroller = IsHorizontal ? _scroll.horizontalScroller : _scroll.verticalScroller;
			if (scroller.highValue < value)
			{
				scroller.highValue = value;
			}

			Vector2 offset = _scroll.scrollOffset;

			if (IsHorizontal)
			{
				offset.x = value;
			}
			else
			{
				offset.y = value;
			}

			_scroll.scrollOffset = offset;
		}

		private float Along(Vector2 value) => IsHorizontal ? value.x : value.y;

		private float Across(Vector2 value) => IsHorizontal ? value.y : value.x;
	}
}