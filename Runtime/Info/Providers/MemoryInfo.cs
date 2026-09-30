using UnityEngine;
using UnityEngine.Profiling;

namespace DTech.OmniDebugger
{
	internal sealed class MemoryInfo : IInfoProvider
	{
		public string Title => "Memory";

		public int Order => 10;

		public void Describe(IInfoSection section)
		{
			section.Graph("Allocated", () => InfoFormat.Megabytes(Profiler.GetTotalAllocatedMemoryLong()), 0.0f, 0.0f, "MB");
			section.Live("Reserved", () => InfoFormat.MegabytesText(Profiler.GetTotalReservedMemoryLong()));
			section.Live("Mono used", () => InfoFormat.MegabytesText(Profiler.GetMonoUsedSizeLong()));
			section.Live("Mono heap", () => InfoFormat.MegabytesText(Profiler.GetMonoHeapSizeLong()));
			section.Live("Graphics driver", () => InfoFormat.MegabytesText(Profiler.GetAllocatedMemoryForGraphicsDriver()));
			section.Text("System RAM", SystemInfo.systemMemorySize + " MB");
		}
	}
}
