using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LockPrompt : IDisposable
	{
		public event Action OnUnlocked;
		public event Action OnCancelled;

		private const long CooldownTickMs = 250;
		private const int KeypadColumns = 3;

		private readonly VisualElement _host;
		private readonly LockAttempts _attempts;
		private readonly VisualElement _root;
		private readonly VisualElement _card;
		private readonly Label _title;
		private readonly Label _status;
		private readonly VisualElement _dots;
		private readonly VisualElement _keypad;
		private readonly VisualElement _password;
		private readonly TextField _field;
		private readonly List<Button> _inputButtons = new ();
		private readonly StringBuilder _pin = new ();

		public bool IsShowing => _root.parent != null;
		
		private bool IsPin => _options != null && _options.Mode == OmniDebuggerLockMode.Pin;
		private bool KnowsPinLength => IsPin && _options.PinLength > 0;
		private int MaxPinInput => KnowsPinLength ? _options.PinLength : OmniDebuggerLockOptions.MaxPinLength;

		private OmniDebuggerLockOptions _options;
		private IVisualElementScheduledItem _cooldownTicker;
		private bool _disposed;

		public LockPrompt(VisualElement host, LockAttempts attempts)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			_attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));

			_root = UiBuild.Element(OmniDebuggerUiClasses.Lock);
			_root.RegisterCallback<PointerDownEvent>(OnBackgroundPointerDown);

			_card = UiBuild.Element(OmniDebuggerUiClasses.LockCard);
			_card.AddManipulator(new Halo());
			_card.focusable = true;
			_card.RegisterCallback<KeyDownEvent>(OnKeyDown);
			_root.Add(_card);

			VisualElement header = UiBuild.Element(OmniDebuggerUiClasses.LockHeader);
			_title = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LockTitle);
			header.Add(_title);
			header.Add(UiBuild.IconButton(IconGlyph.Close, Cancel, "Close"));
			_card.Add(header);

			_dots = UiBuild.Element(OmniDebuggerUiClasses.LockDots);
			_card.Add(_dots);

			_password = UiBuild.Element(OmniDebuggerUiClasses.LockPassword);
			_field = new TextField { isPasswordField = true };
			_field.AddToClassList(OmniDebuggerUiClasses.LockField);
			_field.textEdition.placeholder = "Password";
			_field.RegisterCallback<KeyDownEvent>(OnFieldKeyDown, TrickleDown.TrickleDown);
			_password.Add(_field);
			_password.Add(UiBuild.TextButton("Unlock", Submit, OmniDebuggerUiClasses.ButtonPrimary));
			_card.Add(_password);

			_status = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.LockStatus);
			_card.Add(_status);

			_keypad = UiBuild.Element(OmniDebuggerUiClasses.LockKeypad);
			BuildKeypad();
			_card.Add(_keypad);
		}

		public void Show(OmniDebuggerLockOptions options)
		{
			MainThreadGuard.Verify(nameof(Show));
			ThrowIfDisposed();

			_options = options ?? throw new ArgumentNullException(nameof(options));

			bool pin = IsPin;
			_title.text = pin ? "Enter PIN" : "Enter password";
			UiBuild.SetVisible(_dots, pin);
			UiBuild.SetVisible(_keypad, pin);
			UiBuild.SetVisible(_password, !pin);

			ClearInput();
			_status.text = string.Empty;

			if (!IsShowing)
			{
				_host.Add(_root);
			}

			_root.BringToFront();
			RefreshCooldown();
			_root.schedule.Execute(FocusInput);
		}

		public void Cancel()
		{
			if (!IsShowing)
			{
				return;
			}

			Hide();
			OnCancelled?.Invoke();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			Hide();

			_root.UnregisterCallback<PointerDownEvent>(OnBackgroundPointerDown);
			_card.UnregisterCallback<KeyDownEvent>(OnKeyDown);
			_field.UnregisterCallback<KeyDownEvent>(OnFieldKeyDown, TrickleDown.TrickleDown);

			OnUnlocked = null;
			OnCancelled = null;
		}

		private void BuildKeypad()
		{
			VisualElement row = null;

			for (int i = 1; i <= 9; i++)
			{
				if ((i - 1) % KeypadColumns == 0)
				{
					row = UiBuild.Element(OmniDebuggerUiClasses.LockKeypadRow);
					_keypad.Add(row);
				}

				AddKey(row, CreateDigitKey((char)('0' + i)));
			}

			row = UiBuild.Element(OmniDebuggerUiClasses.LockKeypadRow);
			AddKey(row, UiBuild.IconButton(IconGlyph.Back, RemoveDigit, "Delete"));
			AddKey(row, CreateDigitKey('0'));

			Button submit = UiBuild.IconButton(IconGlyph.Check, Submit, "Unlock");
			submit.AddToClassList(OmniDebuggerUiClasses.ButtonPrimary);
			AddKey(row, submit);

			_keypad.Add(row);
		}

		private Button CreateDigitKey(char digit) =>
			UiBuild.TextButton(digit.ToString(), () => AppendDigit(digit));

		private void AddKey(VisualElement row, Button button)
		{
			button.AddToClassList(OmniDebuggerUiClasses.LockKey);
			button.EnableInClassList(OmniDebuggerUiClasses.LockKeyGap, row.childCount > 0);
			button.focusable = false;
			_inputButtons.Add(button);
			row.Add(button);
		}

		private void AppendDigit(char digit)
		{
			if (!IsPin || _pin.Length >= MaxPinInput || IsCoolingDown())
			{
				return;
			}

			_pin.Append(digit);
			_status.text = string.Empty;
			RefreshDots();

			if (KnowsPinLength && _pin.Length == _options.PinLength)
			{
				Submit();
			}
		}

		private void RemoveDigit()
		{
			if (_pin.Length == 0)
			{
				return;
			}

			_pin.Length--;
			RefreshDots();
		}

		private void Submit()
		{
			if (_options == null || IsCoolingDown())
			{
				return;
			}

			string input = IsPin ? _pin.ToString() : _field.value;

			if (string.IsNullOrEmpty(input) || (KnowsPinLength && input.Length != _options.PinLength))
			{
				return;
			}

			if (_options.Matches(input))
			{
				Hide();
				OnUnlocked?.Invoke();
				return;
			}

			_attempts.RegisterFailure(Time.realtimeSinceStartup, _options.MaxAttempts, _options.CooldownSeconds);
			ClearInput();

			if (RefreshCooldown())
			{
				return;
			}

			string wrong = IsPin ? "Wrong PIN." : "Wrong password.";
			int left = _options.MaxAttempts - _attempts.Failures;
			_status.text = _options.MaxAttempts > 0 ? $"{wrong} Attempts left: {left}." : wrong;
			FocusInput();
		}

		private void Hide()
		{
			StopCooldownTicker();
			ClearInput();
			_root.RemoveFromHierarchy();
		}

		private bool IsCoolingDown() => _attempts.TryGetCooldown(Time.realtimeSinceStartup, out _);

		private bool RefreshCooldown()
		{
			if (_attempts.TryGetCooldown(Time.realtimeSinceStartup, out float remaining))
			{
				_status.text = $"Too many attempts. Try again in {Mathf.CeilToInt(remaining)} s.";
				SetInputEnabled(false);
				_cooldownTicker ??= _root.schedule.Execute(OnCooldownTick).Every(CooldownTickMs);
				return true;
			}

			if (_cooldownTicker != null)
			{
				StopCooldownTicker();
				_status.text = string.Empty;
				FocusInput();
			}

			SetInputEnabled(true);
			return false;
		}

		private void OnCooldownTick() => RefreshCooldown();

		private void StopCooldownTicker()
		{
			_cooldownTicker?.Pause();
			_cooldownTicker = null;
		}

		private void SetInputEnabled(bool enabled)
		{
			for (int i = 0; i < _inputButtons.Count; i++)
			{
				_inputButtons[i].SetEnabled(enabled);
			}

			_password.SetEnabled(enabled);
		}

		private void ClearInput()
		{
			_pin.Clear();
			_field.SetValueWithoutNotify(string.Empty);
			RefreshDots();
		}

		private void RefreshDots()
		{
			_dots.Clear();

			int slots = KnowsPinLength
				? _options.PinLength
				: Mathf.Max(OmniDebuggerLockOptions.MinPinLength, _pin.Length);

			for (int i = 0; i < slots; i++)
			{
				VisualElement dot = UiBuild.Element(OmniDebuggerUiClasses.LockDot);
				dot.EnableInClassList(OmniDebuggerUiClasses.LockDotFilled, i < _pin.Length);
				_dots.Add(dot);
			}
		}

		private void FocusInput()
		{
			if (!IsShowing)
			{
				return;
			}

			if (IsPin)
			{
				_card.Focus();
			}
			else
			{
				_field.Focus();
			}
		}

		private void OnKeyDown(KeyDownEvent evt)
		{
			KeyCode key = evt.keyCode;

			if (key == KeyCode.Escape)
			{
				evt.StopPropagation();
				Cancel();
				return;
			}

			if (!IsPin)
			{
				return;
			}

			if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
			{
				AppendDigit((char)('0' + (key - KeyCode.Alpha0)));
			}
			else if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
			{
				AppendDigit((char)('0' + (key - KeyCode.Keypad0)));
			}
			else if (key == KeyCode.Backspace || key == KeyCode.Delete)
			{
				RemoveDigit();
			}
			else if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
			{
				Submit();
			}
			else
			{
				return;
			}

			evt.StopPropagation();
		}

		private void OnFieldKeyDown(KeyDownEvent evt)
		{
			if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
			{
				return;
			}

			evt.StopPropagation();
			Submit();
		}

		private void OnBackgroundPointerDown(PointerDownEvent evt)
		{
			if (evt.target != _root)
			{
				return;
			}

			evt.StopPropagation();
			Cancel();
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(LockPrompt));
			}
		}
	}
}