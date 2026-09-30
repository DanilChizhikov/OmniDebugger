using System;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal readonly struct LogBodyKey
	{
		private const uint HashSeed = 2166136261;
		private const uint HashPrime = 16777619;

		public readonly LogType Type;
		public readonly string Message;
		public readonly string StackTrace;
		public readonly int MessageLength;
		public readonly int StackTraceLength;
		public readonly int Hash;

		public bool IsMessageTruncated => MessageLength < Message.Length;

		public bool IsStackTraceTruncated => StackTraceLength < StackTrace.Length;

		public LogBodyKey(string message, string stackTrace, LogType type, int maxMessageLength, int maxStackTraceLength)
		{
			Type = type;
			Message = message ?? string.Empty;
			StackTrace = stackTrace ?? string.Empty;
			MessageLength = Math.Min(Message.Length, maxMessageLength);
			StackTraceLength = Math.Min(StackTrace.Length, maxStackTraceLength);
			Hash = Combine(
				type,
				Message.AsSpan(0, MessageLength),
				MessageLength < Message.Length,
				StackTrace.AsSpan(0, StackTraceLength),
				StackTraceLength < StackTrace.Length);
		}

		public bool Matches(LogBody body) =>
			body.Hash == Hash &&
			body.Type == Type &&
			((body.Flags & LogFlags.MessageTruncated) != 0) == IsMessageTruncated &&
			((body.Flags & LogFlags.StackTraceTruncated) != 0) == IsStackTraceTruncated &&
			Message.AsSpan(0, MessageLength).SequenceEqual(body.Message.AsSpan()) &&
			StackTrace.AsSpan(0, StackTraceLength).SequenceEqual(body.StackTrace.AsSpan());

		private static int Combine(
			LogType type,
			ReadOnlySpan<char> message,
			bool isMessageTruncated,
			ReadOnlySpan<char> stackTrace,
			bool isStackTraceTruncated)
		{
			uint hash = HashSeed;
			hash = Mix(hash, (uint)type);
			hash = Mix(hash, isMessageTruncated ? 1u : 0u);
			hash = Mix(hash, message);
			hash = Mix(hash, isStackTraceTruncated ? 1u : 0u);
			hash = Mix(hash, stackTrace);
			return unchecked((int)hash);
		}

		private static uint Mix(uint hash, uint value) => unchecked((hash ^ value) * HashPrime);

		private static uint Mix(uint hash, ReadOnlySpan<char> text)
		{
			for (int i = 0; i < text.Length; i++)
			{
				hash = Mix(hash, text[i]);
			}

			return hash;
		}
	}
}
