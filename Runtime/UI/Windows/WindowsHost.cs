using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class WindowsHost : IDisposable
	{
		private const float EdgePadding = 6.0f;
		private const float StartMargin = 12.0f;
		private const float CascadeStep = 10.0f;
		private const string NoCommandsMessage = "No commands.";

		private readonly VisualElement _layer;
		private readonly ViewServices _services;
		private readonly ICommandCatalog _catalog;
		private readonly WindowRegistry _registry;
		private readonly Dictionary<string, WindowFrame> _frames = new (StringComparer.Ordinal);
		private readonly Dictionary<string, Vector2> _positions = new (StringComparer.Ordinal);
		private readonly List<string> _stale = new ();

		private Vector4 _insets;
		private bool _visible = true;
		private bool _disposed;

		public WindowsHost(VisualElement layer, ViewServices services, WindowRegistry registry)
		{
			_layer = layer ?? throw new ArgumentNullException(nameof(layer));
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));

			_layer.RegisterCallback<GeometryChangedEvent>(OnLayerGeometryChanged);

			_registry.OnChanged += Sync;
			_registry.OnScaleChanged += ApplyScale;
			_catalog = _services.Debugger.Catalog;
			_catalog.OnChanged += OnCatalogChanged;

			if (_services.Pins != null)
			{
				_services.Pins.OnChanged += SyncPinned;
			}

			SyncPinned();
			Sync();
		}

		public void SetVisible(bool visible)
		{
			if (visible == _visible)
			{
				return;
			}

			_visible = visible;
			_layer.EnableInClassList(OmniDebuggerUiClasses.DeskWindowsHidden, !visible);

			if (visible)
			{
				Sync();
			}
		}

		public void SetInsets(Vector4 insets)
		{
			_insets = insets;
			ClampAll();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_registry.OnChanged -= Sync;
			_registry.OnScaleChanged -= ApplyScale;
			_catalog.OnChanged -= OnCatalogChanged;

			if (_services.Pins != null)
			{
				_services.Pins.OnChanged -= SyncPinned;

				if (_registry.TryGet(PinnedWindow.Id, out IOmniDebuggerWindow pinned) &&
					pinned is WindowRegistration registration &&
					registration.Kind == WindowKind.Pinned)
				{
					_registry.Unregister(PinnedWindow.Id);
				}
			}

			foreach (KeyValuePair<string, WindowFrame> pair in _frames)
			{
				pair.Value.Dispose();
			}

			_frames.Clear();
			_layer.UnregisterCallback<GeometryChangedEvent>(OnLayerGeometryChanged);
			_layer.RemoveFromClassList(OmniDebuggerUiClasses.DeskWindowsHidden);
		}

		private static bool IsRegistered(IReadOnlyList<WindowRegistration> registrations, string id)
		{
			for (int i = 0; i < registrations.Count; i++)
			{
				if (string.Equals(registrations[i].Id, id, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private void Sync()
		{
			if (_disposed)
			{
				return;
			}

			IReadOnlyList<WindowRegistration> registrations = _registry.Registrations;

			_stale.Clear();

			foreach (KeyValuePair<string, WindowFrame> pair in _frames)
			{
				if (!pair.Value.Registration.IsRegistered || !pair.Value.Registration.IsOpen)
				{
					_stale.Add(pair.Key);
				}
			}

			for (int i = 0; i < _stale.Count; i++)
			{
				string id = _stale[i];
				WindowFrame frame = _frames[id];

				if (frame.Registration.IsRegistered)
				{
					_positions[id] = frame.Position;
				}
				else if (!IsRegistered(registrations, id))
				{
					_positions.Remove(id);
				}

				frame.Dispose();
				_frames.Remove(id);
			}

			for (int i = 0; i < registrations.Count; i++)
			{
				WindowRegistration registration = registrations[i];

				if (!registration.IsOpen)
				{
					continue;
				}

				if (_frames.TryGetValue(registration.Id, out WindowFrame existing))
				{
					existing.ApplyCollapsed();
					continue;
				}

				Show(registration, _frames.Count);
			}
		}

		private void Show(WindowRegistration registration, int cascade)
		{
			WindowFrame frame = new WindowFrame(registration, GetBounds, OnFrameMoved);
			frame.SetScale(_registry.Scale);
			_frames.Add(registration.Id, frame);
			_layer.Add(frame);

			FillContent(frame);

			bool remembered = _positions.TryGetValue(registration.Id, out Vector2 position);
			frame.RegisterCallback<GeometryChangedEvent>(PlaceOnce);

			void PlaceOnce(GeometryChangedEvent evt)
			{
				frame.UnregisterCallback<GeometryChangedEvent>(PlaceOnce);
				frame.MoveTo(remembered ? position : StartPosition(frame, cascade));
			}
		}

		private void FillContent(WindowFrame frame)
		{
			WindowRegistration registration = frame.Registration;

			switch (registration.Kind)
			{
				case WindowKind.Custom:
					try
					{
						registration.BuildContent?.Invoke(frame.Content);
					}
					catch (Exception exception)
					{
						UnityLogSink.Default.Exception($"Window content failed to build. Id: {registration.Id}.", exception);
					}

					break;

				case WindowKind.Pinned:
					FillCommands(frame, _services.Pins.Keys);
					frame.ShowFooter();
					frame.Footer.Add(UiBuild.TextButton(PinnedWindow.ClearLabel, ClearPins, OmniDebuggerUiClasses.ButtonDestructive));
					break;

				default:
					FillCommands(frame, registration.CommandKeys);
					break;
			}
		}

		private void FillCommands(WindowFrame frame, IReadOnlyList<string> keys)
		{
			ValuePulse pulse = new ValuePulse(frame);
			pulse.Resume();
			frame.Own(pulse);

			int shown = 0;

			for (int i = 0; i < keys.Count; i++)
			{
				if (!_services.Debugger.Catalog.TryGetDefinition(keys[i], out CommandDefinition definition))
				{
					continue;
				}

				CommandRow row = new CommandRow(_services, pulse, definition, CommandRowMode.Compact);
				row.EnableInClassList(OmniDebuggerUiClasses.First, shown == 0);
				frame.Own(row);
				frame.Content.Add(row);
				shown++;
			}

			if (shown == 0)
			{
				frame.Content.Add(UiBuild.Label(NoCommandsMessage, OmniDebuggerUiClasses.Empty));
			}
		}

		private void SyncPinned()
		{
			if (_disposed || _services.Pins == null)
			{
				return;
			}

			bool registered = _registry.TryGet(PinnedWindow.Id, out IOmniDebuggerWindow existing);

			if (_services.Pins.Count == 0)
			{
				if (registered)
				{
					_registry.Unregister(PinnedWindow.Id);
				}

				return;
			}

			bool open = registered && existing.IsOpen;
			bool collapsed = registered && existing.IsCollapsed;

			WindowRegistration registration = new WindowRegistration(
				_registry,
				PinnedWindow.Id,
				PinnedWindow.Title,
				WindowKind.Pinned,
				null,
				null,
				Vector2.zero,
				open,
				collapsed);

			if (_frames.TryGetValue(PinnedWindow.Id, out WindowFrame frame))
			{
				_positions[PinnedWindow.Id] = frame.Position;
				frame.Dispose();
				_frames.Remove(PinnedWindow.Id);
			}

			_registry.RegisterInternal(registration);
		}

		private void ClearPins() => _services.Pins?.Clear();

		private void OnCatalogChanged()
		{
			foreach (KeyValuePair<string, WindowFrame> pair in _frames)
			{
				if (pair.Value.Registration.Kind != WindowKind.Custom)
				{
					_stale.Add(pair.Key);
				}
			}

			for (int i = 0; i < _stale.Count; i++)
			{
				WindowFrame frame = _frames[_stale[i]];
				_positions[_stale[i]] = frame.Position;
				frame.Dispose();
				_frames.Remove(_stale[i]);
			}

			_stale.Clear();
			Sync();
		}

		private void OnFrameMoved(WindowFrame frame) => _positions[frame.Registration.Id] = frame.Position;

		private void OnLayerGeometryChanged(GeometryChangedEvent evt) => ClampAll();

		private void ApplyScale()
		{
			foreach (KeyValuePair<string, WindowFrame> pair in _frames)
			{
				pair.Value.SetScale(_registry.Scale);
			}

			ClampAll();
		}

		private void ClampAll()
		{
			foreach (KeyValuePair<string, WindowFrame> pair in _frames)
			{
				pair.Value.MoveTo(pair.Value.Position);
			}
		}

		private Rect GetBounds()
		{
			Rect area = _layer.layout;

			return Rect.MinMaxRect(
				_insets.x + EdgePadding,
				_insets.z + EdgePadding,
				Mathf.Max(_insets.x + EdgePadding, area.width - _insets.y - EdgePadding),
				Mathf.Max(_insets.z + EdgePadding, area.height - _insets.w - EdgePadding));
		}

		private Vector2 StartPosition(WindowFrame frame, int cascade)
		{
			Rect bounds = GetBounds();
			float width = frame.resolvedStyle.width * _registry.Scale;
			float offset = CascadeStep * cascade;

			return new Vector2(
				bounds.xMax - width - StartMargin - offset,
				bounds.yMin + StartMargin + offset);
		}
	}
}