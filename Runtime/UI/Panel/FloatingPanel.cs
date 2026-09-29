using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class FloatingPanel : IDisposable
	{
		private const float FullSize = 100.0f;
		private const float BaseScale = 0.315f;

		private readonly VisualElement _stage;
		private readonly VisualElement _panel;
		private readonly OmniDebuggerViewState _state;
		private readonly List<VisualElement> _handles = new ();

		private VisualElement _dragHandle;
		private Vector2 _pointerStart;
		private Vector2 _offsetStart;
		private int _pointerId = PointerId.invalidPointerId;
		private float _scale = 1.0f;
		private bool _active;
		private bool _dragging;
		private bool _disposed;

		public FloatingPanel(VisualElement stage, VisualElement panel, OmniDebuggerViewState state)
		{
			_stage = stage ?? throw new ArgumentNullException(nameof(stage));
			_panel = panel ?? throw new ArgumentNullException(nameof(panel));
			_state = state ?? throw new ArgumentNullException(nameof(state));

			_stage.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			_panel.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		public void AddHandle(VisualElement handle)
		{
			if (handle == null)
			{
				throw new ArgumentNullException(nameof(handle));
			}

			handle.RegisterCallback<PointerDownEvent>(OnPointerDown);
			handle.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			handle.RegisterCallback<PointerUpEvent>(OnPointerUp);
			handle.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
			handle.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
			_handles.Add(handle);
		}

		public void SetActive(bool active)
		{
			if (!active)
			{
				EndDrag();
			}

			_active = active;
			Apply();
		}

		public void SetScale(float scale)
		{
			_scale = BaseScale * (scale > 0.0f ? scale : 1.0f);
			Apply();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			EndDrag();

			_stage.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			_panel.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			for (int i = 0; i < _handles.Count; i++)
			{
				VisualElement handle = _handles[i];
				handle.UnregisterCallback<PointerDownEvent>(OnPointerDown);
				handle.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
				handle.UnregisterCallback<PointerUpEvent>(OnPointerUp);
				handle.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
				handle.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
			}

			_handles.Clear();
		}

		private static bool IsInteractive(VisualElement target, VisualElement handle)
		{
			for (VisualElement element = target; element != null && element != handle; element = element.parent)
			{
				if (element.focusable || element is Button)
				{
					return true;
				}
			}

			return false;
		}

		private void Apply()
		{
			if (!_active)
			{
				_panel.style.width = StyleKeyword.Null;
				_panel.style.height = StyleKeyword.Null;
				_panel.style.scale = StyleKeyword.Null;
				_panel.style.translate = StyleKeyword.Null;
				return;
			}

			float size = FullSize / _scale;
			_panel.style.width = Length.Percent(size);
			_panel.style.height = Length.Percent(size);
			_panel.style.scale = new Scale(new Vector3(_scale, _scale, 1.0f));
			Show(Clamp(_state.FloatingOffset));
		}

		private void Show(Vector2 offset) => _panel.style.translate = new Translate(offset.x, offset.y);

		private Vector2 Clamp(Vector2 offset)
		{
			Rect area = _stage.contentRect;
			Vector2 size = _panel.layout.size * _scale;

			if (!(area.width > 0.0f) || !(area.height > 0.0f) || !(size.x > 0.0f) || !(size.y > 0.0f))
			{
				return offset;
			}

			float maxX = Mathf.Max(0.0f, (area.width - size.x) * 0.5f);
			float maxY = Mathf.Max(0.0f, (area.height - size.y) * 0.5f);

			return new Vector2(Mathf.Clamp(offset.x, -maxX, maxX), Mathf.Clamp(offset.y, -maxY, maxY));
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			if (_active)
			{
				Show(Clamp(_state.FloatingOffset));
			}
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			if (!_active || evt.button != 0 || _pointerId != PointerId.invalidPointerId)
			{
				return;
			}

			if (evt.currentTarget is not VisualElement handle || IsInteractive(evt.target as VisualElement, handle))
			{
				return;
			}

			_dragHandle = handle;
			_pointerId = evt.pointerId;
			_pointerStart = evt.position;
			_offsetStart = Clamp(_state.FloatingOffset);
			_dragging = false;
			handle.CapturePointer(_pointerId);
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				return;
			}

			Vector2 delta = (Vector2)evt.position - _pointerStart;

			if (!_dragging && delta.magnitude < TouchSlop.Distance)
			{
				return;
			}

			_dragging = true;

			Vector2 offset = Clamp(_offsetStart + delta);
			_state.FloatingOffset = offset;
			Show(offset);
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (evt.pointerId == _pointerId)
			{
				EndDrag();
			}
		}

		private void OnPointerCancel(PointerCancelEvent evt)
		{
			if (evt.pointerId == _pointerId)
			{
				EndDrag();
			}
		}

		private void OnCaptureOut(PointerCaptureOutEvent evt)
		{
			if (evt.target == _dragHandle)
			{
				EndDrag();
			}
		}

		private void EndDrag()
		{
			int pointerId = _pointerId;
			VisualElement handle = _dragHandle;

			_pointerId = PointerId.invalidPointerId;
			_dragHandle = null;
			_dragging = false;

			if (handle != null && pointerId != PointerId.invalidPointerId && handle.HasPointerCapture(pointerId))
			{
				handle.ReleasePointer(pointerId);
			}
		}
	}
}
