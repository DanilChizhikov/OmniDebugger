using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class DragCornersIndicator : VisualElement
	{
		private const float CornerLength = 0.3f;
		private const float CornerThickness = 0.1f;
		private const float Offset = 0.7f;
		private const float ShowDuration = 0.16f;
		private const float HideDuration = 0.12f;
		private const long FrameMs = 16;

		private IVisualElementScheduledItem _animation;
		private float _progress;
		private float _from;
		private float _to;
		private float _startTime;
		private float _duration;

		public DragCornersIndicator()
		{
			pickingMode = PickingMode.Ignore;
			AddToClassList(OmniDebuggerUiClasses.OpenButtonCorners);
			generateVisualContent += Draw;
		}

		public void Show() => AnimateTo(1.0f, ShowDuration);

		public void Hide() => AnimateTo(0.0f, HideDuration);

		public void HideInstantly()
		{
			_animation?.Pause();
			_progress = 0.0f;
			_to = 0.0f;
			MarkDirtyRepaint();
		}

		private static void Corner(Painter2D painter, float x1, float y1, float x2, float y2, float x3, float y3)
		{
			painter.BeginPath();
			painter.MoveTo(new Vector2(x1, y1));
			painter.LineTo(new Vector2(x2, y2));
			painter.LineTo(new Vector2(x3, y3));
			painter.Stroke();
		}

		private void AnimateTo(float target, float duration)
		{
			if (Mathf.Approximately(_to, target) && _animation != null && _animation.isActive)
			{
				return;
			}

			_from = _progress;
			_to = target;
			_duration = duration;
			_startTime = Time.realtimeSinceStartup;

			_animation ??= schedule.Execute(Tick).Every(FrameMs);
			_animation.Resume();
		}

		private void Tick()
		{
			float t = _duration <= 0.0f ? 1.0f : Mathf.Clamp01((Time.realtimeSinceStartup - _startTime) / _duration);
			float eased = 1.0f - Mathf.Pow(1.0f - t, 3.0f);

			_progress = Mathf.Lerp(_from, _to, eased);
			MarkDirtyRepaint();

			if (t >= 1.0f)
			{
				_animation.Pause();
			}
		}

		private void Draw(MeshGenerationContext context)
		{
			Rect rect = contentRect;

			if (_progress <= 0.001f || rect.width <= 0.0f)
			{
				return;
			}

			float size = Mathf.Min(rect.width, rect.height);
			float extent = size * Offset * _progress * 0.5f;
			float length = size * CornerLength;
			float thickness = size * CornerThickness;

			Rect frame = new Rect(
				rect.x - extent,
				rect.y - extent,
				rect.width + extent * 2.0f,
				rect.height + extent * 2.0f);

			Color color = resolvedStyle.color;
			color.a *= _progress;

			Painter2D painter = context.painter2D;
			painter.strokeColor = color;
			painter.lineWidth = thickness;
			painter.lineCap = LineCap.Butt;
			painter.lineJoin = LineJoin.Miter;

			float inset = thickness * 0.5f;
			float left = frame.xMin + inset;
			float right = frame.xMax - inset;
			float top = frame.yMin + inset;
			float bottom = frame.yMax - inset;

			Corner(painter, left, top + length, left, top, left + length, top);
			Corner(painter, right - length, top, right, top, right, top + length);
			Corner(painter, right, bottom - length, right, bottom, right - length, bottom);
			Corner(painter, left + length, bottom, left, bottom, left, bottom - length);
		}
	}
}