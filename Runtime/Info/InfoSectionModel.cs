using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger
{
	internal sealed class InfoSectionModel : IInfoSection
	{
		public const string Unknown = "—";

		private readonly List<InfoItem> _items = new ();

		public string Title { get; }

		public IReadOnlyList<InfoItem> Items => _items;

		public Exception Error { get; private set; }

		private InfoSectionModel(string title)
		{
			Title = string.IsNullOrWhiteSpace(title) ? Unknown : title;
		}

		public static InfoSectionModel Describe(IInfoProvider provider)
		{
			InfoSectionModel model = new InfoSectionModel(SafeTitle(provider));

			try
			{
				provider.Describe(model);
			}
			catch (Exception exception)
			{
				model.Error = exception;
				UnityLogSink.Default.Exception($"Info section '{model.Title}' failed to describe itself.", exception);
			}

			return model;
		}

		public void Text(string label, string value) =>
			_items.Add(new InfoItem(InfoItemKind.Text, label, string.IsNullOrEmpty(value) ? Unknown : value));

		public void Live(string label, Func<string> read)
		{
			if (read == null)
			{
				throw new ArgumentNullException(nameof(read));
			}

			_items.Add(new InfoItem(InfoItemKind.Live, label, Unknown) { Read = read });
		}

		public void Graph(string label, Func<float> sample, float min, float max, string unit = null)
		{
			if (sample == null)
			{
				throw new ArgumentNullException(nameof(sample));
			}

			_items.Add(new InfoItem(InfoItemKind.Graph, label, Unknown)
			{
				Samples = new InfoSamples(sample, min, max),
				Unit = unit,
			});
		}

		public void Custom(Func<VisualElement> build)
		{
			if (build == null)
			{
				throw new ArgumentNullException(nameof(build));
			}

			_items.Add(new InfoItem(InfoItemKind.Custom, null, null) { Build = build });
		}

		public void Command(string path)
		{
			string normalized = CommandPath.Normalize(path);

			if (normalized.Length == 0)
			{
				throw new ArgumentException("A command row needs a path.", nameof(path));
			}

			_items.Add(new InfoItem(InfoItemKind.Command, CommandPath.GetName(normalized), normalized) { Path = normalized });
		}

		private static string SafeTitle(IInfoProvider provider)
		{
			try
			{
				return provider.Title;
			}
			catch (Exception)
			{
				return Unknown;
			}
		}
	}
}
