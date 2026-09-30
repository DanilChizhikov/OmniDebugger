using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PopupLayer : VisualElement
	{
		private const float AnchorGap = 4.0f;
		private const float MinAnchoredWidth = 192.0f;
		private const float EdgeMargin = 6.0f;

		public bool IsShowing => _popup != null;

		private VisualElement _popup;
		private VisualElement _anchor;
		private Action _onHidden;

		public PopupLayer()
		{
			AddToClassList(OmniDebuggerUiClasses.PopupLayer);
			style.alignItems = Align.Center;
			style.justifyContent = Justify.Center;
			RegisterCallback<PointerDownEvent>(OnPointerDown);
		}

		public static PopupLayer Find(VisualElement element)
		{
			VisualElement current = element;

			while (current != null && !current.ClassListContains(OmniDebuggerUiClasses.Root))
			{
				current = current.parent;
			}

			return current?.Q<PopupLayer>();
		}

		public void ShowCentered(string title, VisualElement content)
		{
			VisualElement popup = UiBuild.Element(OmniDebuggerUiClasses.Popup);
			popup.style.position = Position.Relative;
			popup.AddManipulator(new Halo());

			if (!string.IsNullOrEmpty(title))
			{
				popup.Add(UiBuild.Label(title, OmniDebuggerUiClasses.PopupTitle));
			}

			ScrollView scroll = UiBuild.Scroll();
			scroll.AddToClassList(OmniDebuggerUiClasses.PopupList);
			scroll.Add(content);
			popup.Add(scroll);

			Show(popup, anchor: null, onHidden: null);
		}

		public void ShowCentered(VisualElement content, string modifier, Action onHidden)
		{
			VisualElement popup = UiBuild.Element(OmniDebuggerUiClasses.Popup);
			popup.style.position = Position.Relative;
			popup.AddManipulator(new Halo());

			if (!string.IsNullOrEmpty(modifier))
			{
				popup.AddToClassList(modifier);
			}

			popup.Add(content);
			Show(popup, anchor: null, onHidden);
		}

		public void ShowAnchored(VisualElement anchor, VisualElement content, Action onHidden)
		{
			VisualElement popup = UiBuild.Element(OmniDebuggerUiClasses.Popup);
			popup.AddToClassList(OmniDebuggerUiClasses.PopupAnchored);
			popup.AddManipulator(new Halo());
			popup.style.position = Position.Absolute;
			popup.style.transformOrigin = new TransformOrigin(0, 0);
			popup.style.visibility = Visibility.Hidden;
			popup.Add(content);
			popup.RegisterCallback<GeometryChangedEvent>(OnAnchoredGeometryChanged);

			Show(popup, anchor, onHidden);
		}

		public void Hide()
		{
			if (_popup == null)
			{
				return;
			}

			_popup.UnregisterCallback<GeometryChangedEvent>(OnAnchoredGeometryChanged);
			_popup.RemoveFromHierarchy();
			_popup = null;
			_anchor = null;

			RemoveFromClassList(OmniDebuggerUiClasses.PopupLayerVisible);
			RemoveFromClassList(OmniDebuggerUiClasses.PopupLayerAnchored);

			Action onHidden = _onHidden;
			_onHidden = null;
			onHidden?.Invoke();
		}

		private static float WorldScale(VisualElement element)
		{
			float scale = element.worldBound.width / element.layout.width;
			return float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.0f ? 1.0f : scale;
		}

		private void Show(VisualElement popup, VisualElement anchor, Action onHidden)
		{
			Hide();

			_popup = popup;
			_anchor = anchor;
			_onHidden = onHidden;

			Add(popup);
			AddToClassList(OmniDebuggerUiClasses.PopupLayerVisible);
			EnableInClassList(OmniDebuggerUiClasses.PopupLayerAnchored, anchor != null);
			BringToFront();
		}

		private void OnAnchoredGeometryChanged(GeometryChangedEvent evt)
		{
			if (_popup == null || _anchor == null || _anchor.panel == null)
			{
				return;
			}

			Rect anchor = this.WorldToLocal(_anchor.worldBound);
			Rect area = layout;
			float scale = WorldScale(_anchor) / WorldScale(this);
			float width = _popup.layout.width * scale;
			float height = _popup.layout.height * scale;

			float spaceBelow = area.height - anchor.yMax - AnchorGap - EdgeMargin;
			float spaceAbove = anchor.yMin - AnchorGap - EdgeMargin;
			bool below = height <= spaceBelow || spaceBelow >= spaceAbove;

			float top = below ? anchor.yMax + AnchorGap : anchor.yMin - AnchorGap - Mathf.Min(height, spaceAbove);
			float right = area.width - EdgeMargin;
			float left = anchor.xMin + width <= right ? anchor.xMin : anchor.xMax - width;

			_popup.style.scale = new Scale(new Vector3(scale, scale, 1.0f));
			_popup.style.minWidth = Mathf.Max(anchor.width / scale, MinAnchoredWidth);
			_popup.style.maxWidth = Mathf.Max(0.0f, (area.width - 2.0f * EdgeMargin) / scale);
			_popup.style.maxHeight = Mathf.Max(0.0f, (below ? spaceBelow : spaceAbove) / scale);
			_popup.style.left = Mathf.Clamp(left, EdgeMargin, Mathf.Max(EdgeMargin, right - width));
			_popup.style.top = Mathf.Max(EdgeMargin, top);
			_popup.style.visibility = Visibility.Visible;
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			if (evt.target != this)
			{
				return;
			}

			evt.StopPropagation();
			Hide();
		}
	}
}