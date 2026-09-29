namespace DTech.OmniDebugger.UI
{
	internal sealed class LockAttempts
	{
		public int Failures => _failures;

		private int _failures;
		private float _cooldownEnd;

		public bool TryGetCooldown(float now, out float remaining)
		{
			remaining = _cooldownEnd - now;

			if (remaining > 0.0f)
			{
				return true;
			}

			remaining = 0.0f;
			return false;
		}

		public void RegisterFailure(float now, int maxAttempts, float cooldown)
		{
			_failures++;

			if (maxAttempts <= 0 || _failures < maxAttempts)
			{
				return;
			}

			_failures = 0;
			_cooldownEnd = now + cooldown;
		}

		public void Reset()
		{
			_failures = 0;
			_cooldownEnd = 0.0f;
		}
	}
}