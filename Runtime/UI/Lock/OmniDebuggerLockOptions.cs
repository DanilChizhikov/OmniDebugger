using System;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// A PIN or password the runtime panel asks for before it shows. Only a salted SHA-256 hash of the
	/// secret is kept, in the project settings and in a build alike. A deterrent against testers and
	/// players stumbling into the panel, not protection: a short PIN is quick to guess offline. A PIN's
	/// length is kept too, so the prompt shows one dot per digit and checks it on the last one.
	/// </summary>
	[Serializable]
	public sealed class OmniDebuggerLockOptions
	{
		/// <summary>Fewest digits a PIN may have.</summary>
		public const int MinPinLength = 4;

		/// <summary>Most digits a PIN may have.</summary>
		public const int MaxPinLength = 12;

		/// <summary>
		/// What the panel asks for. <see cref="OmniDebuggerLockMode.None"/> by default. Switching between
		/// <see cref="OmniDebuggerLockMode.Pin"/> and <see cref="OmniDebuggerLockMode.Password"/> clears the
		/// secret, so a password never ends up behind a keypad that cannot type it.
		/// </summary>
		public OmniDebuggerLockMode Mode
		{
			get => _mode;
			set
			{
				if (_mode != value && _mode != OmniDebuggerLockMode.None && value != OmniDebuggerLockMode.None)
				{
					ClearSecret();
				}

				_mode = value;
			}
		}

		/// <summary>How long a correct entry keeps the panel unlocked. <see cref="OmniDebuggerUnlockScope.Session"/> by default.</summary>
		public OmniDebuggerUnlockScope UnlockScope
		{
			get => _unlockScope;
			set => _unlockScope = value;
		}

		/// <summary>Opens the panel without asking while running in the editor. True by default.</summary>
		public bool SkipInEditor
		{
			get => _skipInEditor;
			set => _skipInEditor = value;
		}

		/// <summary>
		/// Wrong entries in a row before the prompt pauses for <see cref="CooldownSeconds"/>. 0 never pauses.
		/// 5 by default.
		/// </summary>
		public int MaxAttempts
		{
			get => Mathf.Max(0, _maxAttempts);
			set => _maxAttempts = value;
		}

		/// <summary>How long the prompt stays paused after <see cref="MaxAttempts"/> wrong entries, in seconds. 30 by default.</summary>
		public float CooldownSeconds
		{
			get => Mathf.Max(0.0f, _cooldownSeconds);
			set => _cooldownSeconds = value;
		}

		/// <summary>Whether a PIN or password was set. Without one the panel opens freely whatever the mode.</summary>
		public bool HasSecret => !string.IsNullOrEmpty(_secretHash) && !string.IsNullOrEmpty(_secretSalt);

		[Tooltip("What the panel asks for before it shows.")]
		[SerializeField] private OmniDebuggerLockMode _mode = OmniDebuggerLockMode.None;

		[Tooltip("How long a correct entry keeps the panel unlocked. Device survives restarts until the secret changes.")]
		[SerializeField] private OmniDebuggerUnlockScope _unlockScope = OmniDebuggerUnlockScope.Session;

		[Tooltip("Opens the panel without asking while running in the editor.")]
		[SerializeField] private bool _skipInEditor = true;

		[Tooltip("Wrong entries in a row before the prompt pauses. 0 never pauses.")]
		[SerializeField, Min(0)] private int _maxAttempts = 5;

		[Tooltip("How long the prompt stays paused after too many wrong entries, in seconds.")]
		[SerializeField, Min(0f)] private float _cooldownSeconds = 30.0f;

		[SerializeField] private string _secretHash;
		[SerializeField] private string _secretSalt;
		[SerializeField] private int _pinLength;

		internal string SecretHash => _secretHash;
		internal int PinLength => _pinLength;

		internal bool IsActive =>
			_mode != OmniDebuggerLockMode.None &&
			HasSecret &&
			!(_skipInEditor && Application.isEditor);

		/// <summary>
		/// Replaces the PIN or password with a fresh salt. Under <see cref="OmniDebuggerLockMode.Pin"/>
		/// it takes <see cref="MinPinLength"/> to <see cref="MaxPinLength"/> digits.
		/// </summary>
		public void SetSecret(string secret)
		{
			if (string.IsNullOrEmpty(secret))
			{
				throw new ArgumentException("A PIN or password must not be empty.", nameof(secret));
			}

			if (_mode == OmniDebuggerLockMode.Pin && !LockSecret.IsValidPin(secret))
			{
				throw new ArgumentException(
					$"A PIN takes {MinPinLength} to {MaxPinLength} digits and nothing else.",
					nameof(secret));
			}

			_secretSalt = LockSecret.CreateSalt();
			_secretHash = LockSecret.Hash(secret, _secretSalt);
			_pinLength = _mode == OmniDebuggerLockMode.Pin ? secret.Length : 0;
		}

		/// <summary>Forgets the PIN or password, so the panel opens freely until a new one is set.</summary>
		public void ClearSecret()
		{
			_secretHash = null;
			_secretSalt = null;
			_pinLength = 0;
		}

		internal bool Matches(string input)
		{
			return HasSecret &&
				!string.IsNullOrEmpty(input) &&
				LockSecret.Matches(LockSecret.Hash(input, _secretSalt), _secretHash);
		}

		internal OmniDebuggerLockOptions Clone()
		{
			return new OmniDebuggerLockOptions
			{
				_mode = _mode,
				_unlockScope = _unlockScope,
				_skipInEditor = _skipInEditor,
				_maxAttempts = _maxAttempts,
				_cooldownSeconds = _cooldownSeconds,
				_secretHash = _secretHash,
				_secretSalt = _secretSalt,
				_pinLength = _pinLength,
			};
		}
	}
}