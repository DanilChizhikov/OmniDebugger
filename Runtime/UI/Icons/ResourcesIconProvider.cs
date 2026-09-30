using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// The default icon provider: reads the key as a path inside a <c>Resources</c> folder and loads a
	/// <see cref="Sprite"/> there, or a <see cref="Texture2D"/> when there is none.
	/// </summary>
	public sealed class ResourcesIconProvider : IOmniDebuggerIconProvider
	{
		/// <summary>The one instance there needs to be.</summary>
		public static readonly ResourcesIconProvider Instance = new ();

		/// <inheritdoc/>
		public bool TryGetIcon(string key, out Background background)
		{
			Sprite sprite = Resources.Load<Sprite>(key);

			if (sprite != null)
			{
				background = Background.FromSprite(sprite);
				return true;
			}

			Texture2D texture = Resources.Load<Texture2D>(key);
			background = texture != null ? Background.FromTexture2D(texture) : default;
			return texture != null;
		}
	}
}
