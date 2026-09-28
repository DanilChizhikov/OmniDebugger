using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Icons by key, for <c>[DebugIcon(DebugIconSource.Catalog, "key")]</c>. Put the asset in a
	/// <c>Resources/OmniDebugger</c> folder to have it found on its own, or pass it to
	/// <see cref="IIconRegistry.AddCatalog"/>.
	/// </summary>
	[Preserve]
	[CreateAssetMenu(menuName = "DTech/OmniDebugger/Icon Catalog", fileName = "OmniDebuggerIconCatalog")]
	public sealed class OmniDebuggerIconCatalog : ScriptableObject
	{
		public IReadOnlyList<IconEntry> Entries => _entries;

		[SerializeField] private List<IconEntry> _entries = new ();

		public bool TryGet(string key, out IconEntry iconEntry)
		{
			for (int i = 0; i < _entries.Count; i++)
			{
				if (string.Equals(_entries[i].Key, key, StringComparison.Ordinal))
				{
					iconEntry = _entries[i];
					return true;
				}
			}

			iconEntry = null;
			return false;
		}
	}
}