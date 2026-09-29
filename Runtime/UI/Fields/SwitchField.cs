using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class SwitchField : BaseField<bool>
	{
		public SwitchField() : this(new Button())
		{
		}

		private SwitchField(Button track) : base(null, track)
		{
			AddToClassList(OmniDebuggerUiClasses.Switch);

			track.AddToClassList(OmniDebuggerUiClasses.SwitchTrack);
			track.AddManipulator(new Halo());
			track.clicked += Flip;

			VisualElement knob = UiBuild.Element(OmniDebuggerUiClasses.SwitchKnob);
			knob.pickingMode = PickingMode.Ignore;
			track.Add(knob);
		}

		public override void SetValueWithoutNotify(bool newValue)
		{
			base.SetValueWithoutNotify(newValue);
			EnableInClassList(OmniDebuggerUiClasses.SwitchOn, newValue);
		}

		private void Flip() => value = !value;
	}
}
