using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class InfoGraph : VisualElement
	{
		private const float LineWidth = 1.5f;
		private const float FillAlpha = 0.15f;

		private readonly InfoSamples _samples;

		public InfoGraph(InfoSamples samples)
		{
			_samples = samples;
			pickingMode = PickingMode.Ignore;
			AddToClassList(OmniDebuggerUiClasses.InfoGraph);
			generateVisualContent += Draw;
		}

		private static Vector2 Point(Rect rect, float x, float value, float min, float range)
		{
			float t = Mathf.Clamp01((value - min) / range);
			return new Vector2(x, rect.yMax - t * rect.height);
		}

		private void Draw(MeshGenerationContext context)
		{
			Rect rect = contentRect;
			int count = _samples.Count;

			if (count < 2 || rect.width <= 0.0f || rect.height <= 0.0f)
			{
				return;
			}

			float min = _samples.Min;
			float range = Mathf.Max(0.0001f, _samples.Top - min);
			float step = rect.width / (InfoSamples.Capacity - 1);
			float startX = rect.xMax - step * (count - 1);

			Painter2D painter = context.painter2D;
			Color color = resolvedStyle.color;

			painter.BeginPath();
			painter.MoveTo(new Vector2(startX, rect.yMax));

			for (int i = 0; i < count; i++)
			{
				painter.LineTo(Point(rect, startX + step * i, _samples[i], min, range));
			}

			painter.LineTo(new Vector2(rect.xMax, rect.yMax));
			painter.ClosePath();
			painter.fillColor = new Color(color.r, color.g, color.b, color.a * FillAlpha);
			painter.Fill();

			painter.BeginPath();
			painter.MoveTo(Point(rect, startX, _samples[0], min, range));

			for (int i = 1; i < count; i++)
			{
				painter.LineTo(Point(rect, startX + step * i, _samples[i], min, range));
			}

			painter.strokeColor = color;
			painter.lineWidth = LineWidth;
			painter.lineJoin = LineJoin.Round;
			painter.Stroke();
		}
	}
}
