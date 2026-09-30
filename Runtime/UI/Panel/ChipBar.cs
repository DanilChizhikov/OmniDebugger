using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ChipBar : VisualElement
	{
		private readonly ScrollView _scroll;

		public int Count => _scroll.contentContainer.childCount;

		public ChipBar()
		{
			AddToClassList(OmniDebuggerUiClasses.Chips);

			_scroll = new ScrollView(ScrollViewMode.Horizontal)
			{
				horizontalScrollerVisibility = ScrollerVisibility.Hidden,
				verticalScrollerVisibility = ScrollerVisibility.Hidden,
			};

			_scroll.AddToClassList(OmniDebuggerUiClasses.ChipsScroll);
			_scroll.contentContainer.AddToClassList(OmniDebuggerUiClasses.ChipsContent);
			UiBuild.MakeDraggable(_scroll);
			Add(_scroll);
		}

		public Button AddChip(string text, Action clicked, bool selected = false, string modifier = null)
		{
			Button chip = CreateChip(clicked, selected, modifier);
			chip.text = text;
			return chip;
		}

		public Button AddRemovableChip(string text, Action removed, string modifier = null)
		{
			Button chip = CreateChip(removed, selected: true, modifier);
			chip.AddToClassList(OmniDebuggerUiClasses.ChipRemovable);
			chip.Add(UiBuild.Label(text, null));
			chip.Add(new OmniIcon(IconGlyph.Close));
			return chip;
		}

		public void AddSeparator() => _scroll.Add(UiBuild.Element(OmniDebuggerUiClasses.ChipsSeparator));

		public void ClearChips() => _scroll.Clear();

		private Button CreateChip(Action clicked, bool selected, string modifier)
		{
			Button chip = new Button(clicked);
			chip.AddToClassList(OmniDebuggerUiClasses.Chip);
			chip.EnableInClassList(OmniDebuggerUiClasses.ChipActive, selected);

			if (!string.IsNullOrEmpty(modifier))
			{
				chip.AddToClassList(modifier);
			}

			_scroll.Add(chip);
			return chip;
		}
	}
}
