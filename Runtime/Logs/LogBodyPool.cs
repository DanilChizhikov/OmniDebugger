using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal sealed class LogBodyPool
	{
		private readonly Dictionary<int, LogBody> _heads = new ();

		public int Count { get; private set; }

		public long TextLength { get; private set; }

		public LogBody Rent(in LogBodyKey key)
		{
			_heads.TryGetValue(key.Hash, out LogBody head);

			for (LogBody body = head; body != null; body = body.Next)
			{
				if (key.Matches(body))
				{
					body.References++;
					return body;
				}
			}

			LogBody created = new LogBody(key, head);
			_heads[key.Hash] = created;
			Count++;
			TextLength += created.TextLength;
			return created;
		}

		public void Return(LogBody body)
		{
			body.References--;

			if (body.References > 0)
			{
				return;
			}

			Unlink(body);
			Count--;
			TextLength -= body.TextLength;
		}

		public void Clear()
		{
			_heads.Clear();
			Count = 0;
			TextLength = 0;
		}

		private void Unlink(LogBody body)
		{
			if (!_heads.TryGetValue(body.Hash, out LogBody head))
			{
				return;
			}

			if (ReferenceEquals(head, body))
			{
				if (body.Next == null)
				{
					_heads.Remove(body.Hash);
				}
				else
				{
					_heads[body.Hash] = body.Next;
				}
			}
			else
			{
				LogBody previous = head;

				while (previous.Next != null && !ReferenceEquals(previous.Next, body))
				{
					previous = previous.Next;
				}

				if (previous.Next != null)
				{
					previous.Next = body.Next;
				}
			}

			body.Next = null;
		}
	}
}
