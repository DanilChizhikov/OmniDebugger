using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// The vector glyphs the panel draws itself, by name. Any of these names works as an icon key —
	/// <c>[DebugIcon("bolt")]</c>, <c>CommandBuilder.Icon("coin")</c> — and needs no asset: the glyph is
	/// drawn in the text colour at any size. A key that is not a glyph name goes to the icon provider.
	/// </summary>
	public static class OmniGlyphs
	{
		private static readonly Dictionary<string, IconGlyph> _glyphs = new (StringComparer.OrdinalIgnoreCase)
		{
			["close"] = IconGlyph.Close,
			["back"] = IconGlyph.Back,
			["chevron"] = IconGlyph.Chevron,
			["chevron-down"] = IconGlyph.ChevronDown,
			["triangle-down"] = IconGlyph.TriangleDown,
			["triangle-up"] = IconGlyph.TriangleUp,
			["triangle-right"] = IconGlyph.TriangleRight,
			["play"] = IconGlyph.Play,
			["star"] = IconGlyph.Star,
			["star-outline"] = IconGlyph.StarOutline,
			["pin"] = IconGlyph.Pin,
			["pin-outline"] = IconGlyph.PinOutline,
			["search"] = IconGlyph.Search,
			["copy"] = IconGlyph.Copy,
			["info"] = IconGlyph.Info,
			["message"] = IconGlyph.Message,
			["warning"] = IconGlyph.Warning,
			["error"] = IconGlyph.Error,
			["arrow-down"] = IconGlyph.ArrowDown,
			["minus"] = IconGlyph.Minus,
			["plus"] = IconGlyph.Plus,
			["expand"] = IconGlyph.Expand,
			["alert"] = IconGlyph.Alert,
			["sun"] = IconGlyph.Sun,
			["moon"] = IconGlyph.Moon,
			["check"] = IconGlyph.Check,
			["trash"] = IconGlyph.Trash,
			["sliders"] = IconGlyph.Sliders,
			["terminal"] = IconGlyph.Terminal,
			["window"] = IconGlyph.Window,
			["grid"] = IconGlyph.Grid,
			["more"] = IconGlyph.More,
			["refresh"] = IconGlyph.Refresh,
			["gear"] = IconGlyph.Gear,
			["bug"] = IconGlyph.Bug,
			["bolt"] = IconGlyph.Bolt,
			["eye"] = IconGlyph.Eye,
			["clock"] = IconGlyph.Clock,
			["heart"] = IconGlyph.Heart,
			["flag"] = IconGlyph.Flag,
			["coin"] = IconGlyph.Coin,
			["user"] = IconGlyph.User,
			["keyboard"] = IconGlyph.Keyboard,
			["chart"] = IconGlyph.Chart,
			["filter"] = IconGlyph.Filter,
			["pop-out"] = IconGlyph.PopOut,
			["dock"] = IconGlyph.Dock,
			["resize"] = IconGlyph.Resize,
		};

		/// <summary>Every glyph name, sorted.</summary>
		public static IReadOnlyList<string> Names
		{
			get
			{
				if (_names == null)
				{
					string[] names = new string[_glyphs.Count];
					_glyphs.Keys.CopyTo(names, 0);
					Array.Sort(names, StringComparer.Ordinal);
					_names = names;
				}

				return _names;
			}
		}

		private static string[] _names;

		/// <summary>Whether <paramref name="name"/> names a built-in glyph, ignoring case.</summary>
		public static bool Contains(string name) => !string.IsNullOrEmpty(name) && _glyphs.ContainsKey(name);

		internal static bool TryGet(string name, out IconGlyph glyph)
		{
			if (string.IsNullOrEmpty(name))
			{
				glyph = IconGlyph.None;
				return false;
			}

			return _glyphs.TryGetValue(name, out glyph);
		}
	}
}
