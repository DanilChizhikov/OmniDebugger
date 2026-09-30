using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger
{
	internal sealed class InfoItem
	{
		public readonly InfoItemKind Kind;
		public readonly string Label;
		public readonly string Value;

		public Func<string> Read;
		public InfoSamples Samples;
		public string Unit;
		public Func<VisualElement> Build;
		public string Path;

		public InfoItem(InfoItemKind kind, string label, string value)
		{
			Kind = kind;
			Label = string.IsNullOrWhiteSpace(label) ? InfoSectionModel.Unknown : label;
			Value = value;
		}

		public string ReadNow()
		{
			try
			{
				string value = Read();
				return string.IsNullOrEmpty(value) ? InfoSectionModel.Unknown : value;
			}
			catch (Exception exception)
			{
				return exception.GetType().Name;
			}
		}
	}
}
