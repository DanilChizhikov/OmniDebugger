using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class FloatingSectionsHost : IDisposable
	{
		private const float EdgePadding = 6.0f;
		private const float Margin = 12.0f;
		private const float Gap = 8.0f;
		private const long RedrawMs = 500;
		private const long GraphRedrawMs = 100;

		private static readonly CustomStyleProperty<float> _refreshProperty = new ("--od-pulse-ms");

		private readonly VisualElement _layer;
		private readonly ViewServices _services;
		private readonly InfoRegistry _registry;
		private readonly ICommandRegistry _commands;
		private readonly IViewPrefs _prefs;
		private readonly ValuePulse _pulse;
		private readonly List<Card> _cards = new ();
		private readonly Dictionary<string, Vector2> _positions = new (StringComparer.Ordinal);
		private readonly Dictionary<string, float> _scales = new (StringComparer.Ordinal);
		private readonly IVisualElementScheduledItem _sampler;
		private readonly IVisualElementScheduledItem _redraw;
		private readonly IVisualElementScheduledItem _redrawGraphs;

		private Vector4 _insets;
		private bool _visible = true;
		private bool _disposed;

		public FloatingSectionsHost(VisualElement layer, ViewServices services, InfoRegistry registry, IViewPrefs prefs)
		{
			_layer = layer ?? throw new ArgumentNullException(nameof(layer));
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
			_commands = services.Debugger.Commands;
			_prefs = prefs;

			if (_prefs != null)
			{
				foreach (KeyValuePair<string, float> pair in _prefs.GetSectionScales())
				{
					_scales[pair.Key] = pair.Value;
				}
			}

			_pulse = new ValuePulse(_layer);
			_sampler = _layer.schedule.Execute(Sample).Every(0);
			_redraw = _layer.schedule.Execute(Redraw).Every(RedrawMs);
			_redrawGraphs = _layer.schedule.Execute(RedrawGraphs).Every(GraphRedrawMs);

			_layer.RegisterCallback<GeometryChangedEvent>(OnLayerGeometryChanged);
			_registry.OnChanged += Rebuild;
			_registry.OnFloatingChanged += Sync;
			_commands.OnChanged += OnCommandsChanged;

			Sync();
		}

		public void SetVisible(bool visible)
		{
			if (visible == _visible)
			{
				return;
			}

			_visible = visible;
			_layer.EnableInClassList(OmniDebuggerUiClasses.DeskFloatingHidden, !visible);
			UpdateClock();
		}

		public void SetInsets(Vector4 insets)
		{
			_insets = insets;
			ClampAll();
		}

		public void Refresh() => Rebuild();

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_registry.OnChanged -= Rebuild;
			_registry.OnFloatingChanged -= Sync;
			_commands.OnChanged -= OnCommandsChanged;
			_layer.UnregisterCallback<GeometryChangedEvent>(OnLayerGeometryChanged);
			_layer.RemoveFromClassList(OmniDebuggerUiClasses.DeskFloatingHidden);

			for (int i = 0; i < _cards.Count; i++)
			{
				_cards[i].Dispose();
			}

			_cards.Clear();
			_sampler.Pause();
			_redraw.Pause();
			_redrawGraphs.Pause();
			_pulse.Dispose();
		}

		private static bool Contains(IReadOnlyList<string> keys, string key)
		{
			for (int i = 0; i < keys.Count; i++)
			{
				if (string.Equals(keys[i], key, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private static bool HasCommands(InfoSectionModel model)
		{
			foreach (InfoItem item in model.Items)
			{
				if (item.Kind == InfoItemKind.Command)
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

			IReadOnlyList<string> keys = _registry.FloatingKeys;

			for (int i = _cards.Count - 1; i >= 0; i--)
			{
				Card card = _cards[i];

				if (!Contains(keys, card.Key) ||
					!_registry.TryGetProvider(card.Key, out IInfoProvider provider) ||
					!ReferenceEquals(provider, card.Provider))
				{
					Remove(i);
				}
			}

			for (int i = 0; i < keys.Count; i++)
			{
				if (IndexOf(keys[i]) < 0 && _registry.TryGetProvider(keys[i], out IInfoProvider provider))
				{
					Show(keys[i], provider);
				}
			}

			UpdateClock();
		}

		private void Rebuild()
		{
			for (int i = _cards.Count - 1; i >= 0; i--)
			{
				Remove(i);
			}

			Sync();
		}

		private void OnCommandsChanged()
		{
			for (int i = _cards.Count - 1; i >= 0; i--)
			{
				if (HasCommands(_cards[i].View.Model))
				{
					Remove(i);
				}
			}

			Sync();
		}

		private void Show(string key, IInfoProvider provider)
		{
			InfoSectionModel model = InfoSectionModel.Describe(provider);
			FloatingSection frame = new FloatingSection(key, model.Title, () => _registry.Dock(provider), GetBounds, OnMoved, OnResized);

			if (_scales.TryGetValue(key, out float scale))
			{
				frame.SetScale(scale);
			}

			frame.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
			_layer.Add(frame);

			InfoSectionView view = new InfoSectionView(model, frame.Rows, _services, _pulse);
			_cards.Add(new Card(key, provider, frame, view));

			frame.RegisterCallback<GeometryChangedEvent>(PlaceOnce);

			void PlaceOnce(GeometryChangedEvent evt)
			{
				frame.UnregisterCallback<GeometryChangedEvent>(PlaceOnce);
				frame.MoveTo(_positions.TryGetValue(key, out Vector2 position) ? position : NextSlot(frame));
			}
		}

		private void Remove(int index)
		{
			Card card = _cards[index];

			if (card.Frame.IsPlaced)
			{
				_positions[card.Key] = card.Frame.Position;
			}

			card.Dispose();
			_cards.RemoveAt(index);
		}

		private Vector2 NextSlot(FloatingSection frame)
		{
			Rect bounds = GetBounds();
			Vector2 size = frame.ScaledSize;
			float top = bounds.yMin + Margin;

			for (float x = bounds.xMax - Margin - size.x; x >= bounds.xMin; x -= size.x + Gap)
			{
				float y = top;

				for (int i = 0; i < _cards.Count; i++)
				{
					FloatingSection other = _cards[i].Frame;

					if (other == frame || !other.IsPlaced)
					{
						continue;
					}

					Vector2 at = other.Position;
					Vector2 extent = other.ScaledSize;

					if (at.x < x + size.x && at.x + extent.x > x)
					{
						y = Mathf.Max(y, at.y + extent.y + Gap);
					}
				}

				if (y + size.y <= bounds.yMax - Margin)
				{
					return new Vector2(x, y);
				}
			}

			return new Vector2(bounds.xMax - Margin - size.x, top);
		}

		private void OnMoved(FloatingSection frame) => _positions[frame.Key] = frame.Position;

		private void OnResized(FloatingSection frame)
		{
			_scales[frame.Key] = frame.Scale;
			_prefs?.SetSectionScales(_scales);
		}

		private void UpdateClock()
		{
			if (_disposed)
			{
				return;
			}

			bool running = _visible && _cards.Count > 0;

			if (running)
			{
				_sampler.Resume();
				_redraw.Resume();
				_redrawGraphs.Resume();
				_pulse.Resume();
				Redraw();
			}
			else
			{
				_sampler.Pause();
				_redraw.Pause();
				_redrawGraphs.Pause();
				_pulse.Pause();
			}
		}

		private void Sample()
		{
			for (int i = 0; i < _cards.Count; i++)
			{
				_cards[i].View.Sample();
			}
		}

		private void Redraw()
		{
			for (int i = 0; i < _cards.Count; i++)
			{
				_cards[i].View.Redraw();
			}
		}

		private void RedrawGraphs()
		{
			for (int i = 0; i < _cards.Count; i++)
			{
				_cards[i].View.RedrawGraphs();
			}
		}

		private void OnLayerGeometryChanged(GeometryChangedEvent evt) => ClampAll();

		private void ClampAll()
		{
			for (int i = 0; i < _cards.Count; i++)
			{
				FloatingSection frame = _cards[i].Frame;

				if (frame.IsPlaced)
				{
					frame.MoveTo(frame.Position);
				}
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

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
		{
			if (evt.customStyle.TryGetValue(_refreshProperty, out float milliseconds) && milliseconds > 0.0f)
			{
				_pulse.SetInterval((long)milliseconds);
			}
		}

		private int IndexOf(string key)
		{
			for (int i = 0; i < _cards.Count; i++)
			{
				if (string.Equals(_cards[i].Key, key, StringComparison.Ordinal))
				{
					return i;
				}
			}

			return -1;
		}

		private sealed class Card : IDisposable
		{
			public readonly string Key;
			public readonly IInfoProvider Provider;
			public readonly FloatingSection Frame;
			public readonly InfoSectionView View;

			public Card(string key, IInfoProvider provider, FloatingSection frame, InfoSectionView view)
			{
				Key = key;
				Provider = provider;
				Frame = frame;
				View = view;
			}

			public void Dispose()
			{
				View.Dispose();
				Frame.Dispose();
			}
		}
	}
}
