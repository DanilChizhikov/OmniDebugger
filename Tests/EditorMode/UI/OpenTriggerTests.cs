using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class OpenTriggerTests
	{
		[Test]
		public void ShortcutTrigger_FiresOnTheFrameTheLastKeyGoesDown()
		{
			FakeInput input = new FakeInput();
			ShortcutTrigger trigger = new ShortcutTrigger(input);
			List<OmniDebuggerShortcut> shortcuts = new List<OmniDebuggerShortcut>
			{
				new OmniDebuggerShortcut(KeyCode.LeftControl, KeyCode.BackQuote),
			};

			input.Hold(KeyCode.LeftControl, pressedNow: true);
			Assert.That(trigger.Poll(shortcuts), Is.False, "half a chord is not a chord");

			input.Hold(KeyCode.LeftControl, pressedNow: false);
			input.Hold(KeyCode.BackQuote, pressedNow: true);
			Assert.That(trigger.Poll(shortcuts), Is.True);

			input.Hold(KeyCode.BackQuote, pressedNow: false);
			Assert.That(trigger.Poll(shortcuts), Is.False, "holding the chord must not repeat it");
		}

		[Test]
		public void ShortcutTrigger_AnyOfSeveralShortcutsWorks_AndEmptyOnesNever()
		{
			FakeInput input = new FakeInput();
			ShortcutTrigger trigger = new ShortcutTrigger(input);
			List<OmniDebuggerShortcut> shortcuts = new List<OmniDebuggerShortcut>
			{
				new OmniDebuggerShortcut(),
				new OmniDebuggerShortcut(KeyCode.None),
				new OmniDebuggerShortcut(KeyCode.F1),
			};

			Assert.That(trigger.Poll(shortcuts), Is.False);

			input.Hold(KeyCode.F1, pressedNow: true);
			Assert.That(trigger.Poll(shortcuts), Is.True);
		}

		[Test]
		public void ShortcutTrigger_ModifierMatchesEitherSide()
		{
			FakeInput input = new FakeInput();
			ShortcutTrigger trigger = new ShortcutTrigger(input);
			List<OmniDebuggerShortcut> shortcuts = new List<OmniDebuggerShortcut>
			{
				new OmniDebuggerShortcut(KeyCode.LeftControl, KeyCode.D),
			};

			input.Hold(KeyCode.RightControl, pressedNow: false);
			input.Hold(KeyCode.D, pressedNow: true);
			Assert.That(trigger.Poll(shortcuts), Is.True, "right Ctrl stands in for left Ctrl");
		}

		[Test]
		public void ShortcutTrigger_OtherKeysHaveNoStandIn()
		{
			FakeInput input = new FakeInput();
			ShortcutTrigger trigger = new ShortcutTrigger(input);
			List<OmniDebuggerShortcut> shortcuts = new List<OmniDebuggerShortcut>
			{
				new OmniDebuggerShortcut(KeyCode.Return),
			};

			input.Hold(KeyCode.KeypadEnter, pressedNow: true);
			Assert.That(trigger.Poll(shortcuts), Is.False);
		}

		[Test]
		public void GamepadCombo_FiresOnTheFrameTheLastButtonGoesDown()
		{
			FakeInput input = new FakeInput();
			ShortcutTrigger trigger = new ShortcutTrigger(input);
			const OmniDebuggerGamepadButtons combo = OmniDebuggerGamepadButtons.Select | OmniDebuggerGamepadButtons.Start;

			input.Hold(OmniDebuggerGamepadButtons.Select, pressedNow: true);
			Assert.That(trigger.PollGamepad(combo), Is.False, "half a combo is not a combo");

			input.Hold(OmniDebuggerGamepadButtons.Select, pressedNow: false);
			input.Hold(OmniDebuggerGamepadButtons.Start, pressedNow: true);
			Assert.That(trigger.PollGamepad(combo), Is.True);

			input.Hold(OmniDebuggerGamepadButtons.Start, pressedNow: false);
			Assert.That(trigger.PollGamepad(combo), Is.False, "holding the combo must not repeat it");
		}

		[Test]
		public void GamepadCombo_NoneNeverFires()
		{
			FakeInput input = new FakeInput();
			ShortcutTrigger trigger = new ShortcutTrigger(input);

			input.Hold(OmniDebuggerGamepadButtons.South, pressedNow: true);
			Assert.That(trigger.PollGamepad(OmniDebuggerGamepadButtons.None), Is.False);
		}

		[Test]
		public void OpenOptions_GamepadComboDefaultsToSelectAndStart_AndSurvivesClone()
		{
			OmniDebuggerOpenOptions options = new OmniDebuggerOpenOptions();
			Assert.That(options.GamepadCombo, Is.EqualTo(OmniDebuggerGamepadButtons.Select | OmniDebuggerGamepadButtons.Start));

			options.GamepadCombo = OmniDebuggerGamepadButtons.LeftStickPress | OmniDebuggerGamepadButtons.RightStickPress;
			Assert.That(options.Clone().GamepadCombo, Is.EqualTo(options.GamepadCombo));
		}

		[Test]
		public void ClickSeries_NeedsTheWholeSeriesWithinTheWindow()
		{
			ClickSeries series = new ClickSeries();

			Assert.That(series.Register(0.0f, 3, 0.4f), Is.False);
			Assert.That(series.Register(0.3f, 3, 0.4f), Is.False);
			Assert.That(series.Register(1.0f, 3, 0.4f), Is.False, "a long pause starts the series over");
			Assert.That(series.Register(1.2f, 3, 0.4f), Is.False);
			Assert.That(series.Register(1.5f, 3, 0.4f), Is.True);
			Assert.That(series.Count, Is.Zero, "a completed series starts over");
		}

		[Test]
		public void OpenButton_SnapsToTheNearestEdgeAndKeepsItsPlaceAlongIt()
		{
			Rect bounds = Rect.MinMaxRect(0.0f, 0.0f, 100.0f, 200.0f);

			OpenButtonGesture.Snap(new Vector2(10.0f, 50.0f), bounds, out ScreenEdge edge, out float along);
			Assert.That(edge, Is.EqualTo(ScreenEdge.Left));
			Assert.That(along, Is.EqualTo(0.25f).Within(0.001f));

			OpenButtonGesture.Snap(new Vector2(60.0f, 195.0f), bounds, out edge, out along);
			Assert.That(edge, Is.EqualTo(ScreenEdge.Bottom));
			Assert.That(along, Is.EqualTo(0.6f).Within(0.001f));

			Vector2 back = OpenButtonGesture.ToPosition(ScreenEdge.Bottom, along, bounds);
			Assert.That(back, Is.EqualTo(new Vector2(60.0f, 200.0f)).Using(Vector2EqualityComparer.Instance), "on the edge, where it was along it");
		}

		[Test]
		public void OpenButton_StartsWhereItsAnchorSays()
		{
			OpenButtonGesture.ResolveAnchor(OpenButtonAnchor.BottomRight, out ScreenEdge edge, out float along);
			Assert.That((edge, along), Is.EqualTo((ScreenEdge.Right, 1.0f)));

			OpenButtonGesture.ResolveAnchor(OpenButtonAnchor.Top, out edge, out along);
			Assert.That((edge, along), Is.EqualTo((ScreenEdge.Top, 0.5f)));
		}

		[Test]
		public void ClickSeries_OneClickOpensRightAway()
		{
			Assert.That(new ClickSeries().Register(5.0f, 1, 0.4f), Is.True);
		}

		[Test]
		public void OpenOptions_ClampTheClickCount()
		{
			OmniDebuggerOpenOptions options = new OmniDebuggerOpenOptions { ButtonClicks = 0 };
			Assert.That(options.ButtonClicks, Is.EqualTo(1));

			options.ButtonClicks = 99;
			Assert.That(options.ButtonClicks, Is.EqualTo(OmniDebuggerOpenOptions.MaxButtonClicks));
		}

		[Test]
		public void OpenOptions_ClampTheButtonOpacity()
		{
			OmniDebuggerOpenOptions options = new OmniDebuggerOpenOptions { ButtonOpacity = 0.0f };
			Assert.That(options.ButtonOpacity, Is.EqualTo(OmniDebuggerOpenOptions.MinButtonOpacity));

			options.ButtonOpacity = 2.0f;
			Assert.That(options.ButtonOpacity, Is.EqualTo(1.0f));
		}

		private sealed class FakeInput : IInputBackend
		{
			private readonly HashSet<KeyCode> _held = new ();
			private readonly HashSet<KeyCode> _pressed = new ();
			private readonly HashSet<OmniDebuggerGamepadButtons> _heldButtons = new ();
			private readonly HashSet<OmniDebuggerGamepadButtons> _pressedButtons = new ();

			public bool IsTouchSupported => false;

			public void Hold(KeyCode key, bool pressedNow)
			{
				_held.Add(key);

				if (pressedNow)
				{
					_pressed.Add(key);
				}
				else
				{
					_pressed.Remove(key);
				}
			}

			public void Hold(OmniDebuggerGamepadButtons button, bool pressedNow)
			{
				_heldButtons.Add(button);

				if (pressedNow)
				{
					_pressedButtons.Add(button);
				}
				else
				{
					_pressedButtons.Remove(button);
				}
			}

			public bool IsKeyHeld(KeyCode key) => _held.Contains(key);

			public bool WasKeyPressed(KeyCode key) => _pressed.Contains(key);

			public bool IsGamepadButtonHeld(OmniDebuggerGamepadButtons button) => _heldButtons.Contains(button);

			public bool WasGamepadButtonPressed(OmniDebuggerGamepadButtons button) => _pressedButtons.Contains(button);

			public NavigationMoveEvent.Direction PollStrandedDpad() => NavigationMoveEvent.Direction.None;
		}
	}
}
