using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal readonly struct IconPen
	{
		private readonly Painter2D _painter;
		private readonly Vector2 _origin;
		private readonly float _scale;

		public IconPen(Painter2D painter, Vector2 origin, float scale)
		{
			_painter = painter;
			_origin = origin;
			_scale = scale;
		}

		public void Line(float x1, float y1, float x2, float y2)
		{
			_painter.BeginPath();
			_painter.MoveTo(Map(x1, y1));
			_painter.LineTo(Map(x2, y2));
			_painter.Stroke();
		}

		public void Polyline(params float[] points) => Path(points, close: false, fill: false);

		public void Polygon(params float[] points) => Path(points, close: true, fill: false);

		public void FilledPolygon(params float[] points) => Path(points, close: true, fill: true);

		public void Circle(float x, float y, float radius) => Round(x, y, radius, fill: false);

		public void Dot(float x, float y, float radius) => Round(x, y, radius, fill: true);

		public void Crescent(
			float outerX,
			float outerY,
			float outerRadius,
			float outerFrom,
			float outerTo,
			float innerX,
			float innerY,
			float innerRadius,
			float innerFrom,
			float innerTo)
		{
			_painter.BeginPath();
			_painter.Arc(
				Map(outerX, outerY),
				outerRadius * _scale,
				Angle.Degrees(outerFrom),
				Angle.Degrees(outerTo),
				ArcDirection.Clockwise);
			_painter.Arc(
				Map(innerX, innerY),
				innerRadius * _scale,
				Angle.Degrees(innerFrom),
				Angle.Degrees(innerTo),
				ArcDirection.CounterClockwise);
			_painter.ClosePath();
			_painter.Fill();
		}

		private void Round(float x, float y, float radius, bool fill)
		{
			_painter.BeginPath();
			_painter.Arc(Map(x, y), radius * _scale, Angle.Degrees(0.0f), Angle.Degrees(360.0f));
			_painter.ClosePath();

			if (fill)
			{
				_painter.Fill();
			}
			else
			{
				_painter.Stroke();
			}
		}

		private void Path(float[] points, bool close, bool fill)
		{
			if (points == null || points.Length < 4)
			{
				return;
			}

			_painter.BeginPath();
			_painter.MoveTo(Map(points[0], points[1]));

			for (int i = 2; i + 1 < points.Length; i += 2)
			{
				_painter.LineTo(Map(points[i], points[i + 1]));
			}

			if (close)
			{
				_painter.ClosePath();
			}

			if (fill)
			{
				_painter.Fill();
			}
			else
			{
				_painter.Stroke();
			}
		}

		private Vector2 Map(float x, float y) => new (_origin.x + x * _scale, _origin.y + y * _scale);
	}
}