using System;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	/// <summary>One icon of an <see cref="OmniDebuggerIconCatalog"/>: a key and the image it stands for.</summary>
	[Serializable]
	public sealed class IconEntry
	{
		/// <summary>What <c>[DebugIcon(DebugIconSource.Catalog, key)]</c> names. Compared ordinally.</summary>
		public string Key => _key;

		/// <summary>Used when set; <see cref="Texture"/> otherwise.</summary>
		public Sprite Sprite => _sprite;

		/// <summary>Used when <see cref="Sprite"/> is not set.</summary>
		public Texture2D Texture => _texture;

		[SerializeField] private string _key;
		[SerializeField] private Sprite _sprite;
		[SerializeField] private Texture2D _texture;
	}
}