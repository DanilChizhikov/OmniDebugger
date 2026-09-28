using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LogRow : VisualElement
	{
		private readonly VisualElement _card;
		private readonly Label _meta;
		private readonly Label _message;
		private readonly Action<LogRecord> _copy;

		private LogRecord _record;

		public LogRow(Action<LogRecord> copy)
		{
			_copy = copy;

			AddToClassList(OmniDebuggerUiClasses.LogRow);

			_card = UiBuild.Element(OmniDebuggerUiClasses.Log);
			Add(_card);

			VisualElement body = UiBuild.Element(OmniDebuggerUiClasses.LogBody);
			_meta = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogMeta);
			_message = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LogMessage);
			body.Add(_meta);
			body.Add(_message);
			_card.Add(body);

			_card.Add(UiBuild.IconButton(IconGlyph.Copy, Copy, "Copy with stack trace"));
		}

		public void Bind(in LogRecord record)
		{
			_record = record;
			_meta.text = LogFormat.Meta(record);
			_message.text = record.Message;

			LogTypeMask kind = LogFilter.MaskOf(record.Type);
			_card.EnableInClassList(OmniDebuggerUiClasses.LogWarning, kind == LogTypeMask.Warning);
			_card.EnableInClassList(OmniDebuggerUiClasses.LogError, kind == LogTypeMask.Error);
		}

		private void Copy() => _copy?.Invoke(_record);
	}
}