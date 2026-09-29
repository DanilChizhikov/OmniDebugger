using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class EnumPopupField : BaseField<Enum>
	{
		private readonly Type _enumType;
		private readonly Button _button;
		private readonly Label _text;

		public EnumPopupField(Type enumType) : this(enumType, new Button())
		{
		}

		private EnumPopupField(Type enumType, Button button) : base(null, button)
		{
			_enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
			_button = button;
			_button.AddToClassList(OmniDebuggerUiClasses.EnumButton);
			_button.clicked += OpenList;

			_text = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.EnumButtonText);
			_button.Add(_text);
			_button.Add(new OmniIcon(IconGlyph.ChevronDown));
		}

		public override void SetValueWithoutNotify(Enum newValue)
		{
			base.SetValueWithoutNotify(newValue);
			_text.text = newValue == null ? string.Empty : newValue.ToString();
		}

		private void OpenList()
		{
			Array values = Enum.GetValues(_enumType);
			PopupLayer layer = PopupLayer.Find(this);

			if (layer == null)
			{
				OpenFallbackMenu(values);
				return;
			}

			ScrollView list = UiBuild.Scroll();
			list.AddToClassList(OmniDebuggerUiClasses.PopupList);

			for (int i = 0; i < values.Length; i++)
			{
				Enum option = (Enum)values.GetValue(i);
				Button item = UiBuild.TextButton(option.ToString(), () => Pick(layer, option), OmniDebuggerUiClasses.PopupOption);
				item.EnableInClassList(OmniDebuggerUiClasses.PopupOptionSelected, Equals(option, value));
				list.Add(item);
			}

			layer.ShowAnchored(_button, list, onHidden: null);
		}

		private void Pick(PopupLayer layer, Enum option)
		{
			layer.Hide();
			value = option;
		}

		private void OpenFallbackMenu(Array values)
		{
			GenericDropdownMenu menu = new GenericDropdownMenu();

			for (int i = 0; i < values.Length; i++)
			{
				Enum option = (Enum)values.GetValue(i);
				menu.AddItem(option.ToString(), Equals(option, value), () => value = option);
			}

			menu.DropDown(_button.worldBound, _button, true);
		}
	}
}