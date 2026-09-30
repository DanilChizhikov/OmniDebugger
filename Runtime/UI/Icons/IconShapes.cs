using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal static class IconShapes
	{
		private const int StarPoints = 5;
		private const float StarOuter = 9.5f;
		private const float StarInner = 4.2f;

		public static void Draw(IconGlyph glyph, in IconPen pen)
		{
			switch (glyph)
			{
				case IconGlyph.Close:
					pen.Line(6.0f, 6.0f, 18.0f, 18.0f);
					pen.Line(18.0f, 6.0f, 6.0f, 18.0f);
					break;
				case IconGlyph.Back:
					pen.Polyline(15.0f, 5.0f, 8.0f, 12.0f, 15.0f, 19.0f);
					break;
				case IconGlyph.Chevron:
					pen.Polyline(9.0f, 5.0f, 16.0f, 12.0f, 9.0f, 19.0f);
					break;
				case IconGlyph.TriangleDown:
					pen.FilledPolygon(6.0f, 9.0f, 18.0f, 9.0f, 12.0f, 16.0f);
					break;
				case IconGlyph.TriangleUp:
					pen.FilledPolygon(6.0f, 15.0f, 18.0f, 15.0f, 12.0f, 8.0f);
					break;
				case IconGlyph.TriangleRight:
					pen.FilledPolygon(9.0f, 6.0f, 16.0f, 12.0f, 9.0f, 18.0f);
					break;
				case IconGlyph.Star:
					DrawStar(pen, fill: true);
					break;
				case IconGlyph.StarOutline:
					DrawStar(pen, fill: false);
					break;
				case IconGlyph.Pin:
					pen.FilledPolygon(9.0f, 3.0f, 15.0f, 3.0f, 14.0f, 10.0f, 17.5f, 14.0f, 6.5f, 14.0f, 10.0f, 10.0f);
					pen.Line(12.0f, 14.0f, 12.0f, 21.0f);
					break;
				case IconGlyph.PinOutline:
					pen.Polygon(9.0f, 3.0f, 15.0f, 3.0f, 14.0f, 10.0f, 17.5f, 14.0f, 6.5f, 14.0f, 10.0f, 10.0f);
					pen.Line(12.0f, 14.0f, 12.0f, 21.0f);
					break;
				case IconGlyph.Search:
					pen.Circle(10.5f, 10.5f, 6.0f);
					pen.Line(15.0f, 15.0f, 20.0f, 20.0f);
					break;
				case IconGlyph.Copy:
					pen.Polygon(9.0f, 9.0f, 20.0f, 9.0f, 20.0f, 20.0f, 9.0f, 20.0f);
					pen.Polyline(5.0f, 15.0f, 4.0f, 15.0f, 4.0f, 4.0f, 15.0f, 4.0f, 15.0f, 5.0f);
					break;
				case IconGlyph.Info:
					pen.Circle(12.0f, 12.0f, 9.0f);
					pen.Line(12.0f, 11.0f, 12.0f, 17.0f);
					pen.Dot(12.0f, 7.5f, 1.3f);
					break;
				case IconGlyph.Message:
					pen.Polygon(4.0f, 5.0f, 20.0f, 5.0f, 20.0f, 16.0f, 12.0f, 16.0f, 8.0f, 20.0f, 8.0f, 16.0f, 4.0f, 16.0f);
					break;
				case IconGlyph.Warning:
					pen.Polygon(12.0f, 3.5f, 21.0f, 20.0f, 3.0f, 20.0f);
					pen.Line(12.0f, 9.5f, 12.0f, 14.0f);
					pen.Dot(12.0f, 17.0f, 1.3f);
					break;
				case IconGlyph.Error:
					pen.Circle(12.0f, 12.0f, 9.0f);
					pen.Line(9.0f, 9.0f, 15.0f, 15.0f);
					pen.Line(15.0f, 9.0f, 9.0f, 15.0f);
					break;
				case IconGlyph.ArrowDown:
					pen.Line(12.0f, 4.0f, 12.0f, 19.0f);
					pen.Polyline(6.0f, 13.0f, 12.0f, 19.0f, 18.0f, 13.0f);
					break;
				case IconGlyph.Minus:
					pen.Line(5.0f, 12.0f, 19.0f, 12.0f);
					break;
				case IconGlyph.Plus:
					pen.Line(5.0f, 12.0f, 19.0f, 12.0f);
					pen.Line(12.0f, 5.0f, 12.0f, 19.0f);
					break;
				case IconGlyph.Expand:
					pen.Polyline(4.0f, 9.0f, 4.0f, 4.0f, 9.0f, 4.0f);
					pen.Polyline(15.0f, 4.0f, 20.0f, 4.0f, 20.0f, 9.0f);
					pen.Polyline(20.0f, 15.0f, 20.0f, 20.0f, 15.0f, 20.0f);
					pen.Polyline(9.0f, 20.0f, 4.0f, 20.0f, 4.0f, 15.0f);
					break;
				case IconGlyph.Alert:
					pen.Circle(12.0f, 12.0f, 9.5f);
					pen.Line(12.0f, 6.5f, 12.0f, 13.5f);
					pen.Dot(12.0f, 17.0f, 1.5f);
					break;
				case IconGlyph.Sun:
					DrawSun(pen);
					break;
				case IconGlyph.Moon:
					pen.Crescent(12.0f, 12.0f, 8.0f, 23.1f, 263.1f, 16.0f, 9.0f, 7.0f, 224.9f, 61.3f);
					break;
				case IconGlyph.Check:
					pen.Polyline(5.0f, 12.5f, 10.0f, 17.5f, 19.0f, 7.0f);
					break;
				case IconGlyph.Trash:
					pen.Line(4.0f, 6.5f, 20.0f, 6.5f);
					pen.Polyline(9.5f, 6.5f, 9.5f, 4.0f, 14.5f, 4.0f, 14.5f, 6.5f);
					pen.Polyline(6.0f, 6.5f, 7.0f, 20.0f, 17.0f, 20.0f, 18.0f, 6.5f);
					pen.Line(10.0f, 10.0f, 10.0f, 16.5f);
					pen.Line(14.0f, 10.0f, 14.0f, 16.5f);
					break;
				case IconGlyph.Sliders:
					pen.Line(4.0f, 7.0f, 20.0f, 7.0f);
					pen.Line(4.0f, 12.0f, 20.0f, 12.0f);
					pen.Line(4.0f, 17.0f, 20.0f, 17.0f);
					pen.Dot(15.0f, 7.0f, 2.3f);
					pen.Dot(8.5f, 12.0f, 2.3f);
					pen.Dot(13.0f, 17.0f, 2.3f);
					break;
				case IconGlyph.Terminal:
					pen.Polygon(3.5f, 5.0f, 20.5f, 5.0f, 20.5f, 19.0f, 3.5f, 19.0f);
					pen.Polyline(7.5f, 9.5f, 10.5f, 12.0f, 7.5f, 14.5f);
					pen.Line(12.5f, 15.0f, 16.5f, 15.0f);
					break;
				case IconGlyph.Window:
					pen.Polygon(3.5f, 5.0f, 20.5f, 5.0f, 20.5f, 19.0f, 3.5f, 19.0f);
					pen.Line(3.5f, 9.5f, 20.5f, 9.5f);
					break;
				case IconGlyph.Grid:
					pen.Polygon(4.0f, 4.0f, 10.5f, 4.0f, 10.5f, 10.5f, 4.0f, 10.5f);
					pen.Polygon(13.5f, 4.0f, 20.0f, 4.0f, 20.0f, 10.5f, 13.5f, 10.5f);
					pen.Polygon(4.0f, 13.5f, 10.5f, 13.5f, 10.5f, 20.0f, 4.0f, 20.0f);
					pen.Polygon(13.5f, 13.5f, 20.0f, 13.5f, 20.0f, 20.0f, 13.5f, 20.0f);
					break;
				case IconGlyph.More:
					pen.Dot(6.0f, 12.0f, 1.7f);
					pen.Dot(12.0f, 12.0f, 1.7f);
					pen.Dot(18.0f, 12.0f, 1.7f);
					break;
				case IconGlyph.ChevronDown:
					pen.Polyline(6.0f, 9.0f, 12.0f, 15.0f, 18.0f, 9.0f);
					break;
				case IconGlyph.Play:
					pen.FilledPolygon(8.0f, 5.0f, 19.0f, 12.0f, 8.0f, 19.0f);
					break;
				case IconGlyph.Refresh:
					pen.Arc(12.0f, 12.0f, 7.0f, 40.0f, 320.0f);
					pen.Polyline(18.0f, 3.5f, 17.4f, 7.5f, 13.5f, 7.0f);
					break;
				case IconGlyph.Gear:
					DrawGear(pen);
					break;
				case IconGlyph.Bug:
					pen.Circle(12.0f, 14.0f, 5.0f);
					pen.Circle(12.0f, 6.5f, 2.3f);
					pen.Line(12.0f, 9.0f, 12.0f, 19.0f);
					pen.Line(7.0f, 11.5f, 3.5f, 9.5f);
					pen.Line(7.0f, 14.5f, 3.5f, 14.5f);
					pen.Line(7.5f, 17.5f, 4.0f, 20.0f);
					pen.Line(17.0f, 11.5f, 20.5f, 9.5f);
					pen.Line(17.0f, 14.5f, 20.5f, 14.5f);
					pen.Line(16.5f, 17.5f, 20.0f, 20.0f);
					break;
				case IconGlyph.Bolt:
					pen.FilledPolygon(13.5f, 2.0f, 5.0f, 13.5f, 11.5f, 13.5f, 10.5f, 22.0f, 19.0f, 10.5f, 12.5f, 10.5f);
					break;
				case IconGlyph.Eye:
					pen.Polygon(2.0f, 12.0f, 6.0f, 7.5f, 12.0f, 5.5f, 18.0f, 7.5f, 22.0f, 12.0f, 18.0f, 16.5f, 12.0f, 18.5f, 6.0f, 16.5f);
					pen.Circle(12.0f, 12.0f, 3.0f);
					break;
				case IconGlyph.Clock:
					pen.Circle(12.0f, 12.0f, 9.0f);
					pen.Polyline(12.0f, 7.0f, 12.0f, 12.0f, 15.5f, 14.0f);
					break;
				case IconGlyph.Heart:
					pen.Polygon(12.0f, 20.0f, 4.0f, 12.5f, 3.5f, 8.0f, 6.0f, 5.0f, 9.5f, 5.0f, 12.0f, 8.0f, 14.5f, 5.0f, 18.0f, 5.0f, 20.5f, 8.0f, 20.0f, 12.5f);
					break;
				case IconGlyph.Flag:
					pen.Line(6.0f, 3.0f, 6.0f, 21.0f);
					pen.Polygon(6.0f, 4.0f, 18.0f, 4.0f, 15.0f, 8.5f, 18.0f, 13.0f, 6.0f, 13.0f);
					break;
				case IconGlyph.Coin:
					pen.Circle(12.0f, 12.0f, 9.0f);
					pen.Circle(12.0f, 12.0f, 5.5f);
					break;
				case IconGlyph.User:
					pen.Circle(12.0f, 8.0f, 4.0f);
					pen.Polyline(4.5f, 20.0f, 5.5f, 16.5f, 9.0f, 14.5f, 15.0f, 14.5f, 18.5f, 16.5f, 19.5f, 20.0f);
					break;
				case IconGlyph.Keyboard:
					pen.Polygon(3.0f, 6.0f, 21.0f, 6.0f, 21.0f, 18.0f, 3.0f, 18.0f);
					pen.Dot(7.0f, 10.0f, 1.0f);
					pen.Dot(11.0f, 10.0f, 1.0f);
					pen.Dot(15.0f, 10.0f, 1.0f);
					pen.Dot(19.0f, 10.0f, 1.0f);
					pen.Line(8.0f, 14.5f, 16.0f, 14.5f);
					break;
				case IconGlyph.Chart:
					pen.Polyline(3.0f, 3.0f, 3.0f, 21.0f, 21.0f, 21.0f);
					pen.Polyline(6.0f, 16.0f, 10.0f, 11.0f, 14.0f, 14.0f, 20.0f, 6.0f);
					break;
				case IconGlyph.Filter:
					pen.Polygon(3.0f, 5.0f, 21.0f, 5.0f, 14.0f, 13.0f, 14.0f, 19.0f, 10.0f, 21.0f, 10.0f, 13.0f);
					break;
				case IconGlyph.PopOut:
					pen.Polyline(11.0f, 4.5f, 4.5f, 4.5f, 4.5f, 19.5f, 19.5f, 19.5f, 19.5f, 13.0f);
					pen.Line(11.0f, 13.0f, 20.0f, 4.0f);
					pen.Polyline(14.0f, 4.0f, 20.0f, 4.0f, 20.0f, 10.0f);
					break;
				case IconGlyph.Dock:
					pen.Polyline(4.0f, 13.0f, 4.0f, 20.0f, 20.0f, 20.0f, 20.0f, 13.0f);
					pen.Line(12.0f, 3.5f, 12.0f, 14.5f);
					pen.Polyline(7.5f, 10.0f, 12.0f, 14.5f, 16.5f, 10.0f);
					break;
				case IconGlyph.Resize:
					pen.Line(20.0f, 8.0f, 8.0f, 20.0f);
					pen.Line(20.0f, 14.0f, 14.0f, 20.0f);
					break;
			}
		}

		private static void DrawStar(in IconPen pen, bool fill)
		{
			float[] points = new float[StarPoints * 4];

			for (int i = 0; i < StarPoints * 2; i++)
			{
				float radius = i % 2 == 0 ? StarOuter : StarInner;
				float angle = (-90.0f + i * 36.0f) * Mathf.Deg2Rad;
				points[i * 2] = 12.0f + Mathf.Cos(angle) * radius;
				points[i * 2 + 1] = 12.8f + Mathf.Sin(angle) * radius;
			}

			if (fill)
			{
				pen.FilledPolygon(points);
			}
			else
			{
				pen.Polygon(points);
			}
		}

		private static void DrawGear(in IconPen pen)
		{
			pen.Circle(12.0f, 12.0f, 3.0f);
			pen.Circle(12.0f, 12.0f, 6.5f);

			for (int i = 0; i < 8; i++)
			{
				float angle = i * 45.0f * Mathf.Deg2Rad;
				float cos = Mathf.Cos(angle);
				float sin = Mathf.Sin(angle);
				pen.Line(12.0f + cos * 6.5f, 12.0f + sin * 6.5f, 12.0f + cos * 9.5f, 12.0f + sin * 9.5f);
			}
		}

		private static void DrawSun(in IconPen pen)
		{
			pen.Circle(12.0f, 12.0f, 4.0f);

			for (int i = 0; i < 8; i++)
			{
				float angle = i * 45.0f * Mathf.Deg2Rad;
				float cos = Mathf.Cos(angle);
				float sin = Mathf.Sin(angle);
				pen.Line(12.0f + cos * 7.0f, 12.0f + sin * 7.0f, 12.0f + cos * 9.5f, 12.0f + sin * 9.5f);
			}
		}
	}
}