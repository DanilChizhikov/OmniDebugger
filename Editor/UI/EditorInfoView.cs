#if OMNI_DEBUGGER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI.Editor
{
	internal sealed class EditorInfoView : IDisposable
	{
		private const long RedrawMs = 500;
		private const long GraphRedrawMs = 100;
		private const float GraphHeight = 56.0f;

		private static readonly Color _graphColor = new (0.35f, 0.65f, 1.0f);

		private readonly IInfoRegistry _registry;
		private readonly ScrollView _scroll;
		private readonly List<(InfoItem Item, TextField Field)> _live = new ();
		private readonly List<(InfoItem Item, TextField Field, InfoGraph Graph)> _graphs = new ();
		private readonly IVisualElementScheduledItem _sampler;
		private readonly IVisualElementScheduledItem _redraw;
		private readonly IVisualElementScheduledItem _redrawGraphs;

		public VisualElement Root => _scroll;

		private bool _disposed;

		public EditorInfoView(IInfoRegistry registry)
		{
			_registry = registry;
			_scroll = new ScrollView(ScrollViewMode.Vertical);
			_scroll.style.flexGrow = 1.0f;
			_scroll.contentContainer.style.paddingLeft = 6.0f;
			_scroll.contentContainer.style.paddingRight = 6.0f;

			_sampler = _scroll.schedule.Execute(Sample).Every(0);
			_redraw = _scroll.schedule.Execute(Redraw).Every(RedrawMs);
			_redrawGraphs = _scroll.schedule.Execute(RedrawGraphs).Every(GraphRedrawMs);

			_registry.OnChanged += Build;
			Build();
		}

		public void SetActive(bool active)
		{
			if (active)
			{
				_sampler.Resume();
				_redraw.Resume();
				_redrawGraphs.Resume();
				Redraw();
			}
			else
			{
				_sampler.Pause();
				_redraw.Pause();
				_redrawGraphs.Pause();
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
			SetActive(false);
			_scroll.RemoveFromHierarchy();
		}

		private static TextField ReadOnly(string label, string value)
		{
			TextField field = new TextField(label) { isReadOnly = true };
			field.SetValueWithoutNotify(value);
			return field;
		}

		private void Build()
		{
			if (_disposed)
			{
				return;
			}

			_live.Clear();
			_graphs.Clear();
			_scroll.Clear();

			foreach (IInfoProvider provider in _registry.All)
			{
				_scroll.Add(BuildSection(InfoSectionModel.Describe(provider)));
			}

			Redraw();
		}

		private VisualElement BuildSection(InfoSectionModel model)
		{
			EditorWindowPrefs prefs = EditorWindowPrefs.instance;
			Foldout foldout = new Foldout { text = model.Title };
			foldout.SetValueWithoutNotify(!prefs.IsSectionCollapsed(model.Title));
			foldout.RegisterValueChangedCallback(evt =>
			{
				if (evt.target == foldout)
				{
					prefs.SetSectionCollapsed(model.Title, !evt.newValue);
				}
			});

			foreach (InfoItem item in model.Items)
			{
				switch (item.Kind)
				{
					case InfoItemKind.Text:
						foldout.Add(ReadOnly(item.Label, item.Value));
						break;

					case InfoItemKind.Live:
						TextField live = ReadOnly(item.Label, item.Value);
						foldout.Add(live);
						_live.Add((item, live));
						break;

					case InfoItemKind.Graph:
						TextField average = ReadOnly(item.Label, item.Value);
						foldout.Add(average);

						InfoGraph graph = new InfoGraph(item.Samples);
						graph.style.height = GraphHeight;
						graph.style.color = _graphColor;
						graph.style.marginBottom = 4.0f;
						foldout.Add(graph);
						_graphs.Add((item, average, graph));
						break;

					case InfoItemKind.Custom:
						try
						{
							VisualElement element = item.Build();
							if (element != null)
							{
								foldout.Add(element);
							}
						}
						catch (Exception exception)
						{
							Debug.LogException(exception);
						}

						break;

					case InfoItemKind.Command:
						foldout.Add(ReadOnly(item.Label, item.Path));
						break;
				}
			}

			if (model.Error != null)
			{
				foldout.Add(new HelpBox(model.Error.Message, HelpBoxMessageType.Error));
			}

			return foldout;
		}

		private void Sample()
		{
			for (int i = 0; i < _graphs.Count; i++)
			{
				_graphs[i].Item.Samples.Sample();
			}
		}

		private void Redraw()
		{
			for (int i = 0; i < _live.Count; i++)
			{
				_live[i].Field.SetValueWithoutNotify(_live[i].Item.ReadNow());
			}

			for (int i = 0; i < _graphs.Count; i++)
			{
				_graphs[i].Field.SetValueWithoutNotify(_graphs[i].Item.Samples.TakeAverage(_graphs[i].Item.Unit));
			}
		}

		private void RedrawGraphs()
		{
			for (int i = 0; i < _graphs.Count; i++)
			{
				_graphs[i].Graph.MarkDirtyRepaint();
			}
		}
	}
}
#endif
