using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal static class UiBuild
	{
		public static Label Label(string text, string className)
		{
			Label label = new Label(text);

			if (!string.IsNullOrEmpty(className))
			{
				label.AddToClassList(className);
			}

			return label;
		}

		public static VisualElement Element(string className)
		{
			VisualElement element = new VisualElement();
			element.AddToClassList(className);
			return element;
		}

		public static Button IconButton(IconGlyph glyph, Action clicked, string tooltip = null)
		{
			Button button = new Button(clicked) { tooltip = tooltip };
			button.AddToClassList(OmniDebuggerUiClasses.IconButton);
			button.Add(new OmniIcon(glyph));
			return button;
		}

		public static void SetGlyph(Button iconButton, IconGlyph glyph)
		{
			OmniIcon icon = iconButton.Q<OmniIcon>();

			if (icon != null)
			{
				icon.Glyph = glyph;
			}
		}

		public static Button TextButton(string text, Action clicked, string className = null)
		{
			Button button = new Button(clicked) { text = text };

			if (!string.IsNullOrEmpty(className))
			{
				button.AddToClassList(className);
			}

			return button;
		}

		public static ScrollView Scroll()
		{
			ScrollView scroll = new ScrollView(ScrollViewMode.Vertical)
			{
				horizontalScrollerVisibility = ScrollerVisibility.Hidden,
			};

			scroll.AddToClassList(OmniDebuggerUiClasses.Scroll);
			scroll.contentContainer.AddToClassList(OmniDebuggerUiClasses.ScrollContent);
			MakeDraggable(scroll);
			return scroll;
		}

		public static DragScroll MakeDraggable(ScrollView scroll)
		{
			scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;

			DragScroll drag = new DragScroll();
			scroll.AddManipulator(drag);
			return drag;
		}

		public static void SetVisible(VisualElement element, bool visible) =>
			element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

		public static TextField SearchBox(VisualElement parent, string placeholder)
		{
			VisualElement box = Element(OmniDebuggerUiClasses.SearchBox);
			box.Add(new OmniIcon(IconGlyph.Search));

			TextField field = new TextField();
			field.AddToClassList(OmniDebuggerUiClasses.SearchBoxField);
			field.textEdition.placeholder = placeholder;
			box.Add(field);

			parent.Add(box);
			return field;
		}
	}
}