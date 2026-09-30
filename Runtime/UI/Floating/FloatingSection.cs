using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class FloatingSection : VisualElement, IDisposable
	{
		internal const float BaseScale = 0.5f;
		internal const float MinScale = 0.5f;
		internal const float MaxScale = 3.0f;
		internal const float ScaleStep = 0.05f;

		private const string DockTooltip = "Stop floating";
		private const string ResizeTooltip = "Drag to resize";

		private readonly VisualElement _title;
		private readonly VisualElement _grip;
		private readonly VisualElement _rows;
		private readonly Func<Rect> _bounds;
		private readonly Action<FloatingSection> _moved;
		private readonly Action<FloatingSection> _resized;

		public string Key { get; }

		public VisualElement Rows => _rows;

		public float Scale => _scale;

		public bool IsPlaced { get; private set; }

		public Vector2 Position => new (resolvedStyle.left, resolvedStyle.top);

		public Vector2 ScaledSize
		{
			get
			{
				Vector2 size = layout.size;
				return float.IsNaN(size.x) || float.IsNaN(size.y) ? Vector2.zero : size * (BaseScale * _scale);
			}
		}

		private Gesture _gesture;
		private Vector2 _pointerStart;
		private Vector2 _positionStart;
		private float _scaleStart;
		private int _pointerId = PointerId.invalidPointerId;
		private float _scale = 1.0f;
		private bool _disposed;

		public FloatingSection(
			string key,
			string title,
			Action dock,
			Func<Rect> bounds,
			Action<FloatingSection> moved,
			Action<FloatingSection> resized)
		{
			Key = key;
			_bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
			_moved = moved;
			_resized = resized;

			AddToClassList(OmniDebuggerUiClasses.FloatingSection);
			this.AddManipulator(new Halo());

			_title = UiBuild.Element(OmniDebuggerUiClasses.FloatingSectionTitleBar);
			_title.Add(UiBuild.Label(title, OmniDebuggerUiClasses.FloatingSectionTitle));
			_title.Add(UiBuild.IconButton(IconGlyph.Dock, dock, DockTooltip));
			Add(_title);

			ScrollView scroll = UiBuild.Scroll();
			scroll.AddToClassList(OmniDebuggerUiClasses.FloatingSectionBody);
			Add(scroll);
			_rows = scroll.contentContainer;

			_grip = UiBuild.Element(OmniDebuggerUiClasses.FloatingSectionGrip);
			_grip.tooltip = ResizeTooltip;
			_grip.Add(new OmniIcon(IconGlyph.Resize));
			Add(_grip);

			Listen(_title, Gesture.Move);
			Listen(_grip, Gesture.Resize);
			RegisterCallback<PointerDownEvent>(OnAnyPointerDown, TrickleDown.TrickleDown);

			SetScale(1.0f);
		}

		public void SetScale(float scale)
		{
			_scale = Mathf.Clamp(scale > 0.0f ? scale : 1.0f, MinScale, MaxScale);
			float drawn = BaseScale * _scale;
			style.scale = new Scale(new Vector3(drawn, drawn, 1.0f));
		}

		public void MoveTo(Vector2 position)
		{
			Rect bounds = _bounds();
			Vector2 size = ScaledSize;

			style.left = Mathf.Clamp(position.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - size.x));
			style.top = Mathf.Clamp(position.y, bounds.yMin, Mathf.Max(bounds.yMin, bounds.yMax - size.y));
			IsPlaced = true;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			Release();
			RemoveFromHierarchy();
		}

		internal static float ResizeScale(float startScale, Vector2 size, Vector2 delta)
		{
			float extent = size.x + size.y;
			float scale = extent > 0.0f ? startScale + (delta.x + delta.y) / extent : startScale;
			scale = Mathf.Round(scale / ScaleStep) * ScaleStep;
			return Mathf.Clamp(scale, MinScale, MaxScale);
		}

		private static bool IsOnButton(VisualElement target, VisualElement handle)
		{
			for (VisualElement element = target; element != null && element != handle; element = element.parent)
			{
				if (element is Button)
				{
					return true;
				}
			}

			return false;
		}

		private void Listen(VisualElement handle, Gesture gesture)
		{
			handle.RegisterCallback<PointerDownEvent>(evt => Begin(evt, handle, gesture));
			handle.RegisterCallback<PointerMoveEvent>(Track);
			handle.RegisterCallback<PointerUpEvent>(evt => Finish(evt.pointerId));
			handle.RegisterCallback<PointerCancelEvent>(evt => Finish(evt.pointerId));
			handle.RegisterCallback<PointerCaptureOutEvent>(_ => Finish(_pointerId));
		}

		private void OnAnyPointerDown(PointerDownEvent evt) => BringToFront();

		private void Begin(PointerDownEvent evt, VisualElement handle, Gesture gesture)
		{
			if (evt.button != 0 || _pointerId != PointerId.invalidPointerId || IsOnButton(evt.target as VisualElement, handle))
			{
				return;
			}

			_gesture = gesture;
			_pointerId = evt.pointerId;
			_pointerStart = evt.position;
			_positionStart = Position;
			_scaleStart = _scale;
			handle.CapturePointer(_pointerId);
			evt.StopPropagation();
		}

		private void Track(PointerMoveEvent evt)
		{
			if (evt.pointerId != _pointerId)
			{
				return;
			}

			Vector2 delta = (Vector2)evt.position - _pointerStart;

			if (_gesture == Gesture.Move)
			{
				if (delta.magnitude < TouchSlop.Distance)
				{
					return;
				}

				_gesture = Gesture.Moving;
			}

			if (_gesture == Gesture.Moving)
			{
				MoveTo(_positionStart + delta);
				return;
			}

			float scale = ResizeScale(_scaleStart, layout.size * BaseScale, delta);

			if (!Mathf.Approximately(scale, _scale))
			{
				SetScale(scale);
				MoveTo(Position);
			}
		}

		private void Finish(int pointerId)
		{
			if (pointerId != _pointerId || _pointerId == PointerId.invalidPointerId)
			{
				return;
			}

			Gesture gesture = _gesture;
			Release();

			if (gesture == Gesture.Moving)
			{
				_moved?.Invoke(this);
			}
			else if (gesture == Gesture.Resize && !Mathf.Approximately(_scaleStart, _scale))
			{
				_resized?.Invoke(this);
			}
		}

		private void Release()
		{
			int pointerId = _pointerId;
			_pointerId = PointerId.invalidPointerId;
			_gesture = Gesture.None;

			if (pointerId == PointerId.invalidPointerId)
			{
				return;
			}

			if (_title.HasPointerCapture(pointerId))
			{
				_title.ReleasePointer(pointerId);
			}

			if (_grip.HasPointerCapture(pointerId))
			{
				_grip.ReleasePointer(pointerId);
			}
		}

		private enum Gesture : byte
		{
			None,
			Move,
			Moving,
			Resize,
		}
	}
}
