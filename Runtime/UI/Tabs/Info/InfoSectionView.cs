using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class InfoSectionView : IDisposable
	{
		private readonly InfoSectionModel _model;
		private readonly VisualElement _container;
		private readonly ViewServices _services;
		private readonly ValuePulse _pulse;
		private readonly List<LiveRow> _live = new ();
		private readonly List<GraphRow> _graphs = new ();
		private readonly List<CommandRow> _commands = new ();

		public InfoSectionModel Model => _model;

		private int _rows;
		private bool _disposed;

		public InfoSectionView(InfoSectionModel model, VisualElement container, ViewServices services, ValuePulse pulse)
		{
			_model = model ?? throw new ArgumentNullException(nameof(model));
			_container = container ?? throw new ArgumentNullException(nameof(container));
			_services = services;
			_pulse = pulse;

			foreach (InfoItem item in model.Items)
			{
				Add(item);
			}

			if (model.Error != null)
			{
				_container.Add(UiBuild.Label(model.Error.Message, OmniDebuggerUiClasses.Empty));
			}

			Redraw();
		}

		public void Sample()
		{
			for (int i = 0; i < _graphs.Count; i++)
			{
				_graphs[i].Item.Samples.Sample();
			}
		}

		public void Redraw()
		{
			for (int i = 0; i < _live.Count; i++)
			{
				_live[i].Label.text = _live[i].Item.ReadNow();
			}

			for (int i = 0; i < _graphs.Count; i++)
			{
				GraphRow graph = _graphs[i];
				graph.Label.text = graph.Item.Samples.TakeAverage(graph.Item.Unit);
			}
		}

		public void RedrawGraphs()
		{
			for (int i = 0; i < _graphs.Count; i++)
			{
				_graphs[i].Graph.MarkDirtyRepaint();
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			for (int i = 0; i < _commands.Count; i++)
			{
				_commands[i].Dispose();
			}

			_commands.Clear();
			_live.Clear();
			_graphs.Clear();
		}

		private void Add(InfoItem item)
		{
			switch (item.Kind)
			{
				case InfoItemKind.Text:
					AddRow(item.Label, item.Value, out _);
					break;

				case InfoItemKind.Live:
					AddRow(item.Label, item.Value, out Label live);
					_live.Add(new LiveRow(item, live));
					break;

				case InfoItemKind.Graph:
					VisualElement row = AddRow(item.Label, item.Value, out Label average);
					row.AddToClassList(OmniDebuggerUiClasses.InfoGraphRow);
					InfoGraph graph = new InfoGraph(item.Samples);
					_container.Add(graph);
					_graphs.Add(new GraphRow(item, average, graph));
					break;

				case InfoItemKind.Custom:
					AddCustom(item);
					break;

				case InfoItemKind.Command:
					AddCommand(item);
					break;
			}
		}

		private VisualElement AddRow(string key, string value, out Label label)
		{
			VisualElement row = UiBuild.Element(OmniDebuggerUiClasses.KeyValueRow);
			row.EnableInClassList(OmniDebuggerUiClasses.First, _rows++ == 0);
			row.Add(UiBuild.Label(key, OmniDebuggerUiClasses.KeyValueKey));

			label = UiBuild.Label(value, OmniDebuggerUiClasses.KeyValueValue);
			row.Add(label);
			_container.Add(row);
			return row;
		}

		private void AddCustom(InfoItem item)
		{
			try
			{
				VisualElement element = item.Build();
				if (element != null)
				{
					_container.Add(element);
				}
			}
			catch (Exception exception)
			{
				UnityLogSink.Default.Exception($"Info section '{_model.Title}' failed to build an element.", exception);
			}
		}

		private void AddCommand(InfoItem item)
		{
			if (_services == null || !_services.Debugger.Commands.TryGet(item.Path, out CommandDefinition definition))
			{
				return;
			}

			CommandRow row = new CommandRow(_services, _pulse, definition, CommandRowMode.Compact);
			row.AddToClassList(OmniDebuggerUiClasses.InfoCommandRow);
			row.EnableInClassList(OmniDebuggerUiClasses.First, _rows++ == 0);
			_commands.Add(row);
			_container.Add(row);
		}

		private readonly struct LiveRow
		{
			public readonly InfoItem Item;
			public readonly Label Label;

			public LiveRow(InfoItem item, Label label)
			{
				Item = item;
				Label = label;
			}
		}

		private readonly struct GraphRow
		{
			public readonly InfoItem Item;
			public readonly Label Label;
			public readonly InfoGraph Graph;

			public GraphRow(InfoItem item, Label label, InfoGraph graph)
			{
				Item = item;
				Label = label;
				Graph = graph;
			}
		}
	}
}
