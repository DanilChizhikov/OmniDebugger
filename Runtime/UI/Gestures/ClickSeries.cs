namespace DTech.OmniDebugger.UI
{
	internal sealed class ClickSeries
	{
		public int Count => _count;

		private int _count;
		private float _lastTime;

		public bool Register(float now, int required, float window)
		{
			if (_count > 0 && now - _lastTime > window)
			{
				_count = 0;
			}

			_count++;
			_lastTime = now;

			if (_count < required)
			{
				return false;
			}

			_count = 0;
			return true;
		}

		public void Reset() => _count = 0;
	}
}