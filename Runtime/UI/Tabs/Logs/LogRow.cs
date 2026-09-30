using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LogRow : VisualElement
	{
		private readonly VisualElement _card;
		private readonly Label _time;
		private readonly Label _type;
		private readonly Label _tags;
		private readonly Label _message;
		private readonly Label _repeats;
		private readonly Action<LogRecord> _copy;

		private LogRecord _record;

		public LogRow(Action<LogRecord> copy)
		{
			_copy = copy;

			AddToClassList(OmniDebuggerUiClasses.LogRow);

			_card = UiBuild.Element(OmniDebuggerUiClasses.Log);
			Add(_card);

			VisualElement body = UiBuild.Element(OmniDebuggerUiClasses.LogBody);
			VisualElement meta = UiBuild.Element(OmniDebuggerUiClasses.LogMeta);
			_time = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogTime);
			_type = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogTypeLabel);
			_tags = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogTags);
			meta.Add(_time);
			meta.Add(_type);
			meta.Add(_tags);
			_repeats = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogRepeats);
			meta.Add(_repeats);
			_message = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogMessage);
			body.Add(meta);
			body.Add(_message);
			_card.Add(body);

			_card.Add(UiBuild.IconButton(IconGlyph.Copy, Copy, "Copy with stack trace"));
		}

		public void Bind(in LogRecord record)
		{
			_record = record;
			_time.text = LogFormat.Timestamp(record);
			_type.text = LogFormat.TypeLabel(record.Type);
			_message.text = record.Message;

			string tags = LogFormat.TagLine(record);
			_tags.text = tags;
			UiBuild.SetVisible(_tags, tags.Length > 0);

			_repeats.text = record.RepeatCount > 1 ? LogFormat.Repeats(record.RepeatCount) : string.Empty;
			UiBuild.SetVisible(_repeats, record.RepeatCount > 1);

			LogTypeMask kind = LogFilter.MaskOf(record.Type);
			_card.EnableInClassList(OmniDebuggerUiClasses.LogWarning, kind == LogTypeMask.Warning);
			_card.EnableInClassList(OmniDebuggerUiClasses.LogError, kind == LogTypeMask.Error);
		}

		private void Copy() => _copy?.Invoke(_record);
	}
}
