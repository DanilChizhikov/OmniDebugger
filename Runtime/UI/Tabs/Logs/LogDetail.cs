using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LogDetail : VisualElement
	{
		private const string NoStackTrace = "No stack trace.";

		private readonly Label _time;
		private readonly Label _type;
		private readonly Label _tags;
		private readonly Label _repeats;
		private readonly Label _message;
		private readonly Label _stack;
		private readonly ScrollView _scroll;
		private readonly Action<LogRecord> _copy;

		public LogRecord Record => _record;

		private LogRecord _record;

		public LogDetail(Action<LogRecord> copy, Action close)
		{
			_copy = copy;

			AddToClassList(OmniDebuggerUiClasses.LogDetail);

			VisualElement header = UiBuild.Element(OmniDebuggerUiClasses.LogDetailHeader);
			VisualElement meta = UiBuild.Element(OmniDebuggerUiClasses.LogMeta);
			_time = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogTime);
			_type = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogTypeLabel);
			_tags = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogTags);
			_repeats = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogRepeats);
			meta.Add(_time);
			meta.Add(_type);
			meta.Add(_tags);
			meta.Add(_repeats);
			header.Add(meta);
			header.Add(UiBuild.IconButton(IconGlyph.Copy, Copy, "Copy with stack trace"));
			header.Add(UiBuild.IconButton(IconGlyph.Close, close, "Close"));
			Add(header);

			_scroll = UiBuild.Scroll();
			_scroll.AddToClassList(OmniDebuggerUiClasses.LogDetailScroll);
			_message = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogDetailMessage);
			_stack = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogDetailStack);
			_stack.enableRichText = false;
			_scroll.Add(_message);
			_scroll.Add(_stack);
			Add(_scroll);
		}

		public void Show(in LogRecord record)
		{
			bool other = record.Id != _record.Id;
			_record = record;

			_time.text = LogFormat.Timestamp(record);
			_type.text = LogFormat.TypeLabel(record.Type);
			_message.text = record.Message;
			_stack.text = record.StackTrace.Length > 0 ? record.StackTrace.TrimEnd() : NoStackTrace;

			string tags = LogFormat.TagLine(record);
			_tags.text = tags;
			UiBuild.SetVisible(_tags, tags.Length > 0);

			_repeats.text = record.RepeatCount > 1 ? LogFormat.Repeats(record.RepeatCount) : string.Empty;
			UiBuild.SetVisible(_repeats, record.RepeatCount > 1);

			LogTypeMask kind = LogFilter.MaskOf(record.Type);
			EnableInClassList(OmniDebuggerUiClasses.LogDetailWarning, kind == LogTypeMask.Warning);
			EnableInClassList(OmniDebuggerUiClasses.LogDetailError, kind == LogTypeMask.Error);

			if (other)
			{
				_scroll.scrollOffset = Vector2.zero;
			}
		}

		private void Copy() => _copy?.Invoke(_record);
	}
}
