#if ENABLE_INPUT_SYSTEM && OMNI_DEBUGGER_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class InputSystemBackend : IInputBackend
	{
		private const float RepeatDelay = 0.4f;
		private const float RepeatInterval = 0.1f;

		public bool IsTouchSupported => Touchscreen.current != null;

		private int _checkedDeviceId = InputDevice.InvalidDeviceId;
		private bool _dpadStranded;
		private NavigationMoveEvent.Direction _heldDirection;
		private float _nextRepeat;

		public bool IsKeyHeld(KeyCode key)
		{
			KeyControl control = Resolve(key);
			return control != null && control.isPressed;
		}

		public bool WasKeyPressed(KeyCode key)
		{
			KeyControl control = Resolve(key);
			return control != null && control.wasPressedThisFrame;
		}

		public bool IsGamepadButtonHeld(OmniDebuggerGamepadButtons button)
		{
			ButtonControl control = Resolve(Gamepad.current, button);
			return control != null && control.isPressed;
		}

		public bool WasGamepadButtonPressed(OmniDebuggerGamepadButtons button)
		{
			ButtonControl control = Resolve(Gamepad.current, button);
			return control != null && control.wasPressedThisFrame;
		}

		public NavigationMoveEvent.Direction PollStrandedDpad()
		{
			Gamepad gamepad = Gamepad.current;

			if (gamepad == null || !IsDpadStranded(gamepad))
			{
				_heldDirection = NavigationMoveEvent.Direction.None;
				return NavigationMoveEvent.Direction.None;
			}

			NavigationMoveEvent.Direction held = HeldDirection(gamepad.dpad);
			float now = Time.unscaledTime;

			if (held != _heldDirection)
			{
				_heldDirection = held;
				_nextRepeat = now + RepeatDelay;
				return held;
			}

			if (held == NavigationMoveEvent.Direction.None || now < _nextRepeat)
			{
				return NavigationMoveEvent.Direction.None;
			}

			_nextRepeat = now + RepeatInterval;
			return held;
		}

		private static KeyControl Resolve(KeyCode code)
		{
			Keyboard keyboard = Keyboard.current;

			if (keyboard == null || !KeyCodeToKey.TryConvert(code, out Key key))
			{
				return null;
			}

			return keyboard[key];
		}

		private static ButtonControl Resolve(Gamepad gamepad, OmniDebuggerGamepadButtons button)
		{
			if (gamepad == null)
			{
				return null;
			}

			switch (button)
			{
				case OmniDebuggerGamepadButtons.South: return gamepad.buttonSouth;
				case OmniDebuggerGamepadButtons.East: return gamepad.buttonEast;
				case OmniDebuggerGamepadButtons.West: return gamepad.buttonWest;
				case OmniDebuggerGamepadButtons.North: return gamepad.buttonNorth;
				case OmniDebuggerGamepadButtons.LeftShoulder: return gamepad.leftShoulder;
				case OmniDebuggerGamepadButtons.RightShoulder: return gamepad.rightShoulder;
				case OmniDebuggerGamepadButtons.LeftTrigger: return gamepad.leftTrigger;
				case OmniDebuggerGamepadButtons.RightTrigger: return gamepad.rightTrigger;
				case OmniDebuggerGamepadButtons.Select: return gamepad.selectButton;
				case OmniDebuggerGamepadButtons.Start: return gamepad.startButton;
				case OmniDebuggerGamepadButtons.LeftStickPress: return gamepad.leftStickButton;
				case OmniDebuggerGamepadButtons.RightStickPress: return gamepad.rightStickButton;
				default: return null;
			}
		}

		private static NavigationMoveEvent.Direction HeldDirection(DpadControl dpad)
		{
			if (dpad.up.isPressed)
			{
				return NavigationMoveEvent.Direction.Up;
			}

			if (dpad.down.isPressed)
			{
				return NavigationMoveEvent.Direction.Down;
			}

			if (dpad.left.isPressed)
			{
				return NavigationMoveEvent.Direction.Left;
			}

			return dpad.right.isPressed ? NavigationMoveEvent.Direction.Right : NavigationMoveEvent.Direction.None;
		}

		private static bool Covers(InputStateBlock outer, InputStateBlock inner)
		{
			long outerStart = outer.byteOffset * 8L + outer.bitOffset;
			long innerStart = inner.byteOffset * 8L + inner.bitOffset;
			return innerStart >= outerStart && innerStart + inner.sizeInBits <= outerStart + outer.sizeInBits;
		}

		private bool IsDpadStranded(Gamepad gamepad)
		{
			if (gamepad.deviceId == _checkedDeviceId)
			{
				return _dpadStranded;
			}

			DpadControl dpad = gamepad.dpad;
			InputStateBlock block = dpad.stateBlock;

			_checkedDeviceId = gamepad.deviceId;
			_dpadStranded = !Covers(block, dpad.up.stateBlock) ||
				!Covers(block, dpad.down.stateBlock) ||
				!Covers(block, dpad.left.stateBlock) ||
				!Covers(block, dpad.right.stateBlock);

			return _dpadStranded;
		}
	}
}
#endif
