using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class OmniIcon : VisualElement
	{
		private const float Grid = 24.0f;
		private const float StrokeWidth = 1.75f;

		public IconGlyph Glyph
		{
			get => _glyph;
			set
			{
				if (_glyph == value)
				{
					return;
				}

				_glyph = value;
				MarkDirtyRepaint();
			}
		}

		private IconGlyph _glyph;

		public OmniIcon(IconGlyph glyph = IconGlyph.None)
		{
			_glyph = glyph;
			pickingMode = PickingMode.Ignore;
			AddToClassList(OmniDebuggerUiClasses.Icon);
			generateVisualContent += Draw;
		}

		private void Draw(MeshGenerationContext context)
		{
			Rect rect = contentRect;

			if (_glyph == IconGlyph.None || rect.width <= 0.0f || rect.height <= 0.0f)
			{
				return;
			}

			float scale = Mathf.Min(rect.width, rect.height) / Grid;
			Vector2 origin = new Vector2(
				rect.x + (rect.width - Grid * scale) * 0.5f,
				rect.y + (rect.height - Grid * scale) * 0.5f);

			Painter2D painter = context.painter2D;
			Color color = resolvedStyle.color;

			painter.strokeColor = color;
			painter.fillColor = color;
			painter.lineWidth = StrokeWidth * scale;
			painter.lineCap = LineCap.Round;
			painter.lineJoin = LineJoin.Round;

			IconPen pen = new IconPen(painter, origin, scale);
			IconShapes.Draw(_glyph, pen);
		}
	}
}