using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LogTagsPage
	{
		private const string NoTagsMessage = "No tags yet. Start a message with [Tag] to tag it.";

		private readonly LogsFilterState _filter;
		private readonly Action _changed;
		private readonly VisualElement _root;
		private readonly Button _modeButton;
		private readonly ScrollView _scroll;
		private readonly List<string> _known = new ();

		public VisualElement Root => _root;

		public LogTagsPage(LogsFilterState filter, Action back, Action changed)
		{
			_filter = filter;
			_changed = changed;

			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);

			_scroll = UiBuild.Scroll();
			_root.Add(_scroll);

			VisualElement header = UiBuild.Element(OmniDebuggerUiClasses.Toolbar);
			header.Add(UiBuild.IconButton(IconGlyph.Back, back, "Back to the logs"));
			header.Add(UiBuild.Label("Tags", OmniDebuggerUiClasses.PageHeaderTitle));

			_modeButton = UiBuild.TextButton(string.Empty, ToggleMode, OmniDebuggerUiClasses.Chip);
			_modeButton.AddToClassList(OmniDebuggerUiClasses.ToolbarChip);
			_modeButton.tooltip = "Whether a log needs every selected tag or any one of them";
			header.Add(_modeButton);

			Button clear = UiBuild.TextButton("Clear", ClearTags, OmniDebuggerUiClasses.Chip);
			clear.AddToClassList(OmniDebuggerUiClasses.ToolbarChip);
			header.Add(clear);

			_root.Insert(0, header);
		}

		public void Show(ILogFeed feed)
		{
			_known.Clear();
			feed.GetKnownTags(_known);

			if (_filter.PruneTags(_known))
			{
				_changed?.Invoke();
			}

			Redraw();
		}

		private void Redraw()
		{
			_modeButton.text = _filter.TagMode == LogTagMode.All ? "Match all" : "Match any";
			_scroll.Clear();

			if (_known.Count == 0)
			{
				_scroll.Add(UiBuild.Label(NoTagsMessage, OmniDebuggerUiClasses.Empty));
				return;
			}

			VisualElement chips = UiBuild.Element(OmniDebuggerUiClasses.Chips);
			_scroll.Add(chips);

			for (int i = 0; i < _known.Count; i++)
			{
				string tag = _known[i];
				Button chip = UiBuild.TextButton($"[{tag}]", () => ToggleTag(tag), OmniDebuggerUiClasses.Chip);
				chip.EnableInClassList(OmniDebuggerUiClasses.ChipActive, _filter.HasTag(tag));
				chips.Add(chip);
			}
		}

		private void ToggleTag(string tag)
		{
			_filter.ToggleTag(tag);
			_changed?.Invoke();
			Redraw();
		}

		private void ToggleMode()
		{
			_filter.TagMode = _filter.TagMode == LogTagMode.All ? LogTagMode.Any : LogTagMode.All;
			_changed?.Invoke();
			Redraw();
		}

		private void ClearTags()
		{
			_filter.ClearTags();
			_changed?.Invoke();
			Redraw();
		}
	}
}