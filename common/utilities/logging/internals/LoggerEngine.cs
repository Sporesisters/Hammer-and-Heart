using System;
using System.Collections.Generic;
using Godot;

namespace Core.Utilities.Logging.Internals;

/// <summary>
/// Provides the internal engine for logging, configuration, and optional batch buffering.
/// </summary>
/// <remarks>
/// <para>
/// This class should not be accessed directly by consumers. It is invoked through
/// the public <see cref="LoggerService"/> facade. It handles message formatting, thresholds,
/// buffering, and delegates to <see cref="LogFormatter"/> and <see cref="LogHandlers"/>.
/// </para>
/// </remarks>
internal class LoggerEngine
{
	/// <summary>
	/// Gets a value indicating whether log messages are collected in a batch
	/// before being flushed to output.
	/// </summary>
	public bool BatchModeEnabled { get; private set; }

	/// <summary>
	/// The maximum number of log messages that may be buffered before automatically flushing.
	/// </summary>
	/// <remarks>
	/// Used only when <see cref="BatchModeEnabled"/> is true. Clamped to a minimum of 2.
	/// Default is <c>50</c>.
	/// </remarks>
	private int _maxBatchSize = 50;

	/// <summary>
	/// The minimum severity level required for a log entry to be processed.
	/// </summary>
	/// <remarks>
	/// Any log below this level is ignored. Default is <see cref="LogLevel.Info"/>.
	/// </remarks>
	private LogLevel _minLogThreshold = LogLevel.Info;

	/// <summary>
	/// The severity level that is treated as an error.
	/// </summary>
	/// <remarks>
	/// When a log entry at or above this level is processed, the buffer is flushed
	/// and <see cref="LogHandlers.ProcessError(string)"/> is invoked.
	/// Default is <see cref="LogLevel.Error"/>.
	/// </remarks>
	private LogLevel _errorThreshold = LogLevel.Error;

	/// <summary>
	/// The severity level that is treated as fatal.
	/// </summary>
	/// <remarks>
	/// When a log entry at or above this level is processed,
	/// <see cref="OnFatalError"/> is triggered via <see cref="LogHandlers.ProcessFatal(Action?)"/>.
	/// Default is <see cref="LogLevel.Fatal"/>.
	/// </remarks>
	private LogLevel _fatalThreshold = LogLevel.Fatal;

	/// <summary>
	/// Buffer that accumulates formatted log entries in batch mode until flushed.
	/// </summary>
	private readonly List<string> _buffer = [];

	/// <summary>
	/// Occurs when a log entry at or above the fatal threshold is processed.
	/// Subscribers can use this to perform shutdown or recovery actions.
	/// </summary>
	public event Action? OnFatalError;

	/// <summary>
	/// Logs a message at the given severity level.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="level">The severity level of the log entry.</param>
	/// <param name="callerFile">
	/// The caller file path, used to extract the caller name for formatting.
	/// </param>
	/// <remarks>
	/// <list type="bullet">
	/// <item>If <paramref name="level"/> is below the current log threshold, the message is ignored.</item>
	/// <item>If batch mode is enabled, the message is added to the buffer until flushed.</item>
	/// <item>If the level is at or above the error threshold, the buffer is flushed and
	/// <see cref="LogHandlers.ProcessError(string)"/> is invoked.</item>
	/// <item>If the level is at or above the fatal threshold, <see cref="OnFatalError"/> is triggered
	/// via <see cref="LogHandlers.ProcessFatal(Action?)"/>.</item>
	/// </list>
	/// </remarks>
	public void Log(string message, LogLevel level, string callerFile)
	{
		if (level < _minLogThreshold) return;

		string formatted = LogFormatter.Format(message, level, callerFile);
		bool printedDirectly = false;

		if (BatchModeEnabled)
		{
			_buffer.Add(formatted);

			if (_buffer.Count >= _maxBatchSize)
			{
				Flush();
			}
		}
		else
		{
			GD.PrintRich(formatted);
			printedDirectly = true;
		}

		if (level >= _errorThreshold)
		{
			Flush();
			LogHandler.ProcessError(message);
		}

		if (printedDirectly)
		{
			GD.Print(LogFormatter.Separator);
		}

		if (level >= _fatalThreshold)
		{
			LogHandler.ProcessFatal(OnFatalError);
		}
	}

	/// <summary>
	/// Immediately writes all buffered log messages to output and clears the buffer.
	/// </summary>
	public void Flush()
	{
		if (_buffer.Count == 0) return;

		GD.PrintRich(string.Join("\n", _buffer));
		_buffer.Clear();
		GD.Print(LogFormatter.Separator);
	}

	/// <summary>
	/// Enables or disables batch mode for logging.
	/// </summary>
	/// <param name="enabled">If <c>true</c>, messages are buffered until flushed.</param>
	/// <param name="callerFile">Caller file path for debug logging.</param>
	/// <remarks>
	/// If batch mode was previously enabled and is being disabled,
	/// the buffer is flushed before deactivation.
	/// </remarks>
	public void SetBatchMode(bool enabled, string callerFile)
	{
		bool wasEnabled = BatchModeEnabled;
		BatchModeEnabled = enabled;

		if (wasEnabled && !enabled)
		{
			Flush();
		}

		Log($"Batch mode {(enabled ? "activated" : "deactivated")}", LogLevel.Debug, callerFile);
	}

	/// <summary>
	/// Sets the maximum number of messages that can be buffered in batch mode before flushing.
	/// </summary>
	/// <param name="size">The maximum batch size. Clamped to a minimum of 2.</param>
	/// <param name="callerFile">Caller file path for debug logging.</param>
	public void SetMaxBatchSize(int size, string callerFile)
	{
		_maxBatchSize = Mathf.Clamp(size, 2, int.MaxValue);
		Log($"Max batch size set to {size}", LogLevel.Debug, callerFile);
	}

	/// <summary>
	/// Sets the minimum log level required for a message to be processed.
	/// </summary>
	/// <param name="level">The new minimum log level.</param>
	/// <param name="callerFile">Caller file path for debug logging.</param>
	public void SetLogLevel(LogLevel level, string callerFile)
	{
		_minLogThreshold = level;
		Log($"Minimum log level set to {level}", LogLevel.Debug, callerFile);
	}

	/// <summary>
	/// Sets the log level that is considered an error and triggers error handling.
	/// </summary>
	/// <param name="level">The log level to treat as an error threshold.</param>
	/// <param name="callerFile">Caller file path for debug logging.</param>
	public void SetErrorThreshold(LogLevel level, string callerFile)
	{
		_errorThreshold = level;
		Log($"Error threshold set to {level}", LogLevel.Debug, callerFile);
	}

	/// <summary>
	/// Sets the log level that is considered fatal and triggers fatal error handling.
	/// </summary>
	/// <param name="level">The log level to treat as a fatal threshold.</param>
	/// <param name="callerFile">Caller file path for debug logging.</param>
	public void SetFatalThreshold(LogLevel level, string callerFile)
	{
		_fatalThreshold = level;
		Log($"Fatal threshold set to {level}", LogLevel.Debug, callerFile);
	}
}
