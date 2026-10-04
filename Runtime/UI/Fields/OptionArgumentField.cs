using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class OptionArgumentField : ArgumentFieldBase<object>
	{
		public OptionArgumentField(in ArgumentFieldRequest request)
			: base(new ChoicePopupField(request.Options), request, commitOnChange: true)
		{
			ChoicePopupField popup = (ChoicePopupField)Field;
			Field.SetValueWithoutNotify(Select(popup.ReadOptions(), request.InitialValue));
		}

		public override bool TryGetValue(out object value)
		{
			value = Field.value;
			return value != null;
		}

		protected override object Parse(object value) => value;

		private static object Select(IReadOnlyList<object> options, object initial)
		{
			if (initial != null)
			{
				for (int i = 0; i < options.Count; i++)
				{
					if (Equals(options[i], initial))
					{
						return options[i];
					}
				}
			}

			return options.Count > 0 ? options[0] : null;
		}
	}
}
