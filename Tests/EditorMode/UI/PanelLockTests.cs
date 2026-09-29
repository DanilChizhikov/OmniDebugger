using System;
using DTech.OmniDebugger.UI;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class PanelLockTests
	{
		[TearDown]
		public void TearDown() => UnlockMemory.Forget();

		[Test]
		public void SetSecret_MatchesOnlyTheSameValue()
		{
			OmniDebuggerLockOptions options = Pin("1234");

			Assert.That(options.HasSecret, Is.True);
			Assert.That(options.Matches("1234"), Is.True);
			Assert.That(options.Matches("4321"), Is.False);
			Assert.That(options.Matches(string.Empty), Is.False);
			Assert.That(options.Matches(null), Is.False);
		}

		[Test]
		public void SetSecret_NeverStoresThePlainValue()
		{
			OmniDebuggerLockOptions options = Password("hunter2");

			Assert.That(options.SecretHash, Does.Not.Contain("hunter2"));
			Assert.That(options.SecretHash, Has.Length.EqualTo(64));
		}

		[Test]
		public void SetSecret_SaltsEveryCall()
		{
			OmniDebuggerLockOptions first = Pin("1234");
			OmniDebuggerLockOptions second = Pin("1234");

			Assert.That(first.SecretHash, Is.Not.EqualTo(second.SecretHash));
			Assert.That(second.Matches("1234"), Is.True);
		}

		[TestCase("123")]
		[TestCase("1234567890123")]
		[TestCase("12a4")]
		[TestCase("")]
		public void SetSecret_RejectsAnInvalidPin(string pin)
		{
			OmniDebuggerLockOptions options = new OmniDebuggerLockOptions { Mode = OmniDebuggerLockMode.Pin };

			Assert.Throws<ArgumentException>(() => options.SetSecret(pin));
			Assert.That(options.HasSecret, Is.False);
		}

		[Test]
		public void SetSecret_TakesAnyCharactersForAPassword()
		{
			OmniDebuggerLockOptions options = Password("Пароль 1!");

			Assert.That(options.Matches("Пароль 1!"), Is.True);
		}

		[Test]
		public void SetSecret_KeepsTheLengthOfAPinOnly()
		{
			Assert.That(Pin("123456").PinLength, Is.EqualTo(6));
			Assert.That(Password("hunter2").PinLength, Is.Zero);
		}

		[Test]
		public void ClearSecret_LeavesNothingToMatch()
		{
			OmniDebuggerLockOptions options = Pin("1234");

			options.ClearSecret();

			Assert.That(options.HasSecret, Is.False);
			Assert.That(options.Matches("1234"), Is.False);
			Assert.That(options.PinLength, Is.Zero);
		}

		[Test]
		public void Mode_SwitchingBetweenPinAndPasswordClearsTheSecret()
		{
			OmniDebuggerLockOptions options = Pin("1234");

			options.Mode = OmniDebuggerLockMode.Password;

			Assert.That(options.HasSecret, Is.False);
			Assert.That(options.PinLength, Is.Zero);
		}

		[Test]
		public void Mode_TurningItOffKeepsTheSecret()
		{
			OmniDebuggerLockOptions options = Pin("1234");

			options.Mode = OmniDebuggerLockMode.None;

			Assert.That(options.HasSecret, Is.True);
		}

		[Test]
		public void IsActive_NeedsAModeASecretAndNoEditorSkip()
		{
			OmniDebuggerLockOptions options = Pin("1234");

			Assert.That(options.IsActive, Is.True);

			options.SkipInEditor = true;
			Assert.That(options.IsActive, Is.False);

			options.SkipInEditor = false;
			options.Mode = OmniDebuggerLockMode.None;
			Assert.That(options.IsActive, Is.False);

			options.Mode = OmniDebuggerLockMode.Pin;
			options.ClearSecret();
			Assert.That(options.IsActive, Is.False);
		}

		[Test]
		public void Clone_CopiesTheLock()
		{
			OmniDebuggerPanelOptions source = new OmniDebuggerPanelOptions();
			source.Lock.Mode = OmniDebuggerLockMode.Pin;
			source.Lock.SetSecret("2468");
			source.Lock.UnlockScope = OmniDebuggerUnlockScope.Device;
			source.Lock.SkipInEditor = false;
			source.Lock.MaxAttempts = 3;
			source.Lock.CooldownSeconds = 10.0f;

			OmniDebuggerLockOptions copy = source.Clone().Lock;

			Assert.That(copy, Is.Not.SameAs(source.Lock));
			Assert.That(copy.Mode, Is.EqualTo(OmniDebuggerLockMode.Pin));
			Assert.That(copy.UnlockScope, Is.EqualTo(OmniDebuggerUnlockScope.Device));
			Assert.That(copy.SkipInEditor, Is.False);
			Assert.That(copy.MaxAttempts, Is.EqualTo(3));
			Assert.That(copy.CooldownSeconds, Is.EqualTo(10.0f));
			Assert.That(copy.Matches("2468"), Is.True);
			Assert.That(copy.PinLength, Is.EqualTo(4));
		}

		[Test]
		public void Attempts_CoolDownAfterTheLimitAndRecover()
		{
			LockAttempts attempts = new LockAttempts();

			attempts.RegisterFailure(0.0f, 3, 30.0f);
			attempts.RegisterFailure(1.0f, 3, 30.0f);
			Assert.That(attempts.TryGetCooldown(1.0f, out _), Is.False);
			Assert.That(attempts.Failures, Is.EqualTo(2));

			attempts.RegisterFailure(2.0f, 3, 30.0f);
			Assert.That(attempts.TryGetCooldown(12.0f, out float remaining), Is.True);
			Assert.That(remaining, Is.EqualTo(20.0f).Within(0.001f));
			Assert.That(attempts.Failures, Is.Zero);

			Assert.That(attempts.TryGetCooldown(32.0f, out _), Is.False);
		}

		[Test]
		public void Attempts_NeverCoolDownWithoutALimit()
		{
			LockAttempts attempts = new LockAttempts();

			for (int i = 0; i < 100; i++)
			{
				attempts.RegisterFailure(i, 0, 30.0f);
			}

			Assert.That(attempts.TryGetCooldown(100.0f, out _), Is.False);
		}

		[Test]
		public void Attempts_ResetClearsTheCooldown()
		{
			LockAttempts attempts = new LockAttempts();
			attempts.RegisterFailure(0.0f, 1, 30.0f);

			attempts.Reset();

			Assert.That(attempts.TryGetCooldown(0.0f, out _), Is.False);
		}

		[Test]
		public void Memory_SessionRemembersUntilTheSecretChanges()
		{
			OmniDebuggerLockOptions options = Pin("1234");
			options.UnlockScope = OmniDebuggerUnlockScope.Session;

			Assert.That(UnlockMemory.IsUnlocked(options), Is.False);

			UnlockMemory.Remember(options);
			Assert.That(UnlockMemory.IsUnlocked(options), Is.True);

			options.SetSecret("5678");
			Assert.That(UnlockMemory.IsUnlocked(options), Is.False);
		}

		[Test]
		public void Memory_EveryOpenNeverRemembers()
		{
			OmniDebuggerLockOptions options = Pin("1234");
			options.UnlockScope = OmniDebuggerUnlockScope.EveryOpen;

			UnlockMemory.Remember(options);

			Assert.That(UnlockMemory.IsUnlocked(options), Is.False);
		}

		private static OmniDebuggerLockOptions Pin(string pin)
		{
			OmniDebuggerLockOptions options = new OmniDebuggerLockOptions
			{
				Mode = OmniDebuggerLockMode.Pin,
				SkipInEditor = false,
			};

			options.SetSecret(pin);
			return options;
		}

		private static OmniDebuggerLockOptions Password(string password)
		{
			OmniDebuggerLockOptions options = new OmniDebuggerLockOptions
			{
				Mode = OmniDebuggerLockMode.Password,
				SkipInEditor = false,
			};

			options.SetSecret(password);
			return options;
		}
	}
}
