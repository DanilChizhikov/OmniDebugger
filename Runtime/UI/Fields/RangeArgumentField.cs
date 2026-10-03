using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class RangeArgumentField<TValue> : IArgumentField
		where TValue : struct, IComparable<TValue>
	{
		public event Action OnCommitted;

		private readonly VisualElement _root;
		private readonly BaseSlider<TValue> _slider;
		private readonly BaseField<TValue> _box;
		private readonly Func<TValue, TValue> _normalize;
		private readonly Func<TValue, int, TValue> _nudge;
		private readonly Func<object, TValue> _parse;
		private readonly Func<TValue, object> _toTarget;

		public VisualElement Root => _root;

		private TValue _committed;
		private bool _pointerDown;
		private bool _disposed;

		public RangeArgumentField(
			BaseSlider<TValue> slider,
			BaseField<TValue> box,
			Func<TValue, TValue> normalize,
			Func<TValue, int, TValue> nudge,
			Func<object, TValue> parse,
			Func<TValue, object> toTarget,
			in ArgumentFieldRequest request)
		{
			_slider = slider ?? throw new ArgumentNullException(nameof(slider));
			_box = box ?? throw new ArgumentNullException(nameof(box));
			_normalize = normalize ?? throw new ArgumentNullException(nameof(normalize));
			_nudge = nudge ?? throw new ArgumentNullException(nameof(nudge));
			_parse = parse ?? throw new ArgumentNullException(nameof(parse));
			_toTarget = toTarget ?? throw new ArgumentNullException(nameof(toTarget));

			_root = UiBuild.Element(OmniDebuggerUiClasses.Field);
			_root.AddToClassList(OmniDebuggerUiClasses.Range);
			_root.tooltip = request.Argument.ToString();

			if (request.ShowLabel)
			{
				_root.Add(UiBuild.Label(request.Argument.Name, OmniDebuggerUiClasses.FieldLabel));
			}

			_slider.AddToClassList(OmniDebuggerUiClasses.RangeSlider);
			_box.AddToClassList(OmniDebuggerUiClasses.RangeBox);
			_root.Add(_slider);
			_root.Add(_box);

			Show(_normalize(_parse(request.InitialValue)));

			_slider.RegisterValueChangedCallback(OnSliderChanged);
			_slider.RegisterCallback<PointerDownEvent>(OnSliderPressed, TrickleDown.TrickleDown);
			_slider.RegisterCallback<PointerCaptureOutEvent>(OnSliderReleased);
			_slider.RegisterCallback<NavigationMoveEvent>(OnSliderNavigationMove, TrickleDown.TrickleDown);
			_box.RegisterCallback<KeyDownEvent>(OnBoxKeyDown);
			_box.RegisterCallback<FocusOutEvent>(OnBoxFocusOut);
		}

		public bool TryGetValue(out object value)
		{
			value = _toTarget(_slider.value);
			return true;
		}

		public void SetValue(object value)
		{
			if (_disposed)
			{
				return;
			}

			Show(_normalize(_parse(value)));
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_slider.UnregisterValueChangedCallback(OnSliderChanged);
			_slider.UnregisterCallback<PointerDownEvent>(OnSliderPressed, TrickleDown.TrickleDown);
			_slider.UnregisterCallback<PointerCaptureOutEvent>(OnSliderReleased);
			_slider.UnregisterCallback<NavigationMoveEvent>(OnSliderNavigationMove, TrickleDown.TrickleDown);
			_box.UnregisterCallback<KeyDownEvent>(OnBoxKeyDown);
			_box.UnregisterCallback<FocusOutEvent>(OnBoxFocusOut);
			OnCommitted = null;
			_root.RemoveFromHierarchy();
		}

		private void Show(TValue value)
		{
			_slider.SetValueWithoutNotify(value);
			_box.SetValueWithoutNotify(value);
			_committed = value;
		}

		private void OnSliderChanged(ChangeEvent<TValue> evt)
		{
			TValue normalized = _normalize(evt.newValue);

			if (!EqualityComparer<TValue>.Default.Equals(normalized, evt.newValue))
			{
				_slider.SetValueWithoutNotify(normalized);
			}

			_box.SetValueWithoutNotify(normalized);

			if (!_pointerDown)
			{
				CommitIfChanged();
			}
		}

		private void OnSliderPressed(PointerDownEvent evt) => _pointerDown = true;

		private void OnSliderReleased(PointerCaptureOutEvent evt)
		{
			if (!_pointerDown)
			{
				return;
			}

			_pointerDown = false;
			CommitIfChanged();
		}

		private void OnSliderNavigationMove(NavigationMoveEvent evt)
		{
			int sign = NudgeSign(evt.direction);

			if (sign == 0 || !_slider.enabledInHierarchy)
			{
				return;
			}

			TValue next = _nudge(_slider.value, sign);

			if (!EqualityComparer<TValue>.Default.Equals(next, _slider.value))
			{
				_slider.value = next;
			}

			evt.StopImmediatePropagation();
			_slider.focusController?.IgnoreEvent(evt);
		}

		private int NudgeSign(NavigationMoveEvent.Direction direction)
		{
			bool horizontal = _slider.direction == SliderDirection.Horizontal;
			int sign;

			switch (direction)
			{
				case NavigationMoveEvent.Direction.Right:
					sign = horizontal ? 1 : 0;
					break;
				case NavigationMoveEvent.Direction.Left:
					sign = horizontal ? -1 : 0;
					break;
				case NavigationMoveEvent.Direction.Up:
					sign = horizontal ? 0 : 1;
					break;
				case NavigationMoveEvent.Direction.Down:
					sign = horizontal ? 0 : -1;
					break;
				default:
					sign = 0;
					break;
			}

			return _slider.inverted ? -sign : sign;
		}

		private void OnBoxKeyDown(KeyDownEvent evt)
		{
			if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
			{
				return;
			}

			TakeBoxValue();
		}

		private void OnBoxFocusOut(FocusOutEvent evt) => TakeBoxValue();

		private void TakeBoxValue()
		{
			TValue normalized = _normalize(_box.value);
			_box.SetValueWithoutNotify(normalized);
			_slider.SetValueWithoutNotify(normalized);
			CommitIfChanged();
		}

		private void CommitIfChanged()
		{
			if (_disposed || EqualityComparer<TValue>.Default.Equals(_slider.value, _committed))
			{
				return;
			}

			_committed = _slider.value;
			OnCommitted?.Invoke();
		}
	}
}
