using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class FocusRing : IDisposable
	{
		private const long TrackIntervalMs = 16;
		private const float Outset = 3.0f;

		private readonly VisualElement _root;
		private readonly VisualElement _ring;
		private readonly IVisualElementScheduledItem _track;

		private Rect _bounds;
		private bool _visible;

		public FocusRing(VisualElement root)
		{
			_root = root ?? throw new ArgumentNullException(nameof(root));

			_ring = UiBuild.Element(OmniDebuggerUiClasses.FocusRing);
			_ring.pickingMode = PickingMode.Ignore;
			_ring.style.position = Position.Absolute;
			UiBuild.SetVisible(_ring, false);
			_root.Add(_ring);

			_track = _root.schedule.Execute(Track).Every(TrackIntervalMs);
			_track.Pause();
		}

		public void SetActive(bool active)
		{
			if (active)
			{
				_track.Resume();
				Track();
				return;
			}

			_track.Pause();
			Hide();
		}

		public void Dispose()
		{
			_track.Pause();
			_ring.RemoveFromHierarchy();
		}

		private static float WorldScale(VisualElement element)
		{
			float scale = element.worldBound.width / element.layout.width;
			return float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.0f ? 1.0f : scale;
		}

		private static float Radius(float radius, float scale, float limit) =>
			Mathf.Clamp(radius * scale, 0.0f, limit) + Outset;

		private void Track()
		{
			VisualElement focused = _root.panel?.focusController?.focusedElement as VisualElement;

			if (focused == null || focused == _root || !_root.Contains(focused))
			{
				Hide();
				return;
			}

			Rect local = _root.WorldToLocal(focused.worldBound);

			if (!(local.width > 0.0f) || !(local.height > 0.0f))
			{
				Hide();
				return;
			}

			if (!_visible)
			{
				_visible = true;
				UiBuild.SetVisible(_ring, true);
			}

			if (_root.hierarchy[_root.hierarchy.childCount - 1] != _ring)
			{
				_ring.BringToFront();
			}

			Rect bounds = new Rect(local.x - Outset, local.y - Outset, local.width + 2.0f * Outset, local.height + 2.0f * Outset);

			if (bounds == _bounds)
			{
				return;
			}

			_bounds = bounds;
			_ring.style.left = bounds.x;
			_ring.style.top = bounds.y;
			_ring.style.width = bounds.width;
			_ring.style.height = bounds.height;

			IResolvedStyle style = focused.resolvedStyle;
			float scale = WorldScale(focused) / WorldScale(_root);
			float limit = Mathf.Min(local.width, local.height) * 0.5f;

			_ring.style.borderTopLeftRadius = Radius(style.borderTopLeftRadius, scale, limit);
			_ring.style.borderTopRightRadius = Radius(style.borderTopRightRadius, scale, limit);
			_ring.style.borderBottomRightRadius = Radius(style.borderBottomRightRadius, scale, limit);
			_ring.style.borderBottomLeftRadius = Radius(style.borderBottomLeftRadius, scale, limit);
		}

		private void Hide()
		{
			if (!_visible)
			{
				return;
			}

			_visible = false;
			_bounds = default;
			UiBuild.SetVisible(_ring, false);
		}
	}
}
