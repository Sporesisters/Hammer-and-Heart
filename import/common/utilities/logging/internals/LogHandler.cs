using System;
using Godot;

namespace Core.Utilities.Logging.Internals;

/// <summary>
/// Provides static helper methods for handling <c>Error</c> and <c>Fatal</c> log entries.
/// </summary>
public static class LogHandler
{
	/// <summary>
	/// Processes an error message using Godot's error reporting facilities.
	/// </summary>
	/// <param name="message">The error message to report.</param>
	/// <remarks>
	/// <para>
	/// In the Godot editor, the message is pushed via <see cref="GD.PushError(string)"/>,
	/// making it visible in the editor's error panel.
	/// </para>
	/// <para>
	/// In exported (non-editor) builds, the current stack trace is written using <see cref="GD.PrintErr(object[])"/>.
	/// </para>
	/// </remarks>
	public static void ProcessError(string message)
	{
		if (OS.HasFeature("editor"))
		{
			GD.PushError(message);
			return;
		}

		GD.PrintErr(System.Environment.StackTrace);
	}

	/// <summary>
	/// Processes a fatal error by invoking a provided callback, if any.
	/// </summary>
	/// <param name="fatalCallback">An optional delegate to execute when a fatal error occurs.</param>
	/// <remarks>
	/// <para>
	/// Intended for handling unrecoverable states, such as terminating the application
	/// or performing cleanup before shutdown.
	/// </para>
	/// <para>
	/// If <paramref name="fatalCallback"/> is <c>null</c>, the method performs no action.
	/// </para>
	/// </remarks>
	public static void ProcessFatal(Action? fatalCallback) => fatalCallback?.Invoke();
}
