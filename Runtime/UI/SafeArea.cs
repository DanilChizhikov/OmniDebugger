using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class SafeArea : IDisposable
	{
		public event Action OnChanged;
		
		private const float Epsilon = 0.5f;

		private readonly VisualElement _observed;
		
		public Vector4 Insets => _insets;

		private Vector4 _insets;
		private bool _disposed;

		public SafeArea(VisualElement observed)
		{
			_observed = observed ?? throw new ArgumentNullException(nameof(observed));
			_observed.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			Measure();
		}

		public static bool TryMeasure(IPanel panel, out Vector4 insets)
		{
			insets = Vector4.zero;

			if (panel == null || panel.contextType != ContextType.Player)
			{
				return false;
			}

			Rect safeArea = Screen.safeArea;
			Rect panelRect = panel.visualTree.layout;

			if (safeArea.width <= 0.0f || safeArea.height <= 0.0f || panelRect.width <= 0.0f)
			{
				return false;
			}

			Vector2 bottomLeft = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safeArea.xMin, Screen.height - safeArea.yMin));
			Vector2 topRight = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safeArea.xMax, Screen.height - safeArea.yMax));

			insets = new Vector4(
				Mathf.Max(0.0f, bottomLeft.x),
				Mathf.Max(0.0f, panelRect.width - topRight.x),
				Mathf.Max(0.0f, topRight.y),
				Mathf.Max(0.0f, panelRect.height - bottomLeft.y));

			return true;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_observed.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			OnChanged = null;
		}

		private static bool Approximately(in Vector4 left, in Vector4 right) =>
			Mathf.Abs(left.x - right.x) < Epsilon &&
			Mathf.Abs(left.y - right.y) < Epsilon &&
			Mathf.Abs(left.z - right.z) < Epsilon &&
			Mathf.Abs(left.w - right.w) < Epsilon;

		private void OnGeometryChanged(GeometryChangedEvent evt) => Measure();

		private void Measure()
		{
			if (_disposed || !TryMeasure(_observed.panel, out Vector4 insets) || Approximately(insets, _insets))
			{
				return;
			}

			_insets = insets;
			OnChanged?.Invoke();
		}
	}
}