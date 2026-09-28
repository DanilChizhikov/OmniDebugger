using System;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	[Serializable]
	public sealed class IconEntry
	{
		public string Key => _key;

		/// <summary>Used when set; <see cref="Texture"/> otherwise.</summary>
		public Sprite Sprite => _sprite;

		public Texture2D Texture => _texture;

		[SerializeField] private string _key;
		[SerializeField] private Sprite _sprite;
		[SerializeField] private Texture2D _texture;
	}
}