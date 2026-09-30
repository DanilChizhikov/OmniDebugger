using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The sections of the Info tab, the built-in ones included. Reached through
	/// <see cref="IOmniDebuggerHost.Info"/>. Main thread only.
	/// </summary>
	public interface IInfoRegistry
	{
		/// <summary>Raised after every registration and removal.</summary>
		event Action OnChanged;

		/// <summary>Raised after a section was floated or docked.</summary>
		event Action OnFloatingChanged;

		/// <summary>Every section, by <see cref="IInfoProvider.Order"/> and then by title.</summary>
		IReadOnlyList<IInfoProvider> All { get; }

		/// <returns><c>false</c> when it is registered already.</returns>
		bool Register(IInfoProvider provider);

		/// <summary>Removes a section; a built-in one too, found through <see cref="All"/>.</summary>
		/// <returns><c>false</c> when it was not registered.</returns>
		bool Unregister(IInfoProvider provider);

		/// <summary>Shows a section over the game, next to the other floating ones.</summary>
		/// <returns><c>false</c> when it floats already.</returns>
		bool Float(IInfoProvider provider);

		/// <summary>Takes a section off the game and leaves it in the Info tab only.</summary>
		/// <returns><c>false</c> when it was not floating.</returns>
		bool Dock(IInfoProvider provider);

		/// <summary>Whether a section floats over the game.</summary>
		bool IsFloating(IInfoProvider provider);
	}
}
