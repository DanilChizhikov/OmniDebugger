using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Builds and reads the stable identifier of a command: <c>"GroupName/CommandName"</c>.
	/// Keys are human readable on purpose, so they can be typed into a console, stored in a
	/// config, or sent over a wire without a lookup table.
	/// </summary>
	public static class CommandKey
	{
		private const char Separator = '/';

		/// <summary>
		/// Joins a group name and a command name into a key.
		/// </summary>
		public static string Create(string groupName, string commandName)
		{
			if (string.IsNullOrWhiteSpace(groupName))
			{
				throw new ArgumentException("Group name cannot be null or whitespace.", nameof(groupName));
			}

			if (string.IsNullOrWhiteSpace(commandName))
			{
				throw new ArgumentException("Command name cannot be null or whitespace.", nameof(commandName));
			}

			return string.Concat(groupName, Separator.ToString(), commandName);
		}

		/// <summary>
		/// Splits a key back into its group name and command name.
		/// </summary>
		/// <returns><c>true</c> when both parts are present and non-whitespace.</returns>
		public static bool TryParse(string key, out string groupName, out string commandName)
		{
			groupName = null;
			commandName = null;

			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}

			int separatorIndex = key.IndexOf(Separator);
			if (separatorIndex <= 0 || separatorIndex >= key.Length - 1)
			{
				return false;
			}

			string group = key.Substring(0, separatorIndex);
			string command = key.Substring(separatorIndex + 1);

			if (string.IsNullOrWhiteSpace(group) || string.IsNullOrWhiteSpace(command))
			{
				return false;
			}

			groupName = group;
			commandName = command;
			return true;
		}
	}
}