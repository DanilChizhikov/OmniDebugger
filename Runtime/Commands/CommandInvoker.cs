using System;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace DTech.OmniDebugger
{
	internal sealed class CommandInvoker : ICommandInvoker
	{
		private readonly ICommandCatalog _catalog;
		private readonly ILogSink _log;

		public CommandInvoker(ICommandCatalog catalog, ILogSink log)
		{
			_catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
			_log = log ?? throw new ArgumentNullException(nameof(log));
		}

		public bool TryGetValue(string key, out object value)
		{
			MainThreadGuard.Verify(nameof(TryGetValue));

			value = null;

			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("Command key cannot be null or whitespace.", nameof(key));
			}

			if (!_catalog.TryGetCommand(key, out IDebugCommand command))
			{
				_log.Error($"Command was not found. Key: {key}.");
				return false;
			}

			if (command is not IReadableCommand readable)
			{
				_log.Error($"Command holds no value. Key: {key}; Kind: {command.Definition.Kind}.");
				return false;
			}

			try
			{
				value = readable.GetValue();
				return true;
			}
			catch (Exception exception)
			{
				_log.Exception($"Reading a command value failed. Key: {key}.", Unwrap(exception));
				return false;
			}
		}

		public bool TryExecute(string key, in InvocationRequest request)
		{
			MainThreadGuard.Verify(nameof(TryExecute));

			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("Command key cannot be null or whitespace.", nameof(key));
			}

			if (!_catalog.TryGetCommand(key, out IDebugCommand command))
			{
				_log.Error($"Command was not found. Key: {key}; Origin: {request.Origin}.");
				return false;
			}

			return Execute(command, request);
		}

		public bool TryExecute(CommandDefinition definition, in InvocationRequest request)
		{
			MainThreadGuard.Verify(nameof(TryExecute));

			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			if (!_catalog.TryGetCommand(definition.Key, out IDebugCommand command))
			{
				_log.Error($"Command was not found. Key: {definition.Key}; Origin: {request.Origin}.");
				return false;
			}

			return Execute(command, request);
		}

		private static Exception Unwrap(Exception exception) =>
			exception is TargetInvocationException reflectionException && reflectionException.InnerException != null
				? reflectionException.InnerException
				: exception;

		private static string FormatInvocation(CommandDefinition definition, string origin, object[] arguments)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append("Executing '").Append(definition.Key).Append("' from ").Append(origin);

			if (arguments == null || arguments.Length == 0)
			{
				return builder.Append('.').ToString();
			}

			builder.Append(" with (");

			for (int i = 0; i < arguments.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(Format(arguments[i]));
			}

			return builder.Append(").").ToString();
		}

		private static string Format(object value) => value switch
		{
			null => "null",
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString(),
		};

		private bool Execute(IDebugCommand command, in InvocationRequest request)
		{
			CommandDefinition definition = command.Definition;

			if (command is not IExecutableCommand executable)
			{
				_log.Error($"Command cannot be executed. Key: {definition.Key}; Kind: {definition.Kind}; Origin: {request.Origin}.");

				return false;
			}

			BindCommandResponse response = CommandArguments.TryBind(definition, request.Arguments);
			if (!response.Success)
			{
				_log.Error($"Command arguments are invalid. Key: {definition.Key}; Origin: {request.Origin}; Reason: {response.Error}.");
				return false;
			}

			_log.Info(FormatInvocation(definition, request.Origin, response.Bound));

			try
			{
				executable.Execute(response.Bound);
				return true;
			}
			catch (Exception exception)
			{
				_log.Exception($"Command threw an exception. Key: {definition.Key}; Origin: {request.Origin}.", Unwrap(exception));
				return false;
			}
		}
	}
}