using System;
using System.IO;
using Godot;

namespace Core.Utilities.Logging.Internals;

/// <summary>
/// Provides static helpers for formatting log messages with timestamps,
/// caller information, and Godot-compatible colorization.
/// </summary>
/// <remarks>
/// <para>
/// The formatter produces log strings suitable for display in Godot's console
/// or editor output panels. It does not write to files or external sinks.
/// </para>
/// </remarks>
public static class LogFormatter
{
	/// <summary>
	/// A visual separator string used to mark divisions in log output.
	/// </summary>
	public static readonly string Separator = new('-', 70);

	/// <summary>
	/// Formats a log entry with a timestamp, severity level, caller file, and message content.
	/// </summary>
	/// <param name="message">The log message to format.</param>
	/// <param name="level">The severity level of the log entry.</param>
	/// <param name="callerFile">The file path of the caller, used to extract the caller name.</param>
	/// <returns>
	/// A formatted log string containing the UTC timestamp, log level, caller name,
	/// and the provided message. The entry is colorized for readability in Godot's console.
	/// </returns>
	public static string Format(string message, LogLevel level, string callerFile)
	{
		string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " UTC";
		string callerName = Path.GetFileNameWithoutExtension(callerFile);
		string entry = $"[{timestamp}] [{level}] [{callerName}] {message}";
		return Colorize(entry, level);
	}

	/// <summary>
	/// Applies Godot BBCode-based colorization to the given text according to the log level.
	/// </summary>
	/// <param name="text">The text to apply color to.</param>
	/// <param name="level">The severity level used to determine the color.</param>
	/// <returns>
	/// The input text wrapped in a Godot <c>[color]</c> tag using
	/// a level-specific color (e.g. gray for Debug, red for Error).
	/// </returns>
	private static string Colorize(string text, LogLevel level)
	{
		Color color = level switch
		{
			LogLevel.Debug => Colors.LightSlateGray,
			LogLevel.Info => Colors.CornflowerBlue,
			LogLevel.Warning => Colors.Goldenrod,
			LogLevel.Error => Colors.Tomato,
			LogLevel.Fatal => Colors.Crimson,
			_ => Colors.White,
		};

		return $"[color={color.ToHtml()}]{text}[/color]";
	}
}
