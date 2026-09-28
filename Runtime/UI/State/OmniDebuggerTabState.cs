using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// A tab's scratch pad. Lives as long as the panel's owner, not as long as the tab's elements,
	/// so what the user typed or selected survives a view rebuild, list recycling, a tab switch and
	/// a catalog change.
	/// </summary>
	public sealed class OmniDebuggerTabState
	{
		private readonly Dictionary<string, object> _values = new (StringComparer.Ordinal);

		/// <summary>Reads a slot.</summary>
		/// <returns><c>false</c> when nothing was stored under the key.</returns>
		public bool TryGetValue(string key, out object value)
		{
			Validate(key);
			return _values.TryGetValue(key, out value);
		}

		/// <summary>
		/// Writes a slot. A null value clears it, so <see cref="TryGetValue"/> reports it as unset
		/// rather than as a stored null.
		/// </summary>
		public void SetValue(string key, object value)
		{
			Validate(key);

			if (value == null)
			{
				_values.Remove(key);
				return;
			}

			_values[key] = value;
		}

		/// <summary>Reads a slot, or creates it with <paramref name="factory"/> when it is unset.</summary>
		public T GetOrCreate<T>(string key, Func<T> factory) where T : class
		{
			Validate(key);

			if (factory == null)
			{
				throw new ArgumentNullException(nameof(factory));
			}

			if (_values.TryGetValue(key, out object stored) && stored is T typed)
			{
				return typed;
			}

			T created = factory.Invoke();
			_values[key] = created;
			return created;
		}

		/// <summary>Clears one slot.</summary>
		/// <returns><c>false</c> when nothing was stored under the key.</returns>
		public bool Remove(string key)
		{
			Validate(key);
			return _values.Remove(key);
		}

		/// <summary>Drops everything the tab remembered.</summary>
		public void Clear() => _values.Clear();

		private static void Validate(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("State key must not be null or whitespace.", nameof(key));
			}
		}
	}
}