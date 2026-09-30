using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class HotbarOverlay : IDisposable
	{
		private const string FoldTooltip = "Fold the hotbar";
		private const string UnfoldTooltip = "Show the hotbar";

		private static readonly CustomStyleProperty<float> _refreshProperty = new ("--od-pulse-ms");

		private readonly ViewServices _services;
		private readonly IViewPrefs _prefs;
		private readonly IHotbar _hotbar;
		private readonly VisualElement _root;
		private readonly ScrollView _strip;
		private readonly Button _fold;
		private readonly Label _count;
		private readonly ValuePulse _pulse;
		private readonly List<HotbarItem> _items = new ();

		private bool _visible = true;
		private bool _collapsed;
		private bool _disposed;

		public HotbarOverlay(VisualElement parent, ViewServices services, IViewPrefs prefs)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_prefs = prefs;
			_hotbar = services.Debugger.Hotbar;

			_root = UiBuild.Element(OmniDebuggerUiClasses.Hotbar);
			_root.pickingMode = PickingMode.Ignore;

			VisualElement bar = UiBuild.Element(OmniDebuggerUiClasses.HotbarBar);
			bar.AddManipulator(new Halo());
			_root.Add(bar);

			_fold = new Button(ToggleCollapsed);
			_fold.AddToClassList(OmniDebuggerUiClasses.HotbarFold);
			_fold.Add(new OmniIcon(IconGlyph.Pin));
			_count = UiBuild.Label(string.Empty, OmniDebuggerUiClasses.HotbarCount);
			_fold.Add(_count);
			bar.Add(_fold);

			_strip = new ScrollView(ScrollViewMode.Horizontal)
			{
				horizontalScrollerVisibility = ScrollerVisibility.Hidden,
				verticalScrollerVisibility = ScrollerVisibility.Hidden,
			};

			_strip.AddToClassList(OmniDebuggerUiClasses.HotbarStrip);
			_strip.contentContainer.AddToClassList(OmniDebuggerUiClasses.HotbarStripContent);
			UiBuild.MakeDraggable(_strip);
			bar.Add(_strip);

			_pulse = new ValuePulse(_root);
			_root.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

			_collapsed = _prefs != null && _prefs.GetHotbarCollapsed();
			ApplyEdge(OmniDebuggerHotbarEdge.Bottom);

			parent.Add(_root);
			_hotbar.OnChanged += Refresh;
			Refresh();
		}

		public void SetEdge(OmniDebuggerHotbarEdge edge)
		{
			if (!_disposed)
			{
				ApplyEdge(edge);
			}
		}

		public void SetVisible(bool visible)
		{
			_visible = visible;
			ApplyVisibility();
		}

		public void SetInsets(Vector4 insets)
		{
			_root.style.paddingLeft = insets.x;
			_root.style.paddingRight = insets.y;
			_root.style.paddingTop = insets.z;
			_root.style.paddingBottom = insets.w;
		}

		public void Refresh()
		{
			if (_disposed)
			{
				return;
			}

			ClearItems();

			ICommandRegistry commands = _services.Debugger.Commands;
			IReadOnlyList<string> paths = _hotbar.Paths;

			for (int i = 0; i < paths.Count; i++)
			{
				if (!commands.TryGet(paths[i], out CommandDefinition definition))
				{
					continue;
				}

				HotbarItem item = new HotbarItem(_services, _pulse, definition);
				_items.Add(item);
				_strip.Add(item);
			}

			_count.text = _items.Count.ToString(CultureInfo.InvariantCulture);
			ApplyCollapsed();
			ApplyVisibility();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_hotbar.OnChanged -= Refresh;
			_root.UnregisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
			ClearItems();
			_pulse.Dispose();
			_root.RemoveFromHierarchy();
		}

		private void ApplyEdge(OmniDebuggerHotbarEdge edge)
		{
			_root.EnableInClassList(OmniDebuggerUiClasses.HotbarTop, edge == OmniDebuggerHotbarEdge.Top);
			_root.EnableInClassList(OmniDebuggerUiClasses.HotbarBottom, edge != OmniDebuggerHotbarEdge.Top);
		}

		private void ToggleCollapsed()
		{
			_collapsed = !_collapsed;
			_prefs?.SetHotbarCollapsed(_collapsed);
			ApplyCollapsed();
		}

		private void ApplyCollapsed()
		{
			_root.EnableInClassList(OmniDebuggerUiClasses.HotbarCollapsed, _collapsed);
			_fold.tooltip = _collapsed ? UnfoldTooltip : FoldTooltip;
			UiBuild.SetVisible(_strip, !_collapsed);
		}

		private void ApplyVisibility()
		{
			bool shown = _visible && _items.Count > 0;
			UiBuild.SetVisible(_root, shown);

			if (shown)
			{
				_pulse.Resume();
			}
			else
			{
				_pulse.Pause();
			}
		}

		private void ClearItems()
		{
			for (int i = 0; i < _items.Count; i++)
			{
				_items[i].Dispose();
			}

			_items.Clear();
			_strip.Clear();
		}

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
		{
			if (evt.customStyle.TryGetValue(_refreshProperty, out float milliseconds) && milliseconds > 0.0f)
			{
				_pulse.SetInterval((long)milliseconds);
			}
		}
	}
}
