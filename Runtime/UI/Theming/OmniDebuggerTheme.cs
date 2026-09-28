using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// A panel skin. Holds nothing but metadata and an ordered list of style sheets, which the
	/// panel adds <b>after</b> its own, so a theme wins every tie and may redefine as little as
	/// one variable.
	/// </summary>
	[Preserve]
	[CreateAssetMenu(menuName = "DTech/OmniDebugger/Theme", fileName = "OmniDebuggerTheme", order = 0)]
	public sealed class OmniDebuggerTheme : ScriptableObject
	{
		/// <summary>
		/// Stable identifier used to remember the selection. Falls back to the asset name, so an
		/// asset that was never given an id still round-trips — until it is renamed.
		/// </summary>
		public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

		/// <summary>Label shown in the theme picker. Falls back to <see cref="Id"/>.</summary>
		public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? Id : _displayName;

		/// <summary>Position in the picker. Lower values are listed first.</summary>
		public int SortOrder => _sortOrder;

		/// <summary>
		/// Sheets applied in order, each one overriding the previous. Never null; null entries are
		/// skipped when the theme is applied.
		/// </summary>
		public IReadOnlyList<StyleSheet> StyleSheets => _styleSheets ??= new ();

		[SerializeField] private string _id;
		[SerializeField] private string _displayName;
		[SerializeField] private int _sortOrder = CommandDefinition.DefaultSortOrder;
		[SerializeField] private List<StyleSheet> _styleSheets = new ();

		/// <summary>Returns <see cref="DisplayName"/> so the type can be dropped into a picker as-is.</summary>
		public override string ToString() => DisplayName;

		internal static OmniDebuggerTheme CreateBuiltIn(string id, string displayName, int sortOrder, StyleSheet sheet)
		{
			OmniDebuggerTheme theme = CreateInstance<OmniDebuggerTheme>();
			theme.name = displayName;
			theme.hideFlags = HideFlags.HideAndDontSave;
			theme._id = id;
			theme._displayName = displayName;
			theme._sortOrder = sortOrder;

			if (sheet != null)
			{
				theme._styleSheets.Add(sheet);
			}

			return theme;
		}
	}
}