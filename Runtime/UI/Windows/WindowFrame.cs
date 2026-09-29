using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class WindowFrame : VisualElement, IDisposable
	{
		private const long FadeDelayMs = 1500;
		private const float DragThreshold = 4.0f;

		private readonly WindowRegistration _registration;
		private readonly VisualElement _header;
		private readonly Button _collapse;
		private readonly VisualElement _content;
		private readonly VisualElement _footer;
		private readonly List<IDisposable> _owned = new ();
		private readonly Func<Rect> _bounds;
		private readonly Action<WindowFrame> _moved;
		private readonly IVisualElementScheduledItem _fade;

		public WindowRegistration Registration => _registration;

		public VisualElement Content => _content;

		public VisualElement Footer => _footer;

		public Vector2 Position => new (resolvedStyle.left, resolvedStyle.top);

		private Vector2 _pointerStart;
		private Vector2 _positionStart;
		private int _pointerId = PointerId.invalidPointerId;
		private float _scale = 1.0f;
		private bool _dragging;
		private bool _disposed;

		public WindowFrame(WindowRegistration registration, Func<Rect> bounds, Action<WindowFrame> moved)
		{
			_registration = registration;
			_bounds = bounds;
			_moved = moved;

			AddToClassList(OmniDebuggerUiClasses.Window);
			this.AddManipulator(new Halo());

			if (registration.Size.x > 0.0f)
			{
				style.width = registration.Size.x;
			}

			if (registration.Size.y > 0.0f)
			{
				style.maxHeight = registration.Size.y;
			}

			_header = UiBuild.Element(OmniDebuggerUiClasses.WindowHeader);
			_header.Add(UiBuild.Label(registration.Title, OmniDebuggerUiClasses.WindowTitle));
			_collapse = UiBuild.IconButton(IconGlyph.Minus, ToggleCollapsed, "Collapse");
			_header.Add(_collapse);
			_header.Add(UiBuild.IconButton(IconGlyph.Close, registration.Close, "Close"));
			Add(_header);

			ScrollView scroll = UiBuild.Scroll();
			scroll.AddToClassList(OmniDebuggerUiClasses.WindowContent);
			Add(scroll);
			_content = scroll.contentContainer;

			_footer = UiBuild.Element(OmniDebuggerUiClasses.WindowFooter);
			Add(_footer);

			_header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
			_header.RegisterCallback<PointerMoveEvent>(OnHeaderPointerMove);
			_header.RegisterCallback<PointerUpEvent>(OnHeaderPointerUp);
			_header.RegisterCallback<PointerCaptureOutEvent>(OnHeaderCaptureOut);
			RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			RegisterCallback<PointerEnterEvent>(OnPointerEnter);
			RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

			_fade = schedule.Execute(Deactivate);
			_fade.Pause();

			ApplyCollapsed();
		}

		public void Own(IDisposable disposable)
		{
			if (disposable != null)
			{
				_owned.Add(disposable);
			}
		}

		public void ShowFooter() => AddToClassList(OmniDebuggerUiClasses.WindowWithFooter);

		public void ApplyCollapsed()
		{
			EnableInClassList(OmniDebuggerUiClasses.WindowCollapsed, _registration.IsCollapsed);
			UiBuild.SetGlyph(_collapse, _registration.IsCollapsed ? IconGlyph.Plus : IconGlyph.Minus);
		}

		public void SetScale(float scale)
		{
			_scale = scale > 0.0f ? scale : 1.0f;
			style.scale = new Scale(new Vector3(_scale, _scale, 1.0f));
		}

		public void MoveTo(Vector2 position)
		{
			Rect bounds = _bounds();
			float width = resolvedStyle.width * _scale;
			float height = resolvedStyle.height * _scale;

			if (float.IsNaN(width) || float.IsNaN(height))
			{
				width = 0.0f;
				height = 0.0f;
			}

			float maxX = Mathf.Max(bounds.xMin, bounds.xMax - width);
			float maxY = Mathf.Max(bounds.yMin, bounds.yMax - height);

			style.left = Mathf.Clamp(position.x, bounds.xMin, maxX);
			style.top = Mathf.Clamp(position.y, bounds.yMin, maxY);
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_fade.Pause();

			for (int i = 0; i < _owned.Count; i++)
			{
				_owned[i].Dispose();
			}

			_owned.Clear();
			RemoveFromHierarchy();
		}

		private void ToggleCollapsed() => _registration.SetCollapsed(!_registration.IsCollapsed);

		private void OnPointerDown(PointerDownEvent evt)
		{
			_fade.Pause();
			AddToClassList(OmniDebuggerUiClasses.WindowActive);
			BringToFront();
		}

		private void OnPointerEnter(PointerEnterEvent evt) => _fade.Pause();

		private void OnPointerLeave(PointerLeaveEvent evt)
		{
			if (!_dragging)
			{
				_fade.ExecuteLater(FadeDelayMs);
			}
		}

		private void Deactivate() => RemoveFromClassList(OmniDebuggerUiClasses.WindowActive);

		private void OnHeaderPointerDown(PointerDownEvent evt)
		{
			if (evt.button != 0 || evt.target is Button || (evt.target as VisualElement)?.parent is Button)
			{
				return;
			}

			_pointerId = evt.pointerId;
			_pointerStart = evt.position;
			_positionStart = Position;
			_dragging = false;
			_header.CapturePointer(_pointerId);
		}

		private void OnHeaderPointerMove(PointerMoveEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				return;
			}

			Vector2 delta = (Vector2)evt.position - _pointerStart;

			if (!_dragging && delta.magnitude < DragThreshold)
			{
				return;
			}

			_dragging = true;
			MoveTo(_positionStart + delta);
		}

		private void OnHeaderPointerUp(PointerUpEvent evt)
		{
			if (evt.pointerId == _pointerId)
			{
				EndDrag();
			}
		}

		private void OnHeaderCaptureOut(PointerCaptureOutEvent evt) => EndDrag();

		private void EndDrag()
		{
			int pointerId = _pointerId;
			bool dragged = _dragging;

			_pointerId = PointerId.invalidPointerId;
			_dragging = false;

			if (pointerId != PointerId.invalidPointerId && _header.HasPointerCapture(pointerId))
			{
				_header.ReleasePointer(pointerId);
			}

			if (dragged)
			{
				_moved?.Invoke(this);
			}
		}
	}
}