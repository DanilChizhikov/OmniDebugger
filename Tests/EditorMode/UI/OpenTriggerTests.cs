using System.Collections.Generic;
using DTech.OmniDebugger.UI;
using NUnit.Framework;
using UnityEngine;

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

		private sealed class FakeInput : IInputBackend
		{
			private readonly HashSet<KeyCode> _held = new ();
			private readonly HashSet<KeyCode> _pressed = new ();

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

			public bool IsKeyHeld(KeyCode key) => _held.Contains(key);

			public bool WasKeyPressed(KeyCode key) => _pressed.Contains(key);
		}
	}
}
