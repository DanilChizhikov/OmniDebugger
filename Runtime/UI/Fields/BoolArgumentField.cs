using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class BoolArgumentField : ArgumentFieldBase<bool>
	{
		public BoolArgumentField(in ArgumentFieldRequest request)
			: base(new SwitchField(), request, commitOnChange: true)
		{
			Field.SetValueWithoutNotify(Parse(request.InitialValue));
		}

		public override bool TryGetValue(out object value)
		{
			value = Field.value;
			return true;
		}

		protected override bool Parse(object value) => value is bool typed && typed;
	}
}
