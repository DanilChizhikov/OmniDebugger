using DTech.OmniDebugger.UI;
using NUnit.Framework;

namespace DTech.OmniDebugger.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ViewStateTests
	{
		[Test]
		public void GetTabState_HandsTheSameStateBackForTheSameTab()
		{
			OmniDebuggerViewState state = new OmniDebuggerViewState();

			OmniDebuggerTabState first = state.GetTabState("commands");
			OmniDebuggerTabState second = state.GetTabState("commands");

			Assert.That(second, Is.SameAs(first), "a rebuilt tab has to find what the previous one typed");
			Assert.That(state.GetTabState("logs"), Is.Not.SameAs(first));
		}

		[Test]
		public void GetTabState_RejectsABlankTabId()
		{
			OmniDebuggerViewState state = new OmniDebuggerViewState();

			Assert.That(() => state.GetTabState(" "), Throws.ArgumentException);
		}

		[Test]
		public void Clear_ForgetsTheSelectionAndEveryTabState()
		{
			OmniDebuggerViewState state = new OmniDebuggerViewState();
			state.SelectedTabId = "commands";
			state.ThemeId = "ocean";
			state.Landscape = true;
			state.IsOpen = true;
			state.GetTabState("commands").SetValue("search", "coins");

			state.Clear();

			Assert.That(state.SelectedTabId, Is.Null);
			Assert.That(state.ThemeId, Is.Null);
			Assert.That(state.Landscape, Is.False);
			Assert.That(state.IsOpen, Is.False);
			Assert.That(state.GetTabState("commands").TryGetValue("search", out _), Is.False);
		}

		[Test]
		public void SetValue_StoringNullClearsTheSlotRatherThanRememberingNull()
		{
			OmniDebuggerTabState state = new OmniDebuggerTabState();
			state.SetValue("group", "Cheats");

			Assert.That(state.TryGetValue("group", out object stored), Is.True);
			Assert.That(stored, Is.EqualTo("Cheats"));

			state.SetValue("group", null);

			Assert.That(state.TryGetValue("group", out _), Is.False, "unset and 'set to null' must look alike");
		}

		[Test]
		public void GetOrCreate_BuildsOnceAndThenReturnsTheSameInstance()
		{
			OmniDebuggerTabState state = new OmniDebuggerTabState();
			int built = 0;

			object first = state.GetOrCreate("arguments", () =>
			{
				built++;
				return new object();
			});

			object second = state.GetOrCreate("arguments", () =>
			{
				built++;
				return new object();
			});

			Assert.That(second, Is.SameAs(first));
			Assert.That(built, Is.EqualTo(1));
		}

		[Test]
		public void GetOrCreate_RebuildsWhenTheStoredValueIsOfAnotherType()
		{
			OmniDebuggerTabState state = new OmniDebuggerTabState();
			state.SetValue("slot", "a string");

			string created = state.GetOrCreate("slot", () => "replacement");

			Assert.That(created, Is.EqualTo("a string"), "a matching type is reused");

			state.SetValue("slot", 5);
			string afterTypeChange = state.GetOrCreate("slot", () => "replacement");

			Assert.That(afterTypeChange, Is.EqualTo("replacement"),
				"a slot holding something else must not be handed back as the wrong type");
		}

		[Test]
		public void Remove_ReportsWhetherAnythingWasStored()
		{
			OmniDebuggerTabState state = new OmniDebuggerTabState();
			state.SetValue("group", "Cheats");

			Assert.That(state.Remove("group"), Is.True);
			Assert.That(state.Remove("group"), Is.False);
		}
	}
}
