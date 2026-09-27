using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The debugger core. The game constructs it, holds it, and disposes it.
	/// </summary>
	public sealed class OmniDebugger : IOmniDebugger, IDisposable
	{
		private readonly CommandCatalog _catalog;
		private readonly CommandInvoker _invoker;
		private readonly GroupOrder _groups;

		/// <inheritdoc/>
		public ICommandCatalog Catalog
		{
			get
			{
				ThrowIfDisposed();
				return _catalog;
			}
		}

		/// <inheritdoc/>
		public ICommandInvoker Commands
		{
			get
			{
				ThrowIfDisposed();
				return _invoker;
			}
		}

		/// <inheritdoc/>
		public IGroupOrder Groups
		{
			get
			{
				ThrowIfDisposed();
				return _groups;
			}
		}
		
		private bool _disposed;

		/// <exception cref="InvalidOperationException">Constructed off the Unity main thread.</exception>
		public OmniDebugger() : this(UnityLogSink.Default)
		{
		}

		internal OmniDebugger(ILogSink log)
		{
			if (log == null)
			{
				throw new ArgumentNullException(nameof(log));
			}

			MainThreadGuard.Verify(nameof(OmniDebugger));

			_groups = new GroupOrder();
			_catalog = new CommandCatalog(log);
			_invoker = new CommandInvoker(_catalog, log);
		}

		/// <summary>
		/// Releases every registered source — MonoBehaviours included — and clears the
		/// <see cref="ICommandCatalog.OnChanged"/> subscriber list. Every member throws
		/// <see cref="ObjectDisposedException"/> afterwards. Safe to call more than once.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			_catalog.Clear();
			_groups.Clear();
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(OmniDebugger));
			}
		}
	}
}