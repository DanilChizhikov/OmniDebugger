using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger
{
	/// <summary>What an <see cref="IInfoProvider"/> puts in its section, row by row.</summary>
	public interface IInfoSection
	{
		/// <summary>A row whose value never changes while the section is shown.</summary>
		void Text(string label, string value);

		/// <summary>A row whose value is read again twice a second while the section is on screen.</summary>
		void Live(string label, Func<string> read);

		/// <summary>
		/// A line chart of the last couple of seconds, sampled every frame while the section is on screen, with
		/// the average of the latest samples written beside the label.
		/// </summary>
		/// <param name="min">Bottom of the chart.</param>
		/// <param name="max">Top of the chart; at or below <paramref name="min"/>, it follows the highest sample.</param>
		/// <param name="unit">Written after the value, such as <c>"fps"</c> or <c>"MB"</c>.</param>
		void Graph(string label, Func<float> sample, float min, float max, string unit = null);

		/// <summary>An element of your own, built when the section is.</summary>
		void Custom(Func<VisualElement> build);

		/// <summary>
		/// A command, runnable right from the section — the same row the Commands tab shows, live value included.
		/// A path no command is registered under is skipped until one is.
		/// </summary>
		/// <param name="path">The command's path, such as <c>"Player/God Mode"</c>.</param>
		void Command(string path);
	}
}
