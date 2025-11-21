using System;
using System.Runtime.CompilerServices;
using Core.Utilities.Logging.Internals;

namespace Core.Utilities.Logging;

/// <summary>
/// Provides globally accessible logging methods and configuration controls.
/// Acts as a static facade over the internal <see cref="LoggerEngine"/>.
/// </summary>
/// <remarks>
/// This class is intended for application-wide use. It exposes simple log methods
/// for all severity levels (<see cref="LogLevel"/>), and allows runtime configuration
/// of batching, thresholds, and fatal error handling.
/// </remarks>
public static class LoggerService
{
	/// <summary>
	/// Internal engine responsible for managing logging, configuration, and error handling.
	/// </summary>
	private static readonly LoggerEngine _engine = new();

	/// <summary>
	/// Gets whether batch mode is currently enabled.
	/// </summary>
	public static bool IsBatched => _engine.BatchModeEnabled;

	/// <summary>
	/// Event triggered when a log entry meets or exceeds the fatal threshold.
	/// </summary>
	/// <remarks>
	/// Subscribers can perform custom shutdown, alerting, or recovery logic.
	/// </remarks>
	public static event Action? OnFatalError
	{
		add => _engine.OnFatalError += value;
		remove => _engine.OnFatalError -= value;
	}

	/// <summary>
	/// Logs a message at the specified <paramref name="level"/>.
	/// </summary>
	/// <param name="message">The log message.</param>
	/// <param name="level">The severity level (defaults to <see cref="LogLevel.Info"/>).</param>
	/// <param name="callerFile">Automatically populated file path of the caller.</param>
	public static void Log(string message, LogLevel level = LogLevel.Info, [CallerFilePath] string callerFile = "")
		=> _engine.Log(message, level, callerFile);

	/// <summary>Logs a <see cref="LogLevel.Debug"/> message.</summary>
	public static void Debug(string message, [CallerFilePath] string callerFile = "")
		=> _engine.Log(message, LogLevel.Debug, callerFile);

	/// <summary>Logs an <see cref="LogLevel.Info"/> message.</summary>
	public static void Info(string message, [CallerFilePath] string callerFile = "")
		=> _engine.Log(message, LogLevel.Info, callerFile);

	/// <summary>Logs a <see cref="LogLevel.Warning"/> message.</summary>
	public static void Warning(string message, [CallerFilePath] string callerFile = "")
		=> _engine.Log(message, LogLevel.Warning, callerFile);

	/// <summary>Logs a <see cref="LogLevel.Error"/> message.</summary>
	public static void Error(string message, [CallerFilePath] string callerFile = "")
		=> _engine.Log(message, LogLevel.Error, callerFile);

	/// <summary>Logs a <see cref="LogLevel.Fatal"/> message.</summary>
	/// <remarks>
	/// Invokes <see cref="OnFatalError"/> if the fatal threshold is met or exceeded.
	/// </remarks>
	public static void Fatal(string message, [CallerFilePath] string callerFile = "")
		=> _engine.Log(message, LogLevel.Fatal, callerFile);

	/// <summary>
	/// Enables or disables batch mode for log entries.
	/// </summary>
	/// <param name="enabled">True to enable batch buffering, false to print immediately.</param>
	/// <param name="callerFile">Automatically populated file path of the caller.</param>
	public static void SetBatchMode(bool enabled, [CallerFilePath] string callerFile = "")
		=> _engine.SetBatchMode(enabled, callerFile);

	/// <summary>
	/// Sets the maximum size of the log buffer in batch mode.
	/// </summary>
	/// <param name="size">Maximum number of entries before automatic flush (min 2).</param>
	/// <param name="callerFile">Automatically populated file path of the caller.</param>
	public static void SetMaxBatchSize(int size, [CallerFilePath] string callerFile = "")
		=> _engine.SetMaxBatchSize(size, callerFile);

	/// <summary>
	/// Sets the minimum severity level required for log entries to be processed.
	/// </summary>
	/// <param name="level">Entries below this level will be ignored.</param>
	/// <param name="callerFile">Automatically populated file path of the caller.</param>
	public static void SetLogLevel(LogLevel level, [CallerFilePath] string callerFile = "")
		=> _engine.SetLogLevel(level, callerFile);

	/// <summary>
	/// Sets the severity level at which entries are treated as errors.
	/// </summary>
	/// <param name="level">Entries at or above this level trigger error handling.</param>
	/// <param name="callerFile">Automatically populated file path of the caller.</param>
	public static void SetErrorThreshold(LogLevel level, [CallerFilePath] string callerFile = "")
		=> _engine.SetErrorThreshold(level, callerFile);

	/// <summary>
	/// Sets the severity level at which entries are treated as fatal.
	/// </summary>
	/// <param name="level">Entries at or above this level trigger fatal handling.</param>
	/// <param name="callerFile">Automatically populated file path of the caller.</param>
	public static void SetFatalThreshold(LogLevel level, [CallerFilePath] string callerFile = "")
		=> _engine.SetFatalThreshold(level, callerFile);
}
