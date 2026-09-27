namespace DTech.OmniDebugger
{
	internal readonly struct BindCommandResponse
	{
		public bool Success { get; }
		public object[] Bound { get; }
		public string Error { get; }

		public BindCommandResponse(object[] bound, string error)
		{
			Bound = bound;
			Error = error;
			Success = string.IsNullOrEmpty(error);
		}
	}
}