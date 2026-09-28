using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PopupLayer : VisualElement
	{
		private const float AnchorGap = 2.0f;
		private const float MinAnchoredWidth = 160.0f;

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

			if (!string.IsNullOrEmpty(title))
			{
				popup.Add(UiBuild.Label(title, OmniDebuggerUiClasses.PopupTitle));
			}

			ScrollView scroll = UiBuild.Scroll();
			scroll.style.flexGrow = 0.0f;
			scroll.contentContainer.style.paddingLeft = 0.0f;
			scroll.contentContainer.style.paddingRight = 0.0f;
			scroll.contentContainer.style.paddingTop = 0.0f;
			scroll.contentContainer.style.paddingBottom = 0.0f;
			scroll.Add(content);
			popup.Add(scroll);

			Show(popup, anchor: null, onHidden: null);
		}

		public void ShowAnchored(VisualElement anchor, VisualElement content, Action onHidden)
		{
			VisualElement popup = UiBuild.Element(OmniDebuggerUiClasses.Popup);
			popup.AddToClassList(OmniDebuggerUiClasses.PopupAnchored);
			popup.style.position = Position.Absolute;
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
			float height = _popup.resolvedStyle.height;
			float width = Mathf.Max(anchor.width, MinAnchoredWidth);

			float spaceBelow = area.height - anchor.yMax - AnchorGap;
			float spaceAbove = anchor.yMin - AnchorGap;
			bool below = height <= spaceBelow || spaceBelow >= spaceAbove;

			float top = below ? anchor.yMax + AnchorGap : anchor.yMin - AnchorGap - Mathf.Min(height, spaceAbove);
			float left = Mathf.Clamp(anchor.xMin, 0.0f, Mathf.Max(0.0f, area.width - width));

			_popup.style.width = width;
			_popup.style.maxHeight = Mathf.Max(0.0f, below ? spaceBelow : spaceAbove);
			_popup.style.left = left;
			_popup.style.top = Mathf.Max(0.0f, top);
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