using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class Halo : Manipulator
	{
		private const int CornerSegments = 6;
		private const int PointsPerCorner = CornerSegments + 1;
		private const int PointsPerLoop = PointsPerCorner * 4;
		private const int LoopCount = 3;
		private const int IndicesPerBand = PointsPerLoop * 6;
		private const float QuarterTurn = 90.0f;

		private static readonly CustomStyleProperty<Color> _colorProperty = new ("--od-halo-color");
		private static readonly CustomStyleProperty<float> _sizeProperty = new ("--od-halo-size");
		private static readonly float[] _loopReach = { 0.0f, 0.45f, 1.0f };
		private static readonly float[] _loopAlpha = { 1.0f, 0.3f, 0.0f };

		private Color _color;
		private float _size;

		protected override void RegisterCallbacksOnTarget()
		{
			target.AddToClassList(OmniDebuggerUiClasses.Halo);
			target.generateVisualContent += Draw;
			target.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.RemoveFromClassList(OmniDebuggerUiClasses.Halo);
			target.generateVisualContent -= Draw;
			target.UnregisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
		}

		private static void WriteLoop(MeshWriteData mesh, Vector2 size, in Vector4 radii, float reach, Color32 tint)
		{
			WriteCorner(mesh, new Vector2(radii.x, radii.x), radii.x + reach, 180.0f, tint);
			WriteCorner(mesh, new Vector2(size.x - radii.y, radii.y), radii.y + reach, 270.0f, tint);
			WriteCorner(mesh, new Vector2(size.x - radii.z, size.y - radii.z), radii.z + reach, 0.0f, tint);
			WriteCorner(mesh, new Vector2(radii.w, size.y - radii.w), radii.w + reach, 90.0f, tint);
		}

		private static void WriteCorner(MeshWriteData mesh, Vector2 center, float radius, float startDegrees, Color32 tint)
		{
			for (int i = 0; i < PointsPerCorner; i++)
			{
				float angle = (startDegrees + i * (QuarterTurn / CornerSegments)) * Mathf.Deg2Rad;
				Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

				mesh.SetNextVertex(new Vertex
				{
					position = new Vector3(point.x, point.y, Vertex.nearZ),
					tint = tint,
				});
			}
		}

		private static void WriteBand(MeshWriteData mesh, int inner)
		{
			int outer = inner + PointsPerLoop;

			for (int i = 0; i < PointsPerLoop; i++)
			{
				int next = (i + 1) % PointsPerLoop;

				mesh.SetNextIndex((ushort)(inner + i));
				mesh.SetNextIndex((ushort)(outer + i));
				mesh.SetNextIndex((ushort)(outer + next));
				mesh.SetNextIndex((ushort)(inner + i));
				mesh.SetNextIndex((ushort)(outer + next));
				mesh.SetNextIndex((ushort)(inner + next));
			}
		}

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
		{
			Color color = evt.customStyle.TryGetValue(_colorProperty, out Color resolvedColor) ? resolvedColor : Color.clear;
			float size = evt.customStyle.TryGetValue(_sizeProperty, out float resolvedSize) ? resolvedSize : 0.0f;

			if (color == _color && Mathf.Approximately(size, _size))
			{
				return;
			}

			_color = color;
			_size = size;
			target.MarkDirtyRepaint();
		}

		private void Draw(MeshGenerationContext context)
		{
			Vector2 size = target.layout.size;

			if (!(_size > 0.0f) || !(_color.a > 0.0f) || !(size.x > 0.0f) || !(size.y > 0.0f))
			{
				return;
			}

			IResolvedStyle style = target.resolvedStyle;
			float limit = Mathf.Min(size.x, size.y) * 0.5f;

			Vector4 radii = new Vector4(
				Mathf.Clamp(style.borderTopLeftRadius, 0.0f, limit),
				Mathf.Clamp(style.borderTopRightRadius, 0.0f, limit),
				Mathf.Clamp(style.borderBottomRightRadius, 0.0f, limit),
				Mathf.Clamp(style.borderBottomLeftRadius, 0.0f, limit));

			MeshWriteData mesh = context.Allocate(PointsPerLoop * LoopCount, IndicesPerBand * (LoopCount - 1));

			for (int loop = 0; loop < LoopCount; loop++)
			{
				Color tint = _color;
				tint.a *= _loopAlpha[loop];
				WriteLoop(mesh, size, radii, _size * _loopReach[loop], tint);
			}

			for (int band = 0; band < LoopCount - 1; band++)
			{
				WriteBand(mesh, band * PointsPerLoop);
			}
		}
	}
}
