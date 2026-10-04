using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ChoicePopupField : BaseField<object>
	{
		private const string NoneText = "None";
		private const string EmptyText = "No options";

		private readonly Action<ICollection<object>> _collect;
		private readonly List<object> _options = new ();
		private readonly Button _button;
		private readonly Label _text;

		public ChoicePopupField(Action<ICollection<object>> collect) : this(collect, new Button())
		{
		}

		private ChoicePopupField(Action<ICollection<object>> collect, Button button) : base(null, button)
		{
			_collect = collect ?? throw new ArgumentNullException(nameof(collect));
			_button = button;
			_button.AddToClassList(OmniDebuggerUiClasses.EnumButton);
			_button.clicked += OpenList;

			_text = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.EnumButtonText);
			_button.Add(_text);
			_button.Add(new OmniIcon(IconGlyph.ChevronDown));
		}

		public static string Format(object option) => option switch
		{
			null => NoneText,
			UnityEngine.Object unityObject => unityObject != null ? unityObject.name : NoneText,
			_ => option.ToString() ?? string.Empty,
		};

		public IReadOnlyList<object> ReadOptions()
		{
			_options.Clear();
			_collect(_options);
			return _options;
		}

		public override void SetValueWithoutNotify(object newValue)
		{
			base.SetValueWithoutNotify(newValue);
			_text.text = Format(newValue);
		}

		private void OpenList()
		{
			IReadOnlyList<object> options = ReadOptions();
			PopupLayer layer = PopupLayer.Find(this);

			if (layer == null)
			{
				OpenFallbackMenu(options);
				return;
			}

			ScrollView list = UiBuild.Scroll();
			list.AddToClassList(OmniDebuggerUiClasses.PopupList);

			if (options.Count == 0)
			{
				list.Add(UiBuild.Label(EmptyText, OmniDebuggerUiClasses.PopupText));
			}

			for (int i = 0; i < options.Count; i++)
			{
				object option = options[i];
				Button item = UiBuild.TextButton(Format(option), () => Pick(layer, option), OmniDebuggerUiClasses.PopupOption);
				item.EnableInClassList(OmniDebuggerUiClasses.PopupOptionSelected, Equals(option, value));
				list.Add(item);
			}

			layer.ShowAnchored(_button, list, onHidden: null);
		}

		private void Pick(PopupLayer layer, object option)
		{
			layer.Hide();
			value = option;
		}

		private void OpenFallbackMenu(IReadOnlyList<object> options)
		{
			GenericDropdownMenu menu = new GenericDropdownMenu();

			if (options.Count == 0)
			{
				menu.AddDisabledItem(EmptyText, false);
			}

			for (int i = 0; i < options.Count; i++)
			{
				object option = options[i];
				menu.AddItem(Format(option), Equals(option, value), () => value = option);
			}

			menu.DropDown(_button.worldBound, _button, true);
		}
	}
}
