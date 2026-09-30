using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class InfoTab : IOmniDebuggerTab
	{
		private const long RedrawMs = 500;
		private const long GraphRedrawMs = 100;
		private const float TwoColumnsFrom = 460.0f;
		private const float OneColumnBelow = 420.0f;
		private const string FloatTooltip = "Float over the game";
		private const string DockTooltip = "Stop floating";

		private static readonly CustomStyleProperty<float> _refreshProperty = new ("--od-pulse-ms");

		private readonly IInfoRegistry _registry;
		private readonly ViewServices _services;
		private readonly VisualElement _root;
		private readonly ScrollView _scroll;
		private readonly VisualElement _flow;
		private readonly ValuePulse _pulse;
		private readonly List<InfoSectionView> _sections = new ();
		private readonly List<FloatToggle> _toggles = new ();
		private readonly IVisualElementScheduledItem _sampler;
		private readonly IVisualElementScheduledItem _redraw;
		private readonly IVisualElementScheduledItem _redrawGraphs;

		public VisualElement Root => _root;

		private bool _twoColumns;
		private bool _disposed;

		public InfoTab(IInfoRegistry registry, ViewServices services)
		{
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
			_services = services;

			_root = UiBuild.Element(OmniDebuggerUiClasses.TabPage);
			_scroll = UiBuild.Scroll();
			_root.Add(_scroll);

			_flow = UiBuild.Element(OmniDebuggerUiClasses.InfoFlow);
			_flow.RegisterCallback<GeometryChangedEvent>(OnFlowGeometryChanged);
			_scroll.Add(_flow);

			_pulse = new ValuePulse(_root);
			_root.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

			_sampler = _root.schedule.Execute(Sample).Every(0);
			_redraw = _root.schedule.Execute(Redraw).Every(RedrawMs);
			_redrawGraphs = _root.schedule.Execute(RedrawGraphs).Every(GraphRedrawMs);
			_sampler.Pause();
			_redraw.Pause();
			_redrawGraphs.Pause();

			_registry.OnChanged += Build;
			_registry.OnFloatingChanged += ShowFloating;
			Build();
		}

		public void OnOpen()
		{
			_sampler.Resume();
			_redraw.Resume();
			_redrawGraphs.Resume();
			_pulse.Resume();
			Redraw();
		}

		public void OnClose()
		{
			_sampler.Pause();
			_redraw.Pause();
			_redrawGraphs.Pause();
			_pulse.Pause();
		}

		public void Refresh()
		{
			if (!_disposed)
			{
				Build();
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_registry.OnChanged -= Build;
			_registry.OnFloatingChanged -= ShowFloating;
			_sampler.Pause();
			_redraw.Pause();
			_redrawGraphs.Pause();
			ClearSections();
			_pulse.Dispose();
			_root.RemoveFromHierarchy();
		}

		private void Build()
		{
			if (_disposed)
			{
				return;
			}

			ClearSections();

			IReadOnlyList<IInfoProvider> providers = _registry.All;
			for (int i = 0; i < providers.Count; i++)
			{
				BuildSection(providers[i]);
			}

			ShowFloating();
		}

		private void BuildSection(IInfoProvider provider)
		{
			InfoSectionModel model = InfoSectionModel.Describe(provider);

			VisualElement section = UiBuild.Element(OmniDebuggerUiClasses.Section);
			VisualElement head = UiBuild.Element(OmniDebuggerUiClasses.SectionHead);
			head.Add(UiBuild.Label(model.Title.ToUpperInvariant(), OmniDebuggerUiClasses.SectionTitle));
			section.Add(head);

			if (_services != null && _services.HostsOverlays)
			{
				Button toggle = UiBuild.IconButton(IconGlyph.PopOut, () => ToggleFloating(provider), FloatTooltip);
				toggle.AddToClassList(OmniDebuggerUiClasses.SectionFloat);
				head.Add(toggle);
				_toggles.Add(new FloatToggle(provider, toggle));
			}

			VisualElement cell = UiBuild.Element(OmniDebuggerUiClasses.InfoFlowCell);
			cell.AddToClassList(_sections.Count % 2 == 0 ? OmniDebuggerUiClasses.InfoFlowCellLeft : OmniDebuggerUiClasses.InfoFlowCellRight);
			cell.Add(section);
			_flow.Add(cell);

			_sections.Add(new InfoSectionView(model, section, _services, _pulse));
		}

		private void ToggleFloating(IInfoProvider provider)
		{
			if (!_registry.Float(provider))
			{
				_registry.Dock(provider);
			}
		}

		private void ShowFloating()
		{
			for (int i = 0; i < _toggles.Count; i++)
			{
				FloatToggle toggle = _toggles[i];
				bool floating = _registry.IsFloating(toggle.Provider);

				toggle.Button.EnableInClassList(OmniDebuggerUiClasses.SectionFloatOn, floating);
				toggle.Button.tooltip = floating ? DockTooltip : FloatTooltip;
				UiBuild.SetGlyph(toggle.Button, floating ? IconGlyph.Dock : IconGlyph.PopOut);
			}
		}

		private void ClearSections()
		{
			for (int i = 0; i < _sections.Count; i++)
			{
				_sections[i].Dispose();
			}

			_sections.Clear();
			_toggles.Clear();
			_flow.Clear();
		}

		private void Sample()
		{
			for (int i = 0; i < _sections.Count; i++)
			{
				_sections[i].Sample();
			}
		}

		private void Redraw()
		{
			for (int i = 0; i < _sections.Count; i++)
			{
				_sections[i].Redraw();
			}
		}

		private void RedrawGraphs()
		{
			for (int i = 0; i < _sections.Count; i++)
			{
				_sections[i].RedrawGraphs();
			}
		}

		private void OnFlowGeometryChanged(GeometryChangedEvent evt)
		{
			float width = evt.newRect.width;

			if (width <= 0.0f)
			{
				return;
			}

			bool twoColumns = _twoColumns ? width >= OneColumnBelow : width >= TwoColumnsFrom;

			if (twoColumns != _twoColumns)
			{
				_twoColumns = twoColumns;
				_flow.EnableInClassList(OmniDebuggerUiClasses.InfoFlowTwo, twoColumns);
			}
		}

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
		{
			if (evt.customStyle.TryGetValue(_refreshProperty, out float milliseconds) && milliseconds > 0.0f)
			{
				_pulse.SetInterval((long)milliseconds);
			}
		}

		private readonly struct FloatToggle
		{
			public readonly IInfoProvider Provider;
			public readonly Button Button;

			public FloatToggle(IInfoProvider provider, Button button)
			{
				Provider = provider;
				Button = button;
			}
		}
	}
}
