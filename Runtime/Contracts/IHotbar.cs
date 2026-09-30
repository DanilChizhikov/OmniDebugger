using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The commands pinned to the hotbar: a strip over the game, at hand while the panel is closed. A path
	/// stays pinned while its command is not registered — it shows up again once it is. The runtime panel
	/// saves the list on the device.
	/// </summary>
	public interface IHotbar
	{
		/// <summary>Raised after every change to <see cref="Paths"/>.</summary>
		event Action OnChanged;

		/// <summary>Pinned command paths, in the order the hotbar shows them.</summary>
		IReadOnlyList<string> Paths { get; }

		/// <summary>Whether a command is pinned.</summary>
		bool Contains(string path);

		/// <summary>Pins a command at the end of the hotbar.</summary>
		/// <returns><c>false</c> when it was already pinned.</returns>
		bool Pin(string path);

		/// <returns><c>false</c> when it was not pinned.</returns>
		bool Unpin(string path);

		/// <summary>Pins a command that is not pinned, and unpins one that is.</summary>
		/// <returns>Whether it is pinned now.</returns>
		bool Toggle(string path);

		/// <summary>Moves a pinned command to another position.</summary>
		void Move(int fromIndex, int toIndex);

		/// <summary>Unpins everything.</summary>
		void Clear();
	}
}
