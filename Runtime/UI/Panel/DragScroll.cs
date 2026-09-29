using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class DragScroll : Manipulator
	{
		public event Action OnDragStarted;

		private const float DragThreshold = 8.0f;
		private const float VelocitySmoothing = 0.35f;
		private const float DampingPerFrame = 0.95f;
		private const float FramesPerSecond = 60.0f;
		private const float MinVelocity = 20.0f;
		private const float MaxVelocity = 3200.0f;
		private const float MaxReleaseDelay = 0.12f;
		private const long InertiaIntervalMs = 16;
		private const int PrimaryButtonMask = 1;

		public bool IsDragging { get; private set; }
		
		private bool IsHorizontal => _scroll.mode == ScrollViewMode.Horizontal;

		private ScrollView _scroll;
		private IVisualElementScheduledItem _inertia;
		private VisualElement _pressed;
		private Vector2 _pointerStart;
		private Vector2 _pointerLast;
		private float _lastMoveTime;
		private float _lastInertiaTime;
		private float _velocity;
		private int _pointerId = PointerId.invalidPointerId;

		protected override void RegisterCallbacksOnTarget()
		{
			_scroll = target as ScrollView ?? throw new InvalidOperationException("A drag scroll drives a ScrollView only.");
			_inertia = _scroll.schedule.Execute(Coast).Every(InertiaIntervalMs);
			_inertia.Pause();

			_scroll.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			_scroll.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
			_scroll.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			_scroll.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
			_scroll.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
			_scroll.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			_scroll.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			_scroll.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
			_scroll.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			_scroll.UnregisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
			_scroll.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
			_scroll.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);

			StopInertia();
			EndPointer();
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

		private void OnPointerDown(PointerDownEvent evt)
		{
			VisualElement pressed = evt.target as VisualElement;

			if (evt.button != 0 || !evt.isPrimary || IsExempt(pressed))
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

			StopInertia();

			_pointerId = evt.pointerId;
			_pointerStart = _scroll.WorldToLocal(evt.position);
			_pointerLast = _pointerStart;
			_lastMoveTime = Time.realtimeSinceStartup;
			IsDragging = false;

			_pressed = FindTappable(pressed);
			_pressed?.AddToClassList(OmniDebuggerUiClasses.Pressed);

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

				if (along < DragThreshold || along <= Mathf.Abs(Across(travel)))
				{
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
				return;
			}

			VisualElement tapped = !IsDragging && _pressed != null && _pressed.worldBound.Contains(evt.position)
				? _pressed
				: null;

			Release(evt);

			if (tapped != null)
			{
				Tap(tapped);
			}
		}

		private void OnPointerCancel(PointerCancelEvent evt)
		{
			if (evt.pointerId == _pointerId)
			{
				Release(evt);
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

			_velocity *= Mathf.Pow(DampingPerFrame, elapsed * FramesPerSecond);

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

		private bool IsExempt(VisualElement element)
		{
			while (element != null && element != _scroll)
			{
				if (element is Scroller || element.ClassListContains(TextInputBaseField<string>.ussClassName))
				{
					return true;
				}

				element = element.parent;
			}

			return false;
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