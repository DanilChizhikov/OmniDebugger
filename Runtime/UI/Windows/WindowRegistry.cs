using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class WindowRegistry : IWindowRegistry
	{
		public event Action OnChanged;

		private readonly List<WindowRegistration> _windows = new ();
		private readonly List<IOmniDebuggerWindow> _view = new ();

		public IReadOnlyList<IOmniDebuggerWindow> All => _view;

		internal IReadOnlyList<WindowRegistration> Registrations => _windows;

		public IOmniDebuggerWindow RegisterCustom(
			string id,
			string title,
			Action<VisualElement> buildContent,
			bool open = false,
			Vector2 size = default)
		{
			if (buildContent == null)
			{
				throw new ArgumentNullException(nameof(buildContent));
			}

			return Add(new WindowRegistration(this, Validate(id), title, WindowKind.Custom, buildContent, null, size, open));
		}

		public IOmniDebuggerWindow RegisterCommands(
			string id,
			string title,
			IReadOnlyList<string> commandKeys,
			bool open = false,
			Vector2 size = default)
		{
			List<string> keys = new List<string>();

			if (commandKeys != null)
			{
				for (int i = 0; i < commandKeys.Count; i++)
				{
					if (!string.IsNullOrWhiteSpace(commandKeys[i]) && !keys.Contains(commandKeys[i]))
					{
						keys.Add(commandKeys[i]);
					}
				}
			}

			return Add(new WindowRegistration(this, Validate(id), title, WindowKind.Commands, null, keys, size, open));
		}

		public bool Unregister(string id)
		{
			MainThreadGuard.Verify(nameof(Unregister));

			int index = IndexOf(id);

			if (index < 0)
			{
				return false;
			}

			_windows[index].IsRegistered = false;
			_windows.RemoveAt(index);
			_view.RemoveAt(index);
			OnChanged?.Invoke();
			return true;
		}

		public bool TryGet(string id, out IOmniDebuggerWindow window)
		{
			int index = IndexOf(id);
			window = index >= 0 ? _windows[index] : null;
			return window != null;
		}

		public bool Open(string id) => SetOpen(id, true);

		public bool Close(string id) => SetOpen(id, false);

		public void CloseAll()
		{
			for (int i = 0; i < _windows.Count; i++)
			{
				_windows[i].Close();
			}
		}

		internal WindowRegistration RegisterInternal(WindowRegistration registration) => Add(registration);

		internal void NotifyChanged(WindowRegistration registration)
		{
			if (registration.IsRegistered)
			{
				OnChanged?.Invoke();
			}
		}

		internal void Clear()
		{
			for (int i = 0; i < _windows.Count; i++)
			{
				_windows[i].IsRegistered = false;
			}

			_windows.Clear();
			_view.Clear();
			OnChanged = null;
		}

		private static string Validate(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("A window needs an id.", nameof(id));
			}

			return id;
		}

		private WindowRegistration Add(WindowRegistration registration)
		{
			MainThreadGuard.Verify(nameof(Add));

			int index = IndexOf(registration.Id);

			if (index >= 0)
			{
				_windows[index].IsRegistered = false;
				_windows[index] = registration;
				_view[index] = registration;
			}
			else
			{
				_windows.Add(registration);
				_view.Add(registration);
			}

			OnChanged?.Invoke();
			return registration;
		}

		private bool SetOpen(string id, bool open)
		{
			int index = IndexOf(id);

			if (index < 0)
			{
				return false;
			}

			_windows[index].SetOpen(open);
			return true;
		}

		private int IndexOf(string id)
		{
			if (id == null)
			{
				return -1;
			}

			for (int i = 0; i < _windows.Count; i++)
			{
				if (string.Equals(_windows[i].Id, id, StringComparison.Ordinal))
				{
					return i;
				}
			}

			return -1;
		}
	}
}