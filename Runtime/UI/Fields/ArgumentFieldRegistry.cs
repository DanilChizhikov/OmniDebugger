using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ArgumentFieldRegistry : IArgumentFieldRegistry
	{
		private static readonly Comparison<Registration> _order = Compare;

		private readonly List<Registration> _handlers = new ();
		private readonly ILogSink _log;

		private int _sequence;
		private bool _dirty;

		public ArgumentFieldRegistry(ILogSink log)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));

			Add(new BoolArgumentFieldHandler());
			Add(new EnumArgumentFieldHandler());
			Add(new NumericArgumentFieldHandler());
			Add(new TextArgumentFieldHandler());
		}

		public void Register(IArgumentFieldHandler handler)
		{
			MainThreadGuard.Verify(nameof(Register));

			if (handler == null)
			{
				throw new ArgumentNullException(nameof(handler));
			}

			Add(handler);
		}

		public bool Unregister(IArgumentFieldHandler handler)
		{
			MainThreadGuard.Verify(nameof(Unregister));

			if (handler == null)
			{
				throw new ArgumentNullException(nameof(handler));
			}

			for (int i = 0; i < _handlers.Count; i++)
			{
				if (!ReferenceEquals(_handlers[i].Handler, handler))
				{
					continue;
				}

				_handlers.RemoveAt(i);
				return true;
			}

			return false;
		}

		public IArgumentField Create(in ArgumentFieldRequest request)
		{
			MainThreadGuard.Verify(nameof(Create));
			Sort();

			IArgumentField field = null;

			for (int i = 0; i < _handlers.Count; i++)
			{
				IArgumentFieldHandler handler = _handlers[i].Handler;

				if (!handler.CanHandle(request.ValueType))
				{
					continue;
				}

				try
				{
					field = handler.Create(request);
				}
				catch (Exception exception)
				{
					_log.Exception(
						$"Argument field failed to build. Argument: {request.Argument.Name}; " +
						$"Type: {request.ValueType.Name}.",
						exception);
				}

				if (field != null)
				{
					break;
				}
			}

			field ??= new UnsupportedArgumentField(request);

			return request.IsNullable ? new NullableArgumentField(field, request) : field;
		}

		internal void Clear()
		{
			_handlers.Clear();
			_sequence = 0;
			_dirty = false;
		}

		private static int Compare(Registration left, Registration right)
		{
			int byPriority = right.Handler.Priority.CompareTo(left.Handler.Priority);
			return byPriority != 0 ? byPriority : left.Sequence.CompareTo(right.Sequence);
		}

		private void Add(IArgumentFieldHandler handler)
		{
			_handlers.Add(new Registration(handler, _sequence++));
			_dirty = true;
		}

		private void Sort()
		{
			if (!_dirty)
			{
				return;
			}

			_dirty = false;

			_handlers.Sort(_order);
		}

		private readonly struct Registration
		{
			public IArgumentFieldHandler Handler { get; }

			public int Sequence { get; }

			public Registration(IArgumentFieldHandler handler, int sequence)
			{
				Handler = handler;
				Sequence = sequence;
			}
		}
	}
}