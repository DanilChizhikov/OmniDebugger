using System.Security.Cryptography;
using System.Text;

namespace DTech.OmniDebugger.UI
{
	internal static class LockSecret
	{
		private const int SaltBytes = 16;
		private const string HexDigits = "0123456789abcdef";

		public static string CreateSalt()
		{
			byte[] bytes = new byte[SaltBytes];

			using (RandomNumberGenerator random = RandomNumberGenerator.Create())
			{
				random.GetBytes(bytes);
			}

			return ToHex(bytes);
		}

		public static string Hash(string secret, string salt)
		{
			using (SHA256 sha = SHA256.Create())
			{
				return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(salt + secret)));
			}
		}

		public static bool Matches(string left, string right)
		{
			if (left == null || right == null || left.Length != right.Length)
			{
				return false;
			}

			int difference = 0;

			for (int i = 0; i < left.Length; i++)
			{
				difference |= left[i] ^ right[i];
			}

			return difference == 0;
		}

		public static bool IsValidPin(string pin)
		{
			if (pin == null ||
				pin.Length < OmniDebuggerLockOptions.MinPinLength ||
				pin.Length > OmniDebuggerLockOptions.MaxPinLength)
			{
				return false;
			}

			for (int i = 0; i < pin.Length; i++)
			{
				if (pin[i] < '0' || pin[i] > '9')
				{
					return false;
				}
			}

			return true;
		}

		private static string ToHex(byte[] bytes)
		{
			char[] chars = new char[bytes.Length * 2];

			for (int i = 0; i < bytes.Length; i++)
			{
				chars[i * 2] = HexDigits[bytes[i] >> 4];
				chars[i * 2 + 1] = HexDigits[bytes[i] & 0xF];
			}

			return new string(chars);
		}
	}
}