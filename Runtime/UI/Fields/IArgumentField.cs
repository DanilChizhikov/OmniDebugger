using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// One editor for one command argument. Implement it together with
	/// <see cref="IArgumentFieldHandler"/> to teach the panel a type it does not know.
	/// </summary>
	public interface IArgumentField : IDisposable
	{
		/// <summary>
		/// Raised when the user finished entering a value — a toggle flipped, a dropdown changed, Enter
		/// pressed. Not raised per keystroke, so a value command never writes half a number.
		/// </summary>
		event Action OnCommitted;

		/// <summary>The element to place in the row.</summary>
		VisualElement Root { get; }

		/// <summary>
		/// Reads the current value, already converted to the argument's type.
		/// </summary>
		/// <returns>
		/// <c>false</c> when the field is empty or cannot be read, which makes the row treat the
		/// argument as "not supplied" — and refuse to run when it is required.
		/// </returns>
		bool TryGetValue(out object value);

		/// <summary>
		/// Shows a value that came from elsewhere — the command's getter, or a remembered entry — with
		/// no <see cref="OnCommitted"/> in response.
		/// </summary>
		void SetValue(object value);
	}
}